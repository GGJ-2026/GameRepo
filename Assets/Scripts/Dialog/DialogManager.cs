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
    
    [Header("Settings")]
    [SerializeField] private int maxWordsPerPage = 25;
    [SerializeField] private float typingSpeed = 0.05f;

    public bool IsDialogOpen { get; private set; } = false;
    private bool isTyping = false;
    private string currentMessage = "";
    private Coroutine typingCoroutine;

    private Queue<string> _pages = new Queue<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        dialogPanel.SetActive(false);
    }

public void StartDialog(string characterName, string fullText, Sprite portrait = null)
{
    _pages.Clear();
    
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

    var slicedPages = SliceText(fullText, maxWordsPerPage);
    foreach (string page in slicedPages)
    {
        _pages.Enqueue(page);
    }

    dialogPanel.SetActive(true);
    IsDialogOpen = true;
    
    AdvanceDialog();
}

    public void AdvanceDialog()
    {
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
        dialogPanel.SetActive(false);
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