using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WaterRenderer : MonoBehaviour
{
    const int MaxWaves = 8;

    [SerializeField] private WaterSettings settings;
    [SerializeField] private Renderer targetRenderer;

    private MaterialPropertyBlock mpb;

    private static readonly int SeaLevelID = Shader.PropertyToID("_SeaLevel");
    private static readonly int GravityID = Shader.PropertyToID("_Gravity");
    private static readonly int SimTimeID = Shader.PropertyToID("_SimTime");
    private static readonly int WaveCountID = Shader.PropertyToID("_WaveCount");

    // Generate property IDs for each wave slot
    private static readonly int[] WaveData1IDs = new int[MaxWaves];
    private static readonly int[] WaveData2IDs = new int[MaxWaves];
    private static bool idsInit = false;

    private void InitIDs()
    {
        if (idsInit) return;
        for (int i = 0; i < MaxWaves; i++)
        {
            WaveData1IDs[i] = Shader.PropertyToID("_WaveData1_" + i);
            WaveData2IDs[i] = Shader.PropertyToID("_WaveData2_" + i);
        }
        idsInit = true;
    }

    private void OnEnable()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        InitIDs();
        PushStaticProps();
    }

    private void OnValidate()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        InitIDs();
        PushStaticProps();
    }

    private void Update()
    {
        if (!settings || !targetRenderer) return;

        float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        float simTime = ((settings != null) ? settings.timeScale : 1f) * time;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(SimTimeID, simTime);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private void PushStaticProps()
    {
        if (!settings || !targetRenderer) return;

        int count = Mathf.Min(settings.waves.Count, MaxWaves);

        targetRenderer.GetPropertyBlock(mpb);

        // Globals
        mpb.SetFloat(SeaLevelID, settings.seaLevel);
        mpb.SetFloat(GravityID, settings.gravity);
        mpb.SetFloat(WaveCountID, count);

        // Pack waves into individual uniforms
        for (int i = 0; i < MaxWaves; i++)
        {
            if (i < count)
            {
                var w = settings.waves[i];
                float rad = w.directionDegrees * Mathf.Deg2Rad;
                Vector4 data1 = new Vector4(w.amplitude, Mathf.Max(0.1f, w.wavelength), Mathf.Cos(rad), Mathf.Sin(rad));
                Vector4 data2 = new Vector4(Mathf.Max(0.001f, w.speed), 0f, 0f, 0f);
                mpb.SetVector(WaveData1IDs[i], data1);
                mpb.SetVector(WaveData2IDs[i], data2);
            }
            else
            {
                mpb.SetVector(WaveData1IDs[i], Vector4.zero);
                mpb.SetVector(WaveData2IDs[i], Vector4.zero);
            }
        }

        // SimTime set in Update
        targetRenderer.SetPropertyBlock(mpb);
    }
}