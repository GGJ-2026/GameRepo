using UnityEngine;
using UnityEngine.AI; // Required for NavMesh
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NPC : MonoBehaviour
{
    [Header("Interaction Data")]
    public string characterName = "WAITER";
    public Sprite facePortrait;
    [TextArea(3, 10)] public string dialogue = "I saw the lady in red...";

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

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _agent.autoBraking = true;
        
        // Auto-register with Manager
        if (InfectionManager.Instance != null) 
            InfectionManager.Instance.RegisterNPC(this);

        if (waypoints.Length > 0)
        {
            MoveToNextWaypoint();
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
        // Check if we've reached the destination
        if (!_isWaiting && !_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitOrDanceRoutine());
        }

        // 3. Animation Sync
        // Tell the animator if we are moving (velocity > 0.1)
        _anim.SetBool("IsWalking", _agent.velocity.magnitude > 0.1f);
    }

    // --- Interaction System ---
    
    public void TriggerDialogue()
    {
        if (_isTalking) return;

        _isTalking = true;
        _agent.isStopped = true;
        _anim.SetBool("IsWalking", false);
        _anim.SetBool("IsDancing", false);

        DialogManager.Instance.StartDialog(characterName, dialogue, facePortrait);
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
        if (waypoints.Length == 0) return;

        // Pick a random spot
        int randomIndex = Random.Range(0, waypoints.Length);
        _agent.SetDestination(waypoints[randomIndex].position);
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
        
        bool shouldDance = Random.value < danceChance;
        
        // Less dancing if infected
        if (shouldDance && currentStage == InfectionStage.None)
        {
            _anim.SetBool("IsDancing", true);
        }

        // Wait time increases if you are the Carrier
        float finalWaitTime = (currentStage == InfectionStage.Carrier) ? waitTime + 2.0f : waitTime;
        yield return new WaitForSeconds(finalWaitTime);

        _anim.SetBool("IsDancing", false);
        _isWaiting = false;
        MoveToNextWaypoint();
    }
    
    // Find nearby NPCs and look at the center of the group
    private void FaceGroup()
    {
        if (_anim.GetBool("IsDancing")) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, socialRadius);
        Vector3 centerPoint = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.GetComponent<NPC>() != null && hit.gameObject != gameObject)
            {
                // Phase 3: Invasion of Space - We might look AT a specific person too intensely
                // For now, keep looking at group center but maybe we modify the position calculation later
                centerPoint += hit.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            centerPoint /= count;
            Vector3 direction = (centerPoint - transform.position).normalized;
            
            // Phase 3 Logic: If Social invasion, maybe we look slightly OFF center or too direct?
            // Keeping it simple for now.
            
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}