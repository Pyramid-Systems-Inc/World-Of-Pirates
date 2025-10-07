using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WaterRenderer : MonoBehaviour
{
    const int MaxWaves = 8;

    [SerializeField] private WaterSettings settings;
    [SerializeField] private Renderer targetRenderer;

    // Cached arrays to avoid GC every frame
    private float[] amp = new float[MaxWaves];
    private float[] wl = new float[MaxWaves];
    private float[] dirX = new float[MaxWaves];
    private float[] dirZ = new float[MaxWaves];
    private float[] speed = new float[MaxWaves];

    private MaterialPropertyBlock mpb;
    private static readonly int SeaLevelID = Shader.PropertyToID("_SeaLevel");
    private static readonly int GravityID = Shader.PropertyToID("_Gravity");
    private static readonly int SimTimeID = Shader.PropertyToID("_SimTime");
    private static readonly int WaveCountID = Shader.PropertyToID("_WaveCount");
    private static readonly int AmpID = Shader.PropertyToID("_Amp");
    private static readonly int WlID = Shader.PropertyToID("_WL");
    private static readonly int DirXID = Shader.PropertyToID("_DirX");
    private static readonly int DirZID = Shader.PropertyToID("_DirZ");
    private static readonly int SpeedID = Shader.PropertyToID("_Speed");

    private void OnEnable()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        PushStaticProps();
    }

    private void OnValidate()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        PushStaticProps();
    }

    private void Update()
    {
        if (!settings || !targetRenderer) return;

        // Use realtime in Edit mode so waves animate without Play
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
        for (int i = 0; i < MaxWaves; i++)
        {
            if (i < count)
            {
                var w = settings.waves[i];
                float rad = w.directionDegrees * Mathf.Deg2Rad;
                amp[i] = w.amplitude;
                wl[i] = Mathf.Max(0.1f, w.wavelength);
                dirX[i] = Mathf.Cos(rad);
                dirZ[i] = Mathf.Sin(rad);
                speed[i] = Mathf.Max(0.001f, w.speed);
            }
            else
            {
                amp[i] = wl[i] = dirX[i] = dirZ[i] = speed[i] = 0f;
            }
        }

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(SeaLevelID, settings.seaLevel);
        mpb.SetFloat(GravityID, settings.gravity);
        mpb.SetInt(WaveCountID, count);
        mpb.SetFloatArray(AmpID, amp);
        mpb.SetFloatArray(WlID, wl);
        mpb.SetFloatArray(DirXID, dirX);
        mpb.SetFloatArray(DirZID, dirZ);
        mpb.SetFloatArray(SpeedID, speed);
        // SimTime set in Update
        targetRenderer.SetPropertyBlock(mpb);
    }
}