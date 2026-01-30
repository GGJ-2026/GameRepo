using UnityEngine;

public class LevelStartTrigger : MonoBehaviour
{
    void Start()
    {
        MessageManager.Instance.Display("Objective: Find Patient Zero");
        MessageManager.Instance.Display("Good Luck.");
    }
}