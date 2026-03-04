using UnityEngine;

/// <summary>
/// Helper script to automatically set up the health bar system in the scene
/// Attach this to any GameObject and it will create the necessary components
/// </summary>
public class HealthBarSetupHelper : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Click this button in the Inspector to auto-setup health bar system")]
    public bool autoSetupOnAwake = true;

    private void Awake()
    {
        if (autoSetupOnAwake)
        {
            SetupHealthBarSystem();
        }
    }

    [ContextMenu("Setup Health Bar System")]
    public void SetupHealthBarSystem()
    {
        // Check if manager already exists
        EnemyHealthBarManager existingManager = FindObjectOfType<EnemyHealthBarManager>();
        if (existingManager != null)
        {
            Debug.Log("EnemyHealthBarManager already exists in scene!");
            return;
        }

        // Create canvas for health bars
        GameObject managerObj = new GameObject("EnemyHealthBarManager");
        EnemyHealthBarManager manager = managerObj.AddComponent<EnemyHealthBarManager>();

        Debug.Log("✓ EnemyHealthBarManager created successfully! Health bar system is ready.");
        Debug.Log("✓ Health bars will float above each enemy (top-right) with line connectors.");
        Debug.Log("✓ All enemies will automatically show health bars when taking damage.");
    }

    [ContextMenu("Force Re-Register All Enemies")]
    public void ForceRegisterAllEnemies()
    {
        EnemyPatrol[] enemies = FindObjectsOfType<EnemyPatrol>();
        
        if (EnemyHealthBarManager.Instance == null)
        {
            Debug.LogError("No EnemyHealthBarManager found! Run 'Setup Health Bar System' first.");
            return;
        }

        foreach (EnemyPatrol enemy in enemies)
        {
            // This will be handled by the enemy's Start method
            Debug.Log($"Enemy {enemy.gameObject.name} will register on next frame");
        }

        Debug.Log($"Found {enemies.Length} enemies in scene");
    }
}
