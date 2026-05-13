using UnityEngine;
using UnityEngine.SceneManagement; // Required for switching scenes

public class MainMenu : MonoBehaviour
{
    // Call this method to start the game
    public void PlayGame()
    {
        // Loads the next scene in the queue, or pass a scene name string like "Level1"
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Call this method to close the game
    public void QuitGame()
    {
        Debug.Log("Quit application triggered."); // Confirms action inside the Unity Editor
        Application.Quit(); // Shuts down the built application
    }
}
