using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject settingsMenu;

    public void Start()
    {
        if (mainMenu != null) mainMenu.SetActive(true);
        if (settingsMenu != null) settingsMenu.SetActive(false);
    }
    public void StartGame()
    {
        SceneManager.LoadSceneAsync(1);
        if (settingsMenu != null) settingsMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
    }

//     DISABLED FOR NOW
/*     public void OpenSettings()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        mainMenu.SetActive(true);
    } */

    public void QuitGame()
    {
        Application.Quit();
    }
}
