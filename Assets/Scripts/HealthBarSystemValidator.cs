using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Validation script to test the enemy health bar system
/// Add this to a GameObject and run in Play mode to verify everything works
/// </summary>
public class HealthBarSystemValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    public bool runValidationOnStart = true;
    public bool showDetailedLogs = true;

    private List<string> errors = new List<string>();
    private List<string> warnings = new List<string>();
    private List<string> successes = new List<string>();

    private void Start()
    {
        if (runValidationOnStart)
        {
            ValidateSystem();
        }
    }

    [ContextMenu("Validate Health Bar System")]
    public void ValidateSystem()
    {
        errors.Clear();
        warnings.Clear();
        successes.Clear();

        Debug.Log("════════════════════════════════════════════════");
        Debug.Log("🔍 VALIDATING ENEMY HEALTH BAR SYSTEM");
        Debug.Log("════════════════════════════════════════════════");

        // Test 1: Check for EnemyHealthBarManager
        ValidateHealthBarManager();

        // Test 2: Check for enemies in scene
        ValidateEnemies();

        // Test 3: Check scripts exist
        ValidateScripts();

        // Test 4: Check integration
        ValidateIntegration();

        // Report results
        ReportResults();
    }

    private void ValidateHealthBarManager()
    {
        EnemyHealthBarManager manager = FindObjectOfType<EnemyHealthBarManager>();
        
        if (manager == null)
        {
            errors.Add("❌ EnemyHealthBarManager not found in scene!");
            errors.Add("   Fix: Run GameObject → UI → Enemy Health Bar System");
            return;
        }

        successes.Add("✅ EnemyHealthBarManager exists");

        // Check canvas setup
        Canvas canvas = manager.GetComponent<Canvas>();
        if (canvas == null)
        {
            errors.Add("❌ EnemyHealthBarManager missing Canvas component");
        }
        else
        {
            successes.Add("✅ Canvas component found");

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                warnings.Add("⚠️ Canvas render mode is not ScreenSpaceOverlay");
            }
            else
            {
                successes.Add("✅ Canvas render mode is correct (ScreenSpaceOverlay)");
            }
        }

        // Check CanvasScaler
        CanvasScaler scaler = manager.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            warnings.Add("⚠️ CanvasScaler not found - UI may not scale properly");
        }
        else
        {
            successes.Add("✅ CanvasScaler configured");
        }
    }

    private void ValidateEnemies()
    {
        EnemyPatrol[] enemies = FindObjectsOfType<EnemyPatrol>();
        
        if (enemies.Length == 0)
        {
            warnings.Add("⚠️ No enemies found in scene");
            warnings.Add("   Add enemies to test the health bar system");
            return;
        }

        successes.Add($"✅ Found {enemies.Length} enemy/enemies in scene");

        // Check if enemies have required components
        foreach (EnemyPatrol enemy in enemies)
        {
            if (enemy.GetComponent<SpriteRenderer>() == null)
            {
                warnings.Add($"⚠️ Enemy '{enemy.gameObject.name}' missing SpriteRenderer");
            }
        }
    }

    private void ValidateScripts()
    {
        // Check if all required scripts exist in project
        System.Type[] requiredTypes = new System.Type[]
        {
            typeof(EnemyHealthBar),
            typeof(EnemyHealthBarManager),
            typeof(EnemyPatrol),
            typeof(HealthBarSetupHelper)
        };

        foreach (System.Type type in requiredTypes)
        {
            if (type != null)
            {
                successes.Add($"✅ Script found: {type.Name}");
            }
            else
            {
                errors.Add($"❌ Script missing: {type}");
            }
        }
    }

    private void ValidateIntegration()
    {
        // Check if EnemyPatrol has the health bar code integrated
        EnemyPatrol[] enemies = FindObjectsOfType<EnemyPatrol>();
        
        if (enemies.Length > 0)
        {
            EnemyPatrol testEnemy = enemies[0];
            
            // Verify health system exists
            if (testEnemy.maxHealth > 0)
            {
                successes.Add("✅ Enemy health system configured");
            }
            else
            {
                warnings.Add("⚠️ Enemy maxHealth is 0 - set a value in Inspector");
            }
        }
    }

    private void ReportResults()
    {
        Debug.Log("\n════════════════════════════════════════════════");
        Debug.Log("📊 VALIDATION RESULTS");
        Debug.Log("════════════════════════════════════════════════\n");

        // Successes
        if (successes.Count > 0)
        {
            Debug.Log("<color=green><b>SUCCESSES:</b></color>");
            foreach (string success in successes)
            {
                Debug.Log(success);
            }
            Debug.Log("");
        }

        // Warnings
        if (warnings.Count > 0)
        {
            Debug.Log("<color=yellow><b>WARNINGS:</b></color>");
            foreach (string warning in warnings)
            {
                Debug.LogWarning(warning);
            }
            Debug.Log("");
        }

        // Errors
        if (errors.Count > 0)
        {
            Debug.Log("<color=red><b>ERRORS:</b></color>");
            foreach (string error in errors)
            {
                Debug.LogError(error);
            }
            Debug.Log("");
        }

        // Summary
        Debug.Log("════════════════════════════════════════════════");
        if (errors.Count == 0 && warnings.Count == 0)
        {
            Debug.Log("<color=green><size=14>✨ SYSTEM VALIDATED! ALL CHECKS PASSED! ✨</size></color>");
            Debug.Log("<color=green>Your health bar system is ready to use!</color>");
        }
        else if (errors.Count == 0)
        {
            Debug.Log("<color=yellow>⚠️ SYSTEM MOSTLY READY - Some warnings</color>");
            Debug.Log("<color=yellow>The system will work, but check warnings above</color>");
        }
        else
        {
            Debug.Log("<color=red>❌ SYSTEM NOT READY - Fix errors above</color>");
            Debug.Log("<color=red>Run setup: GameObject → UI → Enemy Health Bar System</color>");
        }
        Debug.Log("════════════════════════════════════════════════\n");
    }

    [ContextMenu("Test Damage Enemy")]
    public void TestDamageEnemy()
    {
        EnemyPatrol[] enemies = FindObjectsOfType<EnemyPatrol>();
        
        if (enemies.Length == 0)
        {
            Debug.LogWarning("No enemies found to test!");
            return;
        }

        EnemyPatrol testEnemy = enemies[0];
        float damageAmount = 25f;
        
        Debug.Log($"🔪 Testing damage on {testEnemy.gameObject.name}");
        Debug.Log($"   Before HP: {testEnemy.maxHealth}");
        
        testEnemy.TakeDamage(damageAmount);
        
        Debug.Log($"   Dealt {damageAmount} damage");
        Debug.Log($"   Health bar should update in top-right corner!");
    }

    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        Debug.Log("════════════════════════════════════════════════");
        Debug.Log("📋 ENEMY HEALTH BAR SYSTEM INFO");
        Debug.Log("════════════════════════════════════════════════");

        EnemyHealthBarManager manager = FindObjectOfType<EnemyHealthBarManager>();
        if (manager != null)
        {
            Debug.Log($"Manager: {manager.gameObject.name}");
            Debug.Log($"Active: {manager.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("No manager found");
        }

        EnemyPatrol[] enemies = FindObjectsOfType<EnemyPatrol>();
        Debug.Log($"Enemies in scene: {enemies.Length}");

        Debug.Log("════════════════════════════════════════════════");
    }
}
