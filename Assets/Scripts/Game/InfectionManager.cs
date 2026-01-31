using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InfectionManager : MonoBehaviour
{
    public static InfectionManager Instance;

    [Header("Settings")]
    [SerializeField] private float phaseDuration = 30f; // Time between PZ phase advancements
    [SerializeField] private float infectionCheckInterval = 1.0f;

    [Header("State")]
    [SerializeField] private NPC patientZero;
    [SerializeField] private List<NPC> allNPCs = new List<NPC>();
    [SerializeField] private int currentPZPhase = 0;
    
    // Global Waypoints
    [SerializeField] private List<Waypoint> globalWaypoints = new List<Waypoint>();

    private float _timer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Auto-find waypoints if list is empty
        if (globalWaypoints.Count == 0)
        {
            Waypoint[] FoundWaypoints = FindObjectsOfType<Waypoint>();
            globalWaypoints.AddRange(FoundWaypoints);
        }

        // Auto-infect a random NPC if none assigned
        if (patientZero == null && allNPCs.Count > 0)
        {
            StartGame(allNPCs[Random.Range(0, allNPCs.Count)]);
        }
    }

    private void Update()
    {
        if (patientZero == null) return;

        _timer += Time.deltaTime;

        if (_timer >= phaseDuration)
        {
            _timer = 0;
            AdvancePlague();
        }
    }

    public void RegisterNPC(NPC npc)
    {
        if (!allNPCs.Contains(npc))
        {
            allNPCs.Add(npc);
        }
    }

    public void StartGame(NPC initialPatientZero)
    {
        patientZero = initialPatientZero;
        currentPZPhase = 0;
        patientZero.SetInfectionStage(NPC.InfectionStage.Carrier);
        Debug.Log($"Infection Started. Patient Zero is: {patientZero.name}");
    }

    private void AdvancePlague()
    {
        // 1. Advance Patient Zero
        currentPZPhase++;
        if (currentPZPhase > 4) currentPZPhase = 4; // Cap at max phase

        patientZero.SetInfectionStage((NPC.InfectionStage)currentPZPhase);
        Debug.Log($"Patient Zero advanced to Phase {currentPZPhase}");

        // 2. Infect a new victim (start them at Phase 1)
        NPC victim = GetRandomHealthyNPC();
        if (victim != null)
        {
            victim.SetInfectionStage(NPC.InfectionStage.Cough);
            Debug.Log($"New Infection Spreading to: {victim.name}");
        }
    }

    public NPC GetRandomHealthyNPC()
    {
        // Find all NPCs who are NOT the patient zero and are NOT yet infected
        var healthy = allNPCs.Where(n => n != patientZero && n.currentStage == NPC.InfectionStage.None).ToList();
        
        if (healthy.Count == 0) return null;
        
        return healthy[Random.Range(0, healthy.Count)];
    }

    public Waypoint GetCleanWaypoint()
    {
        if (globalWaypoints.Count == 0) return null;

        // Try 10 times to find a spot that isn't crowded
        for (int i = 0; i < 10; i++)
        {
            Waypoint candidate = globalWaypoints[Random.Range(0, globalWaypoints.Count)];
            bool isOccupied = Physics.CheckSphere(candidate.transform.position, 1.0f, LayerMask.GetMask("Default", "NPC")); 
            
            bool tooClose = false;
            foreach(var npc in allNPCs)
            {
                if (Vector3.Distance(npc.transform.position, candidate.transform.position) < 1.5f)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose) return candidate;
        }

        // If crowded, return random
        return globalWaypoints[Random.Range(0, globalWaypoints.Count)];
    }
}
