using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance;

    [Header("UI Components")]
    public TextMeshProUGUI displayText;
    public CanvasGroup panelGroup;

    [Header("Settings")]
    public float typingSpeed = 0.05f; //Time between letters
    public float readTime = 2.0f; //Time to wait after typing finishes

    private Queue<string> messageQueue = new Queue<string>();
    private bool isBusy = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        panelGroup.alpha = 0;
        displayText.maxVisibleCharacters = 0;
    }

    // 2. The Trigger Function (Call this from ANY script)
    public void Display(string message)
    {
        messageQueue.Enqueue(message);
        if (!isBusy) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue() //Allows for queueing any amount of messages that are played in order.
    {
        isBusy = true;

        while (messageQueue.Count > 0)
        {
            string currentMessage = messageQueue.Dequeue();
            displayText.text = currentMessage;
            displayText.maxVisibleCharacters = 0;

            yield return StartCoroutine(Fade(0, 1, 0.5f));

            // Typewriter Effect
            int totalChars = currentMessage.Length;
            for (int i = 0; i <= totalChars; i++)
            {
                displayText.maxVisibleCharacters = i;
                // Optional: Play typing sound here! 
                // AudioManager.Play("TypeKey"); 
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(readTime);
            yield return StartCoroutine(Fade(1, 0, 0.5f));
        }

        isBusy = false;
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            panelGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panelGroup.alpha = end;
    }
}