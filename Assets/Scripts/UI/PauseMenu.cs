using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;

    void Start()
    {
        pauseMenu.SetActive(false);
    }

    public void TogglePauseMenu()
    {
        bool isActive = pauseMenu.activeSelf;
        pauseMenu.SetActive(!isActive);
        Time.timeScale = isActive ? 1 : 0; // Pause or unpause the game
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePauseMenu();
        }
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1; // Resume the game
    }

    public void QuitGame()
    {
        Time.timeScale = 1; // Ensure time scale is reset
        SceneManager.LoadSceneAsync(0);
    }
}
