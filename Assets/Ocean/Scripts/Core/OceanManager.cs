using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Central manager for ocean simulation
/// Handles wave generation, parameters, and provides query interface
/// </summary>
public class OceanManager : MonoBehaviour
{
    #region Wave Data Structures

    [System.Serializable]
    public struct WaveParameters
    {
        [Tooltip("Direction the wave travels (normalized)")]
        public float2 direction;

        [Tooltip("Wave steepness (0 = sine wave, 1 = sharp crest)")]
        [Range(0f, 1f)]
        public float steepness;

        [Tooltip("Distance between wave crests in meters")]
        [Range(1f, 200f)]
        public float wavelength;

        [Tooltip("Speed multiplier for wave motion")]
        [Range(0.1f, 2f)]
        public float speed;

        // Auto-calculated properties
        public float frequency => 2f * Mathf.PI / wavelength;
        public float amplitude => steepness / (frequency * waveCount);

        private static int waveCount = 8; // Set by manager

        public static void SetWaveCount(int count) => waveCount = count;
    }

    #endregion

    #region Inspector Fields

    [Header("Wave Configuration")]
    [Tooltip("Number of wave layers (more = more detailed, but slower)")]
    [Range(4, 16)]
    public int waveCount = 8;

    [Tooltip("Master scale for all wave heights")]
    [Range(0f, 5f)]
    public float globalWaveHeight = 1f;

    [Tooltip("Master scale for wave steepness")]
    [Range(0f, 2f)]
    public float globalWaveSteepness = 1f;

    [Tooltip("Master scale for wave speed")]
    [Range(0f, 2f)]
    public float globalWaveSpeed = 1f;

    [Header("Wave Parameters")]
    public WaveParameters[] waves;

    [Header("Materials")]
    [Tooltip("Ocean surface material (will receive wave parameters)")]
    public Material oceanMaterial;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Transform debugTestPoint; // Optional: visualize wave height at a point

    #endregion

    #region Private Fields

    private NativeArray<WaveParameters> nativeWaves;
    private bool isInitialized = false;

    // Shader property IDs (cached for performance)
    private static readonly int WaveCountID = Shader.PropertyToID("_WaveCount");
    private static readonly int GlobalWaveHeightID = Shader.PropertyToID("_GlobalWaveHeight");
    private static readonly int GlobalWaveSteepnessID = Shader.PropertyToID("_GlobalWaveSteepness");
    private static readonly int GlobalWaveSpeedID = Shader.PropertyToID("_GlobalWaveSpeed");

    // Wave data arrays for shader (max 16 waves)
    private static readonly int WaveDirectionsID = Shader.PropertyToID("_WaveDirections");
    private static readonly int WavePropertiesID = Shader.PropertyToID("_WaveProperties"); // steepness, wavelength, speed, amplitude

    private Vector4[] waveDirections = new Vector4[16];
    private Vector4[] waveProperties = new Vector4[16];

    #endregion

    #region Initialization

    void OnEnable()
    {
        InitializeWaves();
    }

    void OnDisable()
    {
        CleanupNativeArrays();
    }

    void InitializeWaves()
    {
        WaveParameters.SetWaveCount(waveCount);

        // Create default waves if none exist
        if (waves == null || waves.Length != waveCount)
        {
            GenerateDefaultWaves();
        }

        // Create native array for Jobs system
        nativeWaves = new NativeArray<WaveParameters>(waveCount, Allocator.Persistent);
        UpdateNativeWaveArray();

        isInitialized = true;

        Debug.Log($"✅ Ocean Manager initialized with {waveCount} waves");
    }

    void GenerateDefaultWaves()
    {
        waves = new WaveParameters[waveCount];

        // Create a realistic spread of wave sizes and directions
        for (int i = 0; i < waveCount; i++)
        {
            float t = (float)i / (waveCount - 1);

            // Vary wavelength from small ripples to large swells
            float wavelength = Mathf.Lerp(5f, 80f, t);

            // Random direction with some bias toward prevailing wind
            float angle = UnityEngine.Random.Range(-180f, 180f);
            float2 direction = new float2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // Longer waves are generally less steep
            float steepness = Mathf.Lerp(0.6f, 0.3f, t);

            // Slight speed variation for more organic motion
            float speed = UnityEngine.Random.Range(0.9f, 1.1f);

            waves[i] = new WaveParameters
            {
                direction = math.normalize(direction),
                steepness = steepness,
                wavelength = wavelength,
                speed = speed
            };
        }

        Debug.Log("🌊 Generated default wave configuration");
    }

    void UpdateNativeWaveArray()
    {
        if (!nativeWaves.IsCreated) return;

        for (int i = 0; i < waveCount; i++)
        {
            nativeWaves[i] = waves[i];
        }
    }

    void CleanupNativeArrays()
    {
        if (nativeWaves.IsCreated)
        {
            nativeWaves.Dispose();
        }
        isInitialized = false;
    }

    #endregion

    #region Update Loop

    void Update()
    {
        if (!isInitialized) return;

        // Update native array if waves changed in inspector
        UpdateNativeWaveArray();

        // Send wave data to shaders
        UpdateShaderProperties();

        // Debug visualization
        if (showDebugGizmos && debugTestPoint != null)
        {
            DebugWaveHeight();
        }
    }

