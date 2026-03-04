using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor menu items for easy health bar system setup
/// </summary>
public class HealthBarSetupMenu
{
    [MenuItem("GameObject/UI/Enemy Health Bar System", false, 10)]
    public static void CreateHealthBarSystem()
    {
        // Check if manager already exists
        EnemyHealthBarManager existingManager = Object.FindObjectOfType<EnemyHealthBarManager>();
        if (existingManager != null)
        {
            Debug.LogWarning("EnemyHealthBarManager already exists in the scene!");
            Selection.activeGameObject = existingManager.gameObject;
            EditorGUIUtility.PingObject(existingManager.gameObject);
            return;
        }

        // Create manager for world-space health bars
        GameObject managerObj = new GameObject("EnemyHealthBarManager");
        EnemyHealthBarManager manager = managerObj.AddComponent<EnemyHealthBarManager>();
        
        // Set up manager with defaults
        Undo.RegisterCreatedObjectUndo(managerObj, "Create Enemy Health Bar System");
        Selection.activeGameObject = managerObj;

        Debug.Log("✓ Enemy Health Bar System created successfully!");
        Debug.Log("✓ Health bars will float above each enemy (top-right) with line connectors");
        Debug.Log("✓ Customize offset and appearance in Inspector");
    }

    [MenuItem("Tools/Enemy Health Bars/Setup Health Bar System")]
    public static void SetupFromMenu()
    {
        CreateHealthBarSystem();
    }

    [MenuItem("Tools/Enemy Health Bars/Add Setup Helper to Scene")]
    public static void AddSetupHelper()
    {
        GameObject helperObj = new GameObject("HealthBarSetup");
        helperObj.AddComponent<HealthBarSetupHelper>();
        
        Undo.RegisterCreatedObjectUndo(helperObj, "Create Health Bar Setup Helper");
        Selection.activeGameObject = helperObj;

        Debug.Log("✓ HealthBarSetupHelper created! It will auto-setup the system when you play.");
    }

    [MenuItem("Tools/Enemy Health Bars/Documentation")]
    public static void OpenDocumentation()
    {
        string docPath = Application.dataPath + "/../HEALTHBAR_SETUP.md";
        
        if (System.IO.File.Exists(docPath))
        {
            Application.OpenURL("file:///" + docPath);
            Debug.Log("Opening documentation...");
        }
        else
        {
            Debug.LogWarning("Documentation not found at: " + docPath);
            Debug.Log("Documentation should be at project root: HEALTHBAR_SETUP.md");
        }
    }

    [MenuItem("Tools/Enemy Health Bars/Select Health Bar Manager")]
    public static void SelectManager()
    {
        EnemyHealthBarManager manager = Object.FindObjectOfType<EnemyHealthBarManager>();
        if (manager != null)
        {
            Selection.activeGameObject = manager.gameObject;
            EditorGUIUtility.PingObject(manager.gameObject);
        }
        else
        {
            Debug.LogWarning("No EnemyHealthBarManager found in scene. Create one first!");
        }
    }

    [MenuItem("Tools/Enemy Health Bars/Select Health Bar Manager", true)]
    public static bool SelectManagerValidation()
    {
        return Object.FindObjectOfType<EnemyHealthBarManager>() != null;
    }
}
