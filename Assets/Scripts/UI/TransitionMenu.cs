using UnityEngine;

/// <summary>
/// Handles specific UI actions for a menu, such as returning to the main menu.
/// It inherits the default UI selection behavior from the base Menu class.
/// </summary>
public class TransitionMenu : Menu
{
    /// <summary>
    /// Public function to be called by a UI button.
    /// It accesses the GameManager singleton to trigger the main menu transition.
    /// </summary>
    public void GoToMainMenu()
    {
        // Ensure a GameManager instance exists before calling the method.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MainMenu();
        }
        else
        {
            Debug.LogError("GameManager instance not found! Cannot return to the main menu.");
        }
    }
}