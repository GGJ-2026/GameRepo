using UnityEngine;
using TMPro;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float stabRange = 3f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private TextMeshProUGUI interactionText;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
        if (interactionText != null) interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleHover();
    }

    private void HandleHover()
    {
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen)
        {
            if (interactionText != null) interactionText.gameObject.SetActive(false);
            return;
        }

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent(out NPC npc))
            {
                if (interactionText != null)
                {
                    interactionText.text = "[E] Talk | [LMB] Stab";
                    interactionText.gameObject.SetActive(true);
                }
                return;
            }
        }

        if (interactionText != null) interactionText.gameObject.SetActive(false);
    }

    public void Interact()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent(out NPC npc))
            {
                npc.TriggerDialogue();
            }
        }
    }

    /// <summary>
    /// Stab the NPC you're looking at with a needle to end the game.
    /// </summary>
    public void StabTarget()
    {
        // Don't allow stabbing during dialog or if game already ended
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) return;
        if (GameEndManager.Instance != null && GameEndManager.Instance.IsGameEnded) return;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, stabRange, interactLayer))
        {
            if (hit.collider.TryGetComponent(out NPC npc))
            {
                Debug.Log($"Stabbing {npc.characterName}!");
                if (GameEndManager.Instance != null)
                {
                    GameEndManager.Instance.EndGame(npc);
                }
            }
        }
    }
}