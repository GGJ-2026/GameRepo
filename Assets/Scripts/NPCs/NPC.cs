using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NPC : MonoBehaviour
{
    [Header("Interaction Data")]
    public string characterName = "WAITER";
    public Sprite facePortrait;
    [Header("Dialog Data")]
    [TextArea(3, 10)] public string npcDialog1 = "I saw the lady in red...";
    public string playerResponse1 = "Tell me more...";
    [TextArea(3, 10)] public string npcDialog2 = "She was dancing like a maniac.";
    public string playerResponse2 = "I see.";
    [TextArea(3, 10)] public string npcDialog3 = "And then she vanished.";
    public string playerResponse3 = "Wow.";

    [Header("Movement Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 3.0f;
    [SerializeField] private float danceChance = 0.3f;
    [SerializeField] private float socialRadius = 5.0f;

    [Header("Infection Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip coughSound;

    public enum InfectionStage { None, Carrier, Cough, Twitch, Social, Stare }
    [Header("Infection Status")]
    public InfectionStage currentStage = InfectionStage.None;

    // State Handling
    private NavMeshAgent _agent;
    private Animator _anim;
    private bool _isTalking = false;
    private bool _isWaiting = false;
    private bool _hasWalkingParam = false;
    private bool _hasDancingParam = false;
    
    private Waypoint _currentSmartWaypoint;
    
    // Infection behavior flags
    private bool _coughActive = false;
    private bool _twitchActive = false;
    private bool _socialActive = false;
    private bool _stareActive = false;
    private bool _isStaring = false;
    
    // Proximity detection - track nearby NPC distances
    private System.Collections.Generic.Dictionary<NPC, float> _lastKnownDistances = new System.Collections.Generic.Dictionary<NPC, float>();
    private float _proximityCheckTimer = 0f;
    private const float PROXIMITY_CHECK_INTERVAL = 0.5f;
    private const float APPROACH_THRESHOLD = 1.5f; // How much closer before we notice
    
    // Debug
    [SerializeField] private bool showDebugStatus = true;
    private TextMesh _debugTextMesh;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _agent.autoBraking = true;
        
        // Cache Animator parameters
        foreach (var param in _anim.parameters)
        {
            if (param.name == "IsWalking") _hasWalkingParam = true;
            if (param.name == "IsDancing") _hasDancingParam = true;
        }
        
        if (InfectionManager.Instance != null) 
            InfectionManager.Instance.RegisterNPC(this);

        MoveToNextWaypoint();

        if (showDebugStatus)
        {
            CreateDebugLabel();
            UpdateDebugLabel();
        }
    }

    void OnDestroy()
    {
        // Clean up waypoint reservation when NPC is destroyed
        if (InfectionManager.Instance != null)
        {
            InfectionManager.Instance.ReleaseWaypoint(this);
        }
    }

    void Update()
    {
        // Phase 4: Stare Logic (Check periodically or always if close)
        if (currentStage == InfectionStage.Stare && !_isTalking && Camera.main != null)
        {
            // If player is close, stare at them
            float distToPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distToPlayer < 8.0f)
            {
                 FacePlayer(); 
            }
        }

        if (_debugTextMesh != null && Camera.main != null)
        {
            // Billboard effect: Always face camera
            _debugTextMesh.transform.rotation = Camera.main.transform.rotation;
        }

        // 1. Dialogue State Check
        // If we were talking, but the UI is now closed, resume behavior.
        if (_isTalking && !DialogManager.Instance.IsDialogOpen)
        {
            StopTalking();
        }

        if (_isTalking)
        {
            FacePlayer();
            return;
        }
        
        // Proximity detection - look at NPCs that approached us
        if (!_isWaiting && !_isStaring)
        {
            _proximityCheckTimer += Time.deltaTime;
            if (_proximityCheckTimer >= PROXIMITY_CHECK_INTERVAL)
            {
                _proximityCheckTimer = 0f;
                CheckForApproachingNPCs();
            }
        }

        // 2. Movement Logic
        // Check if we've reached the destination (Only if on NavMesh)
        if (_agent.isOnNavMesh && !_isWaiting && !_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitOrDanceRoutine());
        }

        // 3. Animation Sync
        // Tell the animator if we are moving (velocity > 0.1)
        if (_hasWalkingParam)
        {
             _anim.SetBool("IsWalking", _agent.velocity.magnitude > 0.1f);
        }
    }

    // --- Interaction System ---
    
    public void TriggerDialogue()
    {
        if (_isTalking) return;

        _isTalking = true;
        _agent.isStopped = true;
        _anim.SetBool("IsWalking", false);
        _anim.SetBool("IsDancing", false);

        DialogManager.Instance.StartDialog(characterName, npcDialog1, npcDialog2, npcDialog3, playerResponse1, playerResponse2, playerResponse3, facePortrait);
    }

    private void StopTalking()
    {
        _isTalking = false;
        _agent.isStopped = false;
    }
    
    private void CheckForApproachingNPCs()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, socialRadius);
        NPC approachingNPC = null;
        float biggestApproach = 0f;
        
        foreach (var hit in hits)
        {
            NPC other = hit.GetComponent<NPC>();
            if (other != null && other != this)
            {
                float currentDist = Vector3.Distance(transform.position, other.transform.position);
                
                // Check if we have a previous distance for this NPC
                if (_lastKnownDistances.TryGetValue(other, out float previousDist))
                {
                    float approachAmount = previousDist - currentDist;
                    
                    // If they got significantly closer, remember them
                    if (approachAmount > APPROACH_THRESHOLD && approachAmount > biggestApproach)
                    {
                        biggestApproach = approachAmount;
                        approachingNPC = other;
                    }
                }
                
                // Update the distance
                _lastKnownDistances[other] = currentDist;
            }
        }
        
        // If someone approached us, look at them
        if (approachingNPC != null && !_isWaiting)
        {
            Vector3 direction = (approachingNPC.transform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        // Clean up NPCs that are no longer nearby
        var keysToRemove = new System.Collections.Generic.List<NPC>();
        foreach (var kvp in _lastKnownDistances)
        {
            if (kvp.Key == null || Vector3.Distance(transform.position, kvp.Key.transform.position) > socialRadius * 2f)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _lastKnownDistances.Remove(key);
        }
    }

    private void FacePlayer()
    {
        if (Camera.main == null) return;
        Vector3 direction = (Camera.main.transform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    // --- AI Logic ---

    private void MoveToNextWaypoint()
    {
        // Release any previous waypoint reservation
        if (InfectionManager.Instance != null)
        {
            InfectionManager.Instance.ReleaseWaypoint(this);
        }
        
        // Priority 1: Local Waypoints (no reservation needed for local)
        if (waypoints.Length > 0)
        {
            int randomIndex = Random.Range(0, waypoints.Length);
            _agent.SetDestination(waypoints[randomIndex].position);
            _currentSmartWaypoint = null;
            return;
        }

        // Priority 2: Global Waypoints with reservation
        if (InfectionManager.Instance != null)
        {
            Waypoint dest = InfectionManager.Instance.GetCleanWaypoint(this);
            if (dest != null)
            {
                _currentSmartWaypoint = dest;
                _agent.SetDestination(dest.transform.position);
                
                // Reserve this waypoint so others don't target it
                InfectionManager.Instance.ReserveWaypoint(dest, this);
            }
        }
    }

    public void SetInfectionStage(InfectionStage stage)
    {
        currentStage = stage;
        
        _agent.speed = 3.5f;
        
        // STACKING BEHAVIORS: Higher phases include lower phase behaviors
        // Phase 1+ = Cough
        // Phase 2+ = Cough + Twitch
        // Phase 3+ = Cough + Twitch + Social
        // Phase 4+ = Cough + Twitch + Social + Stare
        
        bool shouldCough = (int)stage >= (int)InfectionStage.Cough;
        bool shouldTwitch = (int)stage >= (int)InfectionStage.Twitch;
        bool shouldSocial = (int)stage >= (int)InfectionStage.Social;
        bool shouldStare = (int)stage >= (int)InfectionStage.Stare;
        
        // Start cough if needed and not already running
        if (shouldCough && !_coughActive)
        {
            _coughActive = true;
            StartCoroutine(CoughRoutine());
            Debug.Log($"[INFECTION] {characterName}: Cough behavior STARTED");
        }
        
        // Start twitch if needed
        if (shouldTwitch && !_twitchActive)
        {
            _twitchActive = true;
            StartCoroutine(TwitchRoutine());
            Debug.Log($"[INFECTION] {characterName}: Twitch behavior STARTED");
        }
        
        // Social behavior flag (handled in WaitOrDanceRoutine)
        if (shouldSocial && !_socialActive)
        {
            _socialActive = true;
            Debug.Log($"[INFECTION] {characterName}: Social Invasion behavior STARTED");
        }
        
        // Start stare if needed
        if (shouldStare && !_stareActive)
        {
            _stareActive = true;
            StartCoroutine(StareRoutine());
            Debug.Log($"[INFECTION] {characterName}: Stare behavior STARTED");
        }
        
        // Carrier slows down
        if (stage == InfectionStage.Carrier)
        {
            _agent.speed *= 0.8f;
        }
        
        UpdateDebugLabel();
    }

    private IEnumerator CoughRoutine()
    {
        while (_coughActive)
        {
            yield return new WaitForSeconds(Random.Range(10f, 30f));
            if (!_isTalking && _coughActive)
            {
                Debug.Log($"[COUGH] {characterName}: *Coughs*");
                
                // Play cough sound
                if (audioSource != null && coughSound != null)
                {
                    audioSource.PlayOneShot(coughSound);
                }
            }
        }
    }

    private IEnumerator TwitchRoutine()
    {
        while (_twitchActive)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            if (!_isTalking && _twitchActive && !_isStaring)
            {
                Debug.Log($"[TWITCH] {characterName}: *Twitches erratically*");
                
                // Quick spasmodic rotation
                float duration = 0.15f;
                Quaternion originalRot = transform.rotation;
                
                // Twitch left
                Quaternion twitchLeft = originalRot * Quaternion.Euler(0, -15f, 0);
                float t = 0;
                while (t < 1)
                {
                    t += Time.deltaTime / duration;
                    transform.rotation = Quaternion.Lerp(originalRot, twitchLeft, t);
                    yield return null;
                }
                
                // Twitch right
                Quaternion twitchRight = originalRot * Quaternion.Euler(0, 15f, 0);
                t = 0;
                while (t < 1)
                {
                    t += Time.deltaTime / duration;
                    transform.rotation = Quaternion.Lerp(twitchLeft, twitchRight, t);
                    yield return null;
                }
                
                // Return to original
                t = 0;
                while (t < 1)
                {
                    t += Time.deltaTime / duration;
                    transform.rotation = Quaternion.Lerp(twitchRight, originalRot, t);
                    yield return null;
                }
            }
        }
    }
    
    private IEnumerator StareRoutine()
    {
        while (_stareActive)
        {
            // Wait random time before staring
            yield return new WaitForSeconds(Random.Range(8f, 15f));
            
            if (!_isTalking && _stareActive && Camera.main != null)
            {
                // Check if player is in range
                float distToPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);
                if (distToPlayer < 12f)
                {
                    Debug.Log($"[STARE] {characterName}: *Stares at player*");
                    
                    _isStaring = true;
                    _agent.isStopped = true;
                    
                    // Stare for 2-3 seconds
                    float stareDuration = Random.Range(2f, 3f);
                    float stareTime = 0f;
                    
                    while (stareTime < stareDuration && _stareActive && Camera.main != null)
                    {
                        // Continuously face player
                        FacePlayer();
                        stareTime += Time.deltaTime;
                        yield return null;
                    }
                    
                    Debug.Log($"[STARE] {characterName}: *Stops staring, resumes behavior*");
                    
                    _isStaring = false;
                    _agent.isStopped = false;
                }
            }
        }
    }
    
    // Phase 3: Move closer to nearest NPC after reaching waypoint
    private void MoveCloserToNearestNPC()
    {
        if (!_socialActive) return;
        
        Collider[] hits = Physics.OverlapSphere(transform.position, socialRadius * 2f);
        NPC closestNPC = null;
        float closestDist = float.MaxValue;
        
        foreach (var hit in hits)
        {
            NPC other = hit.GetComponent<NPC>();
            if (other != null && other != this)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestNPC = other;
                }
            }
        }
        
        if (closestNPC != null && closestDist > 1.5f)
        {
            // Move to a point uncomfortably close to them (0.8 units away)
            Vector3 direction = (closestNPC.transform.position - transform.position).normalized;
            Vector3 targetPos = closestNPC.transform.position - direction * 0.8f;
            _agent.SetDestination(targetPos);
            
            Debug.Log($"[SOCIAL] {characterName}: *Moves uncomfortably close to {closestNPC.characterName}*");
        }
    }

    private IEnumerator WaitOrDanceRoutine()
    {
        _isWaiting = true;
        
        // Release waypoint reservation now that we've arrived
        if (InfectionManager.Instance != null)
        {
            InfectionManager.Instance.ReleaseWaypoint(this);
        }
        
        // Phase 3+: Move uncomfortably close to nearest NPC
        if (_socialActive)
        {
            MoveCloserToNearestNPC();
            yield return new WaitForSeconds(0.5f); // Wait for movement to start
        }
        
        // Face nearby people
        FaceGroup();
        
        // --- SMART WAYPOINT LOGIC ---
        bool shouldDance = false;
        float waitModifier = 0f;

        if (_currentSmartWaypoint != null)
        {
            waitModifier = _currentSmartWaypoint.waitTimeModifier;

            if (_currentSmartWaypoint.areaType == Waypoint.AreaType.DanceFloor)
            {
                shouldDance = true; // Always dance on dance floor
            }
            else if (_currentSmartWaypoint.areaType == Waypoint.AreaType.Bar)
            {
                waitModifier += 2.0f; 
                shouldDance = false;
            }
            else
            {
                // Generic Area: Random chance
                shouldDance = Random.value < danceChance;
            }
        }
        else
        {
            // Fallback if no smart waypoint (legacy)
             shouldDance = Random.value < danceChance;
        }
        
        // Less dancing if infected
        if (shouldDance && currentStage == InfectionStage.None && _hasDancingParam)
        {
            _anim.SetBool("IsDancing", true);
        }

        // Wait time increases if you are the Carrier
        float finalWaitTime = (currentStage == InfectionStage.Carrier) ? waitTime + 2.0f : waitTime;
        finalWaitTime += waitModifier;
        
        yield return new WaitForSeconds(finalWaitTime);

        if (_hasDancingParam) _anim.SetBool("IsDancing", false);
        _isWaiting = false;
        MoveToNextWaypoint();
    }
    
    // Find the closest nearby NPC and look at them
    private void FaceGroup()
    {
        if (_hasDancingParam && _anim.GetBool("IsDancing")) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, socialRadius);
        
        // Find the closest NPC
        NPC closestNPC = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            NPC otherNPC = hit.GetComponent<NPC>();
            if (otherNPC != null && otherNPC != this)
            {
                float dist = Vector3.Distance(transform.position, otherNPC.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestNPC = otherNPC;
                }
            }
        }

        // Look at the closest NPC
        if (closestNPC != null)
        {
            Vector3 direction = (closestNPC.transform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    // --- Debug ---
    private void CreateDebugLabel()
    {
        GameObject labelObj = new GameObject("DebugLabel");
        labelObj.transform.SetParent(this.transform);
        labelObj.transform.localPosition = Vector3.up * 2.2f; // Above head
        
        _debugTextMesh = labelObj.AddComponent<TextMesh>();
        _debugTextMesh.alignment = TextAlignment.Center;
        _debugTextMesh.anchor = TextAnchor.LowerCenter;
        _debugTextMesh.characterSize = 0.1f;
        _debugTextMesh.fontSize = 60;
        _debugTextMesh.color = Color.white;
    }

    private void UpdateDebugLabel()
    {
        if (_debugTextMesh == null) return;
        
        string colorHex = "white";
        switch(currentStage)
        {
            case InfectionStage.Carrier: colorHex = "yellow"; break;
            case InfectionStage.Cough: colorHex = "orange"; break;
            case InfectionStage.Twitch: colorHex = "red"; break;
            case InfectionStage.Social: colorHex = "magenta"; break;
            case InfectionStage.Stare: colorHex = "purple"; break;
        }
        
        _debugTextMesh.text = $"<color={colorHex}>{currentStage}</color>";
    }
}