using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public enum AreaType
    {
        Generic,
        DanceFloor,
        Bar,
        Seating
    }

    [Header("Waypoint Settings")]
    public AreaType areaType = AreaType.Generic;
    public float waitTimeModifier = 0f; // Additive modifier to NPC wait time
}
