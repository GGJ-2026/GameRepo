using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private TextMeshProUGUI nameText;
    
    [Header("Settings")]
    [SerializeField] private int maxWordsPerPage = 25;

    public bool IsDialogOpen { get; private set; } = false;

    private Queue<string> _pages = new Queue<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        dialogPanel.SetActive(false);
    }

public void StartDialog(string characterName, string fullText)
{
    _pages.Clear();
    
    // Update the Name Tag UI
    if (nameText != null) 
    {
        nameText.text = characterName;
    }

    // Slice the text into pages
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
        if (_pages.Count > 0)
        {
            dialogText.text = _pages.Dequeue();
        }
        else
        {
            EndDialog();
        }
    }

    private void EndDialog()
    {
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