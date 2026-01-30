using UnityEngine;

public class LevelStartTrigger : MonoBehaviour
{
    void Start()
    {
        MessageManager.Instance.Display("Anne is een lief");
        MessageManager.Instance.Display("Hou van!");
    }
}