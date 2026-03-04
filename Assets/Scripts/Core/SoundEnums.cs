using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enum for all sound effects in the game.
/// Add new sound effects here as you add them to AudioManager.
/// </summary>
public enum SFXType
{
    Dash,
    Suspense,
    FootstepGrass1,
    FootstepGrass2,
    FootstepGrass3,
    AttackSwoosh1,
    AttackSwoosh2,
    AttackSwoosh3,
    Slash1,
    Slash2,
    Slash3,
}

/// <summary>
/// Enum for all music tracks in the game.
/// Add new music tracks here as you add them to AudioManager.
/// </summary>
public enum MusicType
{
    AmbientBGM,
    BattleBGM1
}

/// <summary>
/// Helper class to work with sound enums
/// </summary>
public static class SoundHelper
{
    /// <summary>
    /// Convert SFXType enum to string name
    /// </summary>
    public static string GetName(this SFXType sfx)
    {
        return sfx.ToString();
    }

    /// <summary>
    /// Convert MusicType enum to string name
    /// </summary>
    public static string GetName(this MusicType music)
    {
        return music.ToString();
    }

    /// <summary>
    /// Convert array of SFXType to array of string names
    /// </summary>
    public static string[] GetNames(this SFXType[] sfxArray)
    {
        string[] names = new string[sfxArray.Length];
        for (int i = 0; i < sfxArray.Length; i++)
        {
            names[i] = sfxArray[i].ToString();
        }
        return names;
    }

    /// <summary>
    /// Convert array of MusicType to array of string names
    /// </summary>
    public static string[] GetNames(this MusicType[] musicArray)
    {
        string[] names = new string[musicArray.Length];
        for (int i = 0; i < musicArray.Length; i++)
        {
            names[i] = musicArray[i].ToString();
        }
        return names;
    }
}
