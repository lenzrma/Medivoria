using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHomeButton();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}