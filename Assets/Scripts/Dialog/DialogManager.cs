using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;
using System.Collections;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button tellMeMoreButton;
    [SerializeField] private Button nevermindButton;
    
    [Header("Settings")]
    [SerializeField] private int maxWordsPerPage = 25;
    [SerializeField] private float typingSpeed = 0.05f;

    public bool IsDialogOpen { get; private set; } = false;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private string currentMessage = "";
    private Coroutine typingCoroutine;
    
    // Multi-stage data
    private string _followUpText = "";
    private string _confirmLabel1 = "";
    private string _confirmLabel2 = "";
    private bool _hasFollowUp = false;
    private bool _inFollowUpPhase = false;

    private Queue<string> _pages = new Queue<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        dialogPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        if (tellMeMoreButton != null) tellMeMoreButton.onClick.AddListener(() => OnOptionSelected(true));
        if (nevermindButton != null) nevermindButton.onClick.AddListener(() => OnOptionSelected(false));
    }

public void StartDialog(string characterName, string introText, string followUpText, string confirmLabel1, string confirmLabel2, Sprite portrait = null)
{
    _pages.Clear();
    
    // reset state
    _inFollowUpPhase = false;
    _followUpText = followUpText;
    _confirmLabel1 = confirmLabel1;
    _confirmLabel2 = confirmLabel2;
    _hasFollowUp = !string.IsNullOrEmpty(followUpText);

    if (nameText != null) 
    {
        nameText.text = characterName;
    }

    if (portraitImage != null)
    {
        if (portrait != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }
    }

    var slicedPages = SliceText(introText, maxWordsPerPage);
    foreach (string page in slicedPages)
    {
        _pages.Enqueue(page);
    }

    dialogPanel.SetActive(true);
    IsDialogOpen = true;
    
    // Unlock cursor immediately
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    
    AdvanceDialog();
}



    private void Update()
    {
        // 1. Advance / Skip logic
        bool clicked = false;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
             if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) clicked = true;
        }

        if (IsDialogOpen)
        {
            // Force cursor to stay visible and unlocked
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (clicked && !isWaitingForChoice)
            {
                AdvanceDialog();
            }
        }
    }

    public void AdvanceDialog()
    {
        if (isWaitingForChoice) return;

        if (isTyping)
        {
            // If typing, skip to end
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogText.text = currentMessage;
            dialogText.maxVisibleCharacters = currentMessage.Length;
            isTyping = false;
            return;
        }

        if (_pages.Count > 0)
        {
            currentMessage = _pages.Dequeue();
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterEffect(currentMessage));
        }
        else
        {
            // Show choices after Intro AND after FollowUp (per user request)
            ShowChoices();
        }
    }

    private void ShowChoices()
    {
        if (choicePanel != null)
        {
            isWaitingForChoice = true;
            choicePanel.SetActive(true);
            
            // Unlock cursor for selection
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Update button texts based on phase
            if (tellMeMoreButton != null)
            {
               string label = !_inFollowUpPhase 
                   ? (string.IsNullOrEmpty(_confirmLabel1) ? "Tell me more..." : _confirmLabel1)
                   : (string.IsNullOrEmpty(_confirmLabel2) ? "I see." : _confirmLabel2);

               // Try to use Hover Effect script if it exists, otherwise fallback
               var hover = tellMeMoreButton.GetComponent<UIButtonHover>();
               if (hover != null)
               {
                   hover.UpdateText(label);
               }
               else
               {
                   var txt = tellMeMoreButton.GetComponentInChildren<TextMeshProUGUI>();
                   if (txt != null) txt.text = label;
               }
            }
        }
        else
        {
            // Fallback if no choice panel assigned
            EndDialog();
        }
    }

    private void OnOptionSelected(bool wantsMore)
    {
        // Hide panel immediately
        if (choicePanel != null) choicePanel.SetActive(false);
        isWaitingForChoice = false;
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;

        if (wantsMore && _hasFollowUp && !_inFollowUpPhase)
        {
            // Start Part 2
            _inFollowUpPhase = true;
            _pages.Clear();
            var slicedPages = SliceText(_followUpText, maxWordsPerPage);
            foreach (string page in slicedPages) _pages.Enqueue(page);
            
            AdvanceDialog();
        }
        else
        {
            // End conversation (Nevermind OR we just finished the follow up)
            EndDialog();
        }
    }

    private IEnumerator TypewriterEffect(string message)
    {
        isTyping = true;
        dialogText.text = message;
        dialogText.maxVisibleCharacters = 0;

        int totalChars = message.Length;
        for (int i = 0; i <= totalChars; i++)
        {
            dialogText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void EndDialog()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        IsDialogOpen = false;
        isWaitingForChoice = false;
        
        dialogPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private List<string> SliceText(string text, int limit)
    {
        string[] words = text.Split(' ');
        List<string> pages = new List<string>();
        StringBuilder currentBuilder = new StringBuilder();
        int wordCount = 0;

        foreach (string word in words)
        {
            if (wordCount >= limit)
            {
                pages.Add(currentBuilder.ToString());
                currentBuilder.Clear();
                wordCount = 0;
            }
            currentBuilder.Append(word + " ");
            wordCount++;
        }
        if (currentBuilder.Length > 0) pages.Add(currentBuilder.ToString());
        return pages;
    }
}