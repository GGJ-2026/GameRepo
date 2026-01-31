using UnityEngine;

public class NPC : MonoBehaviour
{
    public string characterName = "WAITER";
    [TextArea(5, 10)] public string dialogue = "I saw the lady in red...";

    public void TriggerDialogue()
    {
        DialogManager.Instance.StartDialog(characterName, dialogue);
    }
}