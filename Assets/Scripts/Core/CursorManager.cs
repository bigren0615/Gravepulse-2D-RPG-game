using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Auto-hides the cursor during gameplay.
/// Hold Left Alt to temporarily show it — while visible, mouse-click attacks
/// and dashes are suppressed so the player can safely interact with UI buttons.
///
/// Add this component to the GameManager GameObject (or any persistent object).
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    /// <summary>True while Left Alt is held and the cursor is visible.</summary>
    public static bool IsCursorVisible { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HideCursor();
    }

    private void Update()
    {
        bool altHeld = Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;

        if (altHeld && !IsCursorVisible)
            ShowCursor();
        else if (!altHeld && IsCursorVisible)
            HideCursor();
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        IsCursorVisible = true;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        IsCursorVisible = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Re-apply the correct cursor state when the window regains focus.
        if (!hasFocus) return;
        if (IsCursorVisible) ShowCursor();
        else HideCursor();
    }
}
