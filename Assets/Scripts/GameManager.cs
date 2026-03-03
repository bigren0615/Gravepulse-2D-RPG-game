using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Track all enemies currently in combat
    private HashSet<EnemyPatrol> enemiesInCombat = new HashSet<EnemyPatrol>();
    private bool isBattleMusicPlaying = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioManager.Instance.PlayMusic(MusicType.AmbientBGM);
    }

    // Called when an enemy enters combat (hit by player or chasing)
    public void EnterCombat(EnemyPatrol enemy)
    {
        if (enemy == null) return;
        
        bool wasEmpty = enemiesInCombat.Count == 0;
        enemiesInCombat.Add(enemy);

        // Start battle music if this is the first enemy in combat
        if (wasEmpty && !isBattleMusicPlaying)
        {
            AudioManager.Instance.CrossfadeMusic(MusicType.BattleBGM1, 0.5f);
            isBattleMusicPlaying = true;
            Debug.Log("Battle music started! Enemies in combat: " + enemiesInCombat.Count);
        }
    }

    // Called when an enemy exits combat (died or stopped chasing)
    public void ExitCombat(EnemyPatrol enemy)
    {
        if (enemy == null) return;
        
        enemiesInCombat.Remove(enemy);

        // Return to ambient music if no enemies left in combat
        if (enemiesInCombat.Count == 0 && isBattleMusicPlaying)
        {
            AudioManager.Instance.CrossfadeMusic(MusicType.AmbientBGM, 1f);
            isBattleMusicPlaying = false;
            Debug.Log("Battle music stopped! No enemies in combat.");
        }
    }

    // Check if currently in combat
    public bool IsInCombat()
    {
        return enemiesInCombat.Count > 0;
    }

    // Get number of enemies in combat
    public int GetCombatEnemyCount()
    {
        return enemiesInCombat.Count;
    }
}
