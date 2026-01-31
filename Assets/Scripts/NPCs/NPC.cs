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

    [Header("Movement Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 3.0f;
    [SerializeField] private float danceChance = 0.3f;
    [SerializeField] private float socialRadius = 5.0f;

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

    void Update()
    {
        // Phase 4: Stare Logic (Check periodically or always if close)
        if (currentStage == InfectionStage.Stare && !_isTalking)
        {
            // If player is close, stare at them
            float distToPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distToPlayer < 8.0f)
            {
                 FacePlayer(); 
                 // If staring, maybe slow down agent?
            }
        }

        if (_debugTextMesh != null)
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

        DialogManager.Instance.StartDialog(characterName, npcDialog1, npcDialog2, playerResponse1, playerResponse2, facePortrait);
    }

    private void StopTalking()
    {
        _isTalking = false;
        _agent.isStopped = false;
    }

    private void FacePlayer()
    {
        Vector3 direction = (Camera.main.transform.position - transform.position).normalized;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // --- AI Logic ---

    private void MoveToNextWaypoint()
    {
        // Priority 1: Local Waypoints
        if (waypoints.Length > 0)
        {
            int randomIndex = Random.Range(0, waypoints.Length);
            _agent.SetDestination(waypoints[randomIndex].position);
            return;
        }

        // Priority 2: Global Waypoints
        if (InfectionManager.Instance != null)
        {
            Waypoint dest = InfectionManager.Instance.GetCleanWaypoint();
            if (dest != null)
            {
                _currentSmartWaypoint = dest;
                _agent.SetDestination(dest.transform.position);
            }
        }
    }

    public void SetInfectionStage(InfectionStage stage)
    {
        currentStage = stage;
        
        // Reset/Apply modifiers based on stage
        _agent.speed = 3.5f; // Reset to default (assuming 3.5)
        
        switch (currentStage)
        {
            case InfectionStage.Carrier:
                _agent.speed *= 0.8f; // Slower
                break;
            case InfectionStage.Twitch:
                StartCoroutine(TwitchRoutine());
                break;
            case InfectionStage.Cough:
                StartCoroutine(CoughRoutine());
                break;
            case InfectionStage.Stare:
                break;
        }
        
        UpdateDebugLabel();
    }

    private IEnumerator CoughRoutine()
    {
        while (currentStage == InfectionStage.Cough)
        {
            yield return new WaitForSeconds(Random.Range(5f, 15f));
            if (!_isTalking)
            {
                Debug.Log($"{characterName}: *Coughs*");
                // TODO: Play Audio Clip
                _anim.SetTrigger("Cough"); // Assuming trigger exists, or remove if not
            }
        }
    }

    private IEnumerator TwitchRoutine()
    {
        while (currentStage == InfectionStage.Twitch)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            if (!_isTalking && !_agent.pathPending && _agent.velocity.magnitude > 0.1f)
            {
                // Quick spasmodic rotation
                float duration = 0.2f;
                Quaternion originalRot = transform.rotation;
                Quaternion twitchRot = originalRot * Quaternion.Euler(0, Random.Range(-15, 15), 0);
                
                float t = 0;
                while(t < 1)
                {
                    t += Time.deltaTime / duration;
                    transform.rotation = Quaternion.Lerp(twitchRot, originalRot, t);
                    yield return null;
                }
            }
        }
    }

    private IEnumerator WaitOrDanceRoutine()
    {
        _isWaiting = true;
        
        // Socialize: Face nearby people
        // Phase 3 (Social Invasion): Stand weirdly close or ignore distance
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
                // TODO: Drinking animation trigger? For now just wait longer.
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
    
    // Find nearby NPCs and look at the center of the group
    private void FaceGroup()
    {
        if (_hasDancingParam && _anim.GetBool("IsDancing")) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, socialRadius);
        Vector3 centerPoint = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.GetComponent<NPC>() != null && hit.gameObject != gameObject)
            {
                // Phase 3: Invasion of Space - We might look AT a specific person too intensely
                centerPoint += hit.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            centerPoint /= count;
            Vector3 direction = (centerPoint - transform.position).normalized;
            
            // Phase 3 Logic: If Social invasion, maybe we look slightly OFF center or too direct?
            
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