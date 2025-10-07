using UnityEngine;

public interface IWaterProvider
{
    // Returns true if sampled successfully
    bool SampleHeightAndNormal(Vector3 worldPos, out float height, out Vector3 normal);
    float GetSeaLevel();
}