using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer;
    private Camera _cam;

    private void Start() => _cam = Camera.main;

    public void Interact()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            // Check if object has an NPC script
            if (hit.collider.TryGetComponent(out NPC npc))
            {
                npc.TriggerDialogue();
            }
        }
    }
}