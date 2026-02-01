using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.InputSystem;

public class InfectionManager : MonoBehaviour
{
    public static InfectionManager Instance;

    [Header("Settings")]
    [SerializeField] private List<float> phaseDurations = new List<float>() { 60f, 60f, 45f, 30f, 30f }; // Duration for Phase 0, 1, 2, 3, 4
    [SerializeField] private float defaultPhaseDuration = 30f;
    [SerializeField] private float infectionCheckInterval = 1.0f;
    
    [Header("Debug")]
    public bool isProgressionPaused = false;
    [SerializeField] private Key debugAdvanceKey = Key.P;

    [Header("State")]
    [SerializeField] private NPC patientZero;
    [SerializeField] private List<NPC> allNPCs = new List<NPC>();
    [SerializeField] private int currentPZPhase = 0;
    
    [SerializeField] private List<Waypoint> globalWaypoints = new List<Waypoint>();
    
    // Waypoint reservation system - tracks which NPC is heading to which waypoint
    private Dictionary<Waypoint, NPC> _waypointReservations = new Dictionary<Waypoint, NPC>();

    private float _timer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (globalWaypoints.Count == 0)
        {
            Waypoint[] FoundWaypoints = FindObjectsOfType<Waypoint>();
            globalWaypoints.AddRange(FoundWaypoints);

            if (globalWaypoints.Count == 0)
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("Waypoint");
                foreach (var go in taggedObjects)
                {
                    Waypoint wp = go.GetComponent<Waypoint>();
                    if (wp == null) wp = go.AddComponent<Waypoint>();
                    
                    globalWaypoints.Add(wp);
                }
                
                if (globalWaypoints.Count > 0)
                {
                    Debug.Log($"InfectionManager: Auto-converted {globalWaypoints.Count} objects to Smart Waypoints.");
                }
            }
        }

        if (patientZero == null && allNPCs.Count > 0)
        {
            StartGame(allNPCs[Random.Range(0, allNPCs.Count)]);
        }
    }

    private void Update()
    {
        if (patientZero == null) return;
        
        // Debug Input
        if (Keyboard.current != null && Keyboard.current[debugAdvanceKey].wasPressedThisFrame)
        {
            AdvancePlague();
        }

        if (isProgressionPaused) return;

        _timer += Time.deltaTime;
        
        float currentLimit = GetCurrentPhaseDuration();

        if (_timer >= currentLimit)
        {
            _timer = 0;
            AdvancePlague();
        }
    }
    
    private float GetCurrentPhaseDuration()
    {
        if (currentPZPhase < phaseDurations.Count) return phaseDurations[currentPZPhase];
        return defaultPhaseDuration;
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

    [ContextMenu("Force Advance Plague")]
    public void AdvancePlague()
    {
        currentPZPhase++;
        if (currentPZPhase > 4) currentPZPhase = 4;

        patientZero.SetInfectionStage((NPC.InfectionStage)(currentPZPhase + 1));
        Debug.Log($"Patient Zero advanced to Phase {currentPZPhase}");

        NPC victim = GetRandomHealthyNPC();
        if (victim != null)
        {
            victim.SetInfectionStage(NPC.InfectionStage.Cough);
            Debug.Log($"New Infection Spreading to: {victim.name}");
        }
    }

    public NPC GetRandomHealthyNPC()
    {
        var healthy = allNPCs.Where(n => n != patientZero && n.currentStage == NPC.InfectionStage.None).ToList();
        
        if (healthy.Count == 0) return null;
        
        return healthy[Random.Range(0, healthy.Count)];
    }

    public bool IsPatientZero(NPC npc)
    {
        return npc != null && npc == patientZero;
    }


    public Waypoint GetCleanWaypoint(NPC requestingNPC = null)
    {
        if (globalWaypoints.Count == 0) return null;
        
        bool wantsToCrowd = requestingNPC != null && 
                            requestingNPC.currentStage == NPC.InfectionStage.Social;
        
        if (wantsToCrowd)
        {
            var crowdedWaypoints = _waypointReservations.Keys.ToList();
            if (crowdedWaypoints.Count > 0)
            {
                Waypoint target = crowdedWaypoints[Random.Range(0, crowdedWaypoints.Count)];
                return target;
            }
        }

        for (int i = 0; i < 10; i++)
        {
            Waypoint candidate = globalWaypoints[Random.Range(0, globalWaypoints.Count)];
            
            if (_waypointReservations.TryGetValue(candidate, out NPC owner) && owner != requestingNPC)
            {
                continue;
            }
            
            bool tooClose = false;
            foreach(var npc in allNPCs)
            {
                if (npc == requestingNPC) continue;
                if (Vector3.Distance(npc.transform.position, candidate.transform.position) < 1.5f)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose) return candidate;
        }

        var unreserved = globalWaypoints.Where(w => !_waypointReservations.ContainsKey(w)).ToList();
        if (unreserved.Count > 0)
        {
            return unreserved[Random.Range(0, unreserved.Count)];
        }
        
        return globalWaypoints[Random.Range(0, globalWaypoints.Count)];
    }
    
    public void ReserveWaypoint(Waypoint waypoint, NPC npc)
    {
        if (waypoint == null || npc == null) return;
        _waypointReservations[waypoint] = npc;
    }
    
    public void ReleaseWaypoint(NPC npc)
    {
        if (npc == null) return;
        
        var toRemove = _waypointReservations.Where(kvp => kvp.Value == npc).Select(kvp => kvp.Key).ToList();
        foreach (var wp in toRemove)
        {
            _waypointReservations.Remove(wp);
        }
    }
    
    public int GetReservationCount() => _waypointReservations.Count;
}
