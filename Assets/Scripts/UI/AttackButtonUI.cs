using UnityEngine;

/// <summary>
/// Attack button UI — tints background/icon based on attack cooldown progress
/// and shows the per-platform input hint icon.
/// No cooldown mask or ready ring (attack cooldown is too short to need them).
/// Extends ActionButtonUI for shared input-hint and colour-tint logic.
///
/// CHILD ORDER (top = drawn first = behind):
///
///   AttackButton  (this script + OnScreenButton)
///   ├─ Background   ← solid circle
///   └─ Icon         ← attack/sword sprite on top
///
/// Optional hint children (same as DashButtonUI):
///   AttackButton
///   ├─ InputHintBg  ← dark silhouette plate (assign → inputHintBgImage)
///   └─ InputHint    ← key/button icon        (assign → inputHintImage)
/// </summary>
public class AttackButtonUI : ActionButtonUI
{
    [Header("References")]
    [Tooltip("PlayerController on the player GameObject.")]
    public PlayerController playerController;

    private void Update()
    {
        if (playerController == null) return;

        ApplyProgressTint(playerController.AttackCooldownProgress);
        UpdateInputHintIfNeeded();
    }
}
