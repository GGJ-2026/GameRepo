using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GameEndManager : MonoBehaviour
{
    public static GameEndManager Instance;

    [Header("UI References")]
    [SerializeField] private Canvas endScreenCanvas;
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenText;
    [SerializeField] private Image backgroundImage;

    [Header("Settings")]
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private string winText = "Congrats!";
    [SerializeField] private string loseText = "Game Over";
    [SerializeField] private float fadeDuration = 1.0f;

    private bool _gameEnded = false;
    private bool _waitingForInput = false;

    public bool IsGameEnded => _gameEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Hide end screen on start
        HideEndScreen();
    }

    private void Update()
    {
        // Wait for any input to return to menu (using new Input System)
        if (_waitingForInput)
        {
            // Check keyboard
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                ReturnToMenu();
                return;
            }
            
            // Check mouse clicks
            if (Mouse.current != null && 
                (Mouse.current.leftButton.wasPressedThisFrame || 
                 Mouse.current.rightButton.wasPressedThisFrame))
            {
                ReturnToMenu();
                return;
            }
            
            // Check gamepad
            if (Gamepad.current != null && 
                (Gamepad.current.aButton.wasPressedThisFrame || 
                 Gamepad.current.startButton.wasPressedThisFrame))
            {
                ReturnToMenu();
                return;
            }
        }
    }

    /// <summary>
    /// Called when the player stabs an NPC. Checks if it's Patient Zero.
    /// </summary>
    public void EndGame(NPC targetNPC)
    {
        if (_gameEnded) return;
        _gameEnded = true;

        bool isWin = false;
        if (InfectionManager.Instance != null)
        {
            isWin = InfectionManager.Instance.IsPatientZero(targetNPC);
        }

        Debug.Log(isWin ? "WIN! Stabbed Patient Zero!" : "LOSE! Wrong target!");
        ShowEndScreen(isWin);
    }

    private void ShowEndScreen(bool isWin)
    {
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause game time
        Time.timeScale = 0f;

        // Set the text FIRST before showing anything
        string displayText = isWin ? winText : loseText;
        if (endScreenText != null)
        {
            endScreenText.text = displayText;
        }

        // Activate the canvas
        if (endScreenCanvas != null)
        {
            endScreenCanvas.gameObject.SetActive(true);
        }

        // Activate the panel
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);
        }

        // Start fade in
        StartCoroutine(FadeInAndWaitForInput());
    }

    private IEnumerator FadeInAndWaitForInput()
    {
        // Set initial alpha to 0
        if (backgroundImage != null)
        {
            Color startColor = backgroundColor;
            startColor.a = 0f;
            backgroundImage.color = startColor;
        }
        
        if (endScreenText != null)
        {
            Color textColor = endScreenText.color;
            textColor.a = 0f;
            endScreenText.color = textColor;
        }

        // Fade in over time (using unscaled time since game is paused)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade background
            if (backgroundImage != null)
            {
                Color bgColor = backgroundColor;
                bgColor.a = t;
                backgroundImage.color = bgColor;
            }

            // Fade text
            if (endScreenText != null)
            {
                Color textColor = endScreenText.color;
                textColor.a = t;
                endScreenText.color = textColor;
            }

            yield return null;
        }

        // Ensure final alpha is 1
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
        if (endScreenText != null)
        {
            Color textColor = endScreenText.color;
            textColor.a = 1f;
            endScreenText.color = textColor;
        }

        // Small delay to prevent accidental skip
        yield return new WaitForSecondsRealtime(0.3f);
        
        // Now wait for input
        _waitingForInput = true;
    }

    private void HideEndScreen()
    {
        if (endScreenCanvas != null)
            endScreenCanvas.gameObject.SetActive(false);
        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);
    }

    private void ReturnToMenu()
    {
        _waitingForInput = false;
        
        // Reset time scale before loading new scene
        Time.timeScale = 1f;

        // Load main menu (scene index 0)
        SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset if object is destroyed
        Time.timeScale = 1f;
    }
}
