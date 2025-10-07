using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaterSettings", menuName = "Crimson/Water Settings")]
public class WaterSettings : ScriptableObject
{
    [Header("Global")]
    public float seaLevel = 0f;
    public float gravity = 9.81f;
    [Tooltip("Scales simulation time (1 = realtime).")]
    public float timeScale = 1f;

    [Header("Waves")]
    [Tooltip("Sum of Gerstner waves. Keep steepness reasonable to avoid self-intersection.")]
    public List<Wave> waves = new List<Wave>()
    {
        new Wave { amplitude = 0.6f, wavelength = 12f, directionDegrees = 20f, steepness = 0.6f, speed = 1.0f },
        new Wave { amplitude = 0.4f, wavelength = 8f,  directionDegrees = 135f, steepness = 0.5f, speed = 1.1f },
        new Wave { amplitude = 0.3f, wavelength = 20f, directionDegrees = 300f, steepness = 0.5f, speed = 0.9f },
    };

    [Serializable]
    public struct Wave
    {
        [Tooltip("Meters")] public float amplitude;
        [Tooltip("Meters")] public float wavelength;
        [Tooltip("Direction in degrees (0 = +X, clockwise on XZ)")] public float directionDegrees;
        [Range(0f, 1f)] public float steepness;
        [Tooltip("Multiplier on deep-water dispersion speed")] public float speed;
    }
}