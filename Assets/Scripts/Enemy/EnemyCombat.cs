using UnityEngine;

/// <summary>
/// Enemy combat coordinator - manages combat state and GameManager integration
/// </summary>
[RequireComponent(typeof(EnemyController), typeof(EnemyHealth))]
public class EnemyCombat : MonoBehaviour
{
    private bool isInCombat = false;
    private EnemyHealth health;

    void Start()
    {
        health = GetComponent<EnemyHealth>();
    }

    /// <summary>
    /// Enter combat state
    /// </summary>
    public void EnterCombat()
    {
        if (isInCombat) return;
        
        isInCombat = true;
        
        // Show health bar when entering combat
        if (health != null)
        {
            EnemyHealthBar healthBar = health.GetHealthBar();
            if (healthBar != null)
            {
                healthBar.Show();
            }
        }
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterCombat(gameObject);
        }
    }

    /// <summary>
    /// Exit combat state
    /// </summary>
    public void ExitCombat()
    {
        if (!isInCombat) return;
        
        isInCombat = false;
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombat(gameObject);
        }
    }

    /// <summary>
    /// Check if currently in combat
    /// </summary>
    public bool IsInCombat() => isInCombat;

    private void OnDestroy()
    {
        // Make sure to exit combat when destroyed
        if (isInCombat && GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombat(gameObject);
        }
    }
}