    void UpdateShaderProperties()
    {
        if (oceanMaterial == null) return;

        // Update global parameters
        Shader.SetGlobalFloat(GlobalWaveHeightID, globalWaveHeight);
        Shader.SetGlobalFloat(GlobalWaveSteepnessID, globalWaveSteepness);
        Shader.SetGlobalFloat(GlobalWaveSpeedID, globalWaveSpeed);
        Shader.SetGlobalInt(WaveCountID, waveCount);

        // Pack wave data into shader arrays
        for (int i = 0; i < waveCount; i++)
        {
            waveDirections[i] = new Vector4(
                waves[i].direction.x,
                waves[i].direction.y,
                0, 0
            );

            waveProperties[i] = new Vector4(
                waves[i].steepness,
                waves[i].wavelength,
                waves[i].speed,
                0 // Reserved for future use
            );
        }

        Shader.SetGlobalVectorArray(WaveDirectionsID, waveDirections);
        Shader.SetGlobalVectorArray(WavePropertiesID, waveProperties);
    }

    #endregion

    #region Public Query Interface

    /// <summary>
    /// Get water height at a specific world position (synchronous)
    /// </summary>
    public float GetWaterHeight(Vector3 worldPosition)
    {
        if (!isInitialized) return 0f;

        float height = 0f;
        float time = Time.time;

        for (int i = 0; i < waveCount; i++)
        {
            height += CalculateGerstnerWaveHeight(
                worldPosition,
                waves[i],
                time
            );
        }

        return height * globalWaveHeight;
    }

    /// <summary>
    /// Get water height and normal at a specific position
    /// </summary>
    public void GetWaterData(Vector3 worldPosition, out float height, out Vector3 normal)
    {
        if (!isInitialized)
        {
            height = 0f;
            normal = Vector3.up;
            return;
        }

        float3 totalOffset = float3.zero;
        float3 totalNormal = new float3(0, 1, 0);
        float time = Time.time;

        for (int i = 0; i < waveCount; i++)
        {
            CalculateGerstnerWave(
                worldPosition,
                waves[i],
                time,
                out float3 offset,
                out float3 waveNormal
            );

            totalOffset += offset;
            totalNormal += waveNormal;
        }

        height = totalOffset.y * globalWaveHeight;
        normal = math.normalize(totalNormal);
    }

    #endregion

    #region Gerstner Wave Calculations

    /// <summary>
    /// Calculate Gerstner wave - returns only height (Y offset)
    /// </summary>
    private float CalculateGerstnerWaveHeight(Vector3 position, WaveParameters wave, float time)
    {
        float k = 2f * Mathf.PI / wave.wavelength;
        float c = Mathf.Sqrt(9.8f / k); // Wave speed from gravity
        float2 d = math.normalize(wave.direction);
        float f = k * (d.x * position.x + d.y * position.z - c * time * wave.speed * globalWaveSpeed);
        float a = wave.steepness / k;

        return a * Mathf.Sin(f);
    }

    /// <summary>
    /// Calculate full Gerstner wave - returns offset and normal
    /// This is the core wave calculation that must match the shader exactly
    /// </summary>
    private void CalculateGerstnerWave(
        Vector3 position,
        WaveParameters wave,
        float time,
        out float3 offset,
        out float3 normal)
    {
        // Wave parameters
        float k = 2f * Mathf.PI / wave.wavelength; // Wave number
        float c = Mathf.Sqrt(9.8f / k); // Wave speed (deep water)
        float2 d = math.normalize(wave.direction);
        float f = k * (d.x * position.x + d.y * position.z - c * time * wave.speed * globalWaveSpeed);
        float a = wave.steepness / k; // Amplitude

        // Position offset (Gerstner wave displacement)
        offset = new float3(
            d.x * (a * math.cos(f)),
            a * math.sin(f),
            d.y * (a * math.cos(f))
        );

        // Normal calculation
        float wa = k * a;
        normal = new float3(
            -d.x * wa * math.cos(f),
            1f - wave.steepness * math.sin(f),
            -d.y * wa * math.cos(f)
        );
    }

    #endregion

    #region Debug

    void DebugWaveHeight()
    {
        if (debugTestPoint == null) return;

        Vector3 testPos = debugTestPoint.position;
        GetWaterData(testPos, out float height, out Vector3 normal);

        // Move test point to water surface
        debugTestPoint.position = new Vector3(testPos.x, height, testPos.z);

        // Draw normal
        Debug.DrawRay(debugTestPoint.position, normal * 2f, Color.cyan);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || waves == null) return;

        // Draw wave directions
        Gizmos.color = Color.yellow;
        for (int i = 0; i < Mathf.Min(waves.Length, waveCount); i++)
        {
            Vector3 dir = new Vector3(waves[i].direction.x, 0, waves[i].direction.y);
            Vector3 start = transform.position + Vector3.up * i * 0.5f;
            Gizmos.DrawRay(start, dir * 5f);
        }
    }

    #endregion
}