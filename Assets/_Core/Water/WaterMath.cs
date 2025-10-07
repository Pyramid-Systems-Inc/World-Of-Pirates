using UnityEngine;

public static class WaterMath
{
    const float TWO_PI = Mathf.PI * 2f;

    // Deep-water dispersion relation:
    // omega = sqrt(g * k), k = 2pi / lambda
    public static void SampleGerstnerSum(WaterSettings settings, Vector3 worldPos, float time,
        out float height, out Vector3 normal)
    {
        float y = 0f;
        Vector2 grad = Vector2.zero;

        foreach (var w in settings.waves)
        {
            if (w.wavelength < 0.1f) continue;

            float k = TWO_PI / w.wavelength;
            float omega = Mathf.Sqrt(settings.gravity * k) * Mathf.Max(0.001f, w.speed);
            float dirRad = w.directionDegrees * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(dirRad), Mathf.Sin(dirRad)); // XZ plane dir

            float phase = k * (d.x * worldPos.x + d.y * worldPos.z) - omega * time;

            float sinP = Mathf.Sin(phase);
            float cosP = Mathf.Cos(phase);

            y += w.amplitude * sinP;

            // Gradient of height wrt x,z (for normal)
            float a_k = w.amplitude * k;
            grad.x += a_k * d.x * cosP; // ∂y/∂x
            grad.y += a_k * d.y * cosP; // ∂y/∂z
        }

        height = settings.seaLevel + y;

        // Normal from gradient: n = normalize(-∂y/∂x, 1, -∂y/∂z)
        Vector3 n = new Vector3(-grad.x, 1f, -grad.y).normalized;
        normal = n;
    }
}