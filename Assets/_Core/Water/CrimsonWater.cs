using UnityEngine;

[DefaultExecutionOrder(-200)]
public class CrimsonWater : MonoBehaviour, IWaterProvider
{
    public static CrimsonWater Instance { get; private set; }

    [SerializeField] private WaterSettings settings;
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private int gizmoGrid = 16;
    [SerializeField] private float gizmoSpacing = 2f;
    [SerializeField] private Color gizmoColor = new Color(0f, 0.6f, 1f, 0.5f);

    float SimTime => (settings ? settings.timeScale : 1f) * Time.time;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!settings) Debug.LogWarning("CrimsonWater: No WaterSettings assigned.");
        DontDestroyOnLoad(gameObject);
    }

    public bool SampleHeightAndNormal(Vector3 worldPos, out float height, out Vector3 normal)
    {
        if (!settings)
        {
            height = 0f; normal = Vector3.up;
            return false;
        }

        WaterMath.SampleGerstnerSum(settings, worldPos, SimTime, out height, out normal);
        return true;
    }

    public float GetSeaLevel() => settings ? settings.seaLevel : 0f;

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !settings) return;

        Gizmos.color = gizmoColor;
        // Draw a small grid around this object position to visualize the surface
        Vector3 origin = transform.position;
        for (int x = -gizmoGrid; x <= gizmoGrid; x++)
        {
            for (int z = -gizmoGrid; z <= gizmoGrid; z++)
            {
                Vector3 p = origin + new Vector3(x * gizmoSpacing, 0f, z * gizmoSpacing);
                SampleHeightAndNormal(p, out float h, out Vector3 n);
                Vector3 ph = new Vector3(p.x, h, p.z);
                Gizmos.DrawSphere(ph, 0.05f);
                // small normal line
                Gizmos.DrawLine(ph, ph + n * 0.5f);
            }
        }
    }
}