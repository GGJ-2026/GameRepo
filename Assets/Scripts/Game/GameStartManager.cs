using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    [Header("UI References")]
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private Image fadeImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorCloseSound;

    [Header("Settings")]
    [SerializeField] private float fadeOutDuration = 2.0f;
    [SerializeField] private float initialDelay = 0.5f;

    private bool _introComplete = false;

    public bool IsIntroComplete => _introComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Lock cursor during intro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Temporarily disable player input
        DisablePlayerInput();

        // Start with black screen
        if (fadeCanvas != null)
            fadeCanvas.gameObject.SetActive(true);
        
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }

        // Start the intro sequence
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // Wait a moment before starting
        yield return new WaitForSeconds(initialDelay);

        // Play door close sound
        if (audioSource != null && doorCloseSound != null)
        {
            audioSource.PlayOneShot(doorCloseSound);
        }

        // Wait a moment after door sound starts
        yield return new WaitForSeconds(0.5f);

        // Fade out the black screen
        float elapsed = 0f;
        Color startColor = fadeImage != null ? fadeImage.color : Color.black;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            if (fadeImage != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                fadeImage.color = c;
            }

            yield return null;
        }

        // Ensure fully transparent
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // Hide canvas
        if (fadeCanvas != null)
            fadeCanvas.gameObject.SetActive(false);

        // Re-enable player input
        EnablePlayerInput();

        _introComplete = true;
    }

    private void DisablePlayerInput()
    {
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null)
        {
            player.enabled = false;
        }
    }

    private void EnablePlayerInput()
    {
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null)
        {
            player.enabled = true;
        }
    }
}