using UnityEngine;

/// <summary>
/// Manages the ocean plane position and wave calculations.
/// This will eventually sync with the shader for physics.
/// </summary>
public class OceanManager : MonoBehaviour
{
    [Header("Ocean Plane Settings")]
    [SerializeField] private Transform oceanPlane;
    [SerializeField] private Transform followTarget; // Usually the main camera
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float yOffset = 0f; // Sea level height
    
    [Header("Wave Settings - Will sync with shader")]
    [SerializeField] private int waveCount = 4;
    [SerializeField] private float waveScale = 1f;
    
    private void Start()
    {
        // If no target set, use main camera
        if (followTarget == null)
        {
            followTarget = Camera.main.transform;
        }
        
        // Set initial position
        if (oceanPlane != null)
        {
            Vector3 pos = followTarget.position;
            oceanPlane.position = new Vector3(pos.x, yOffset, pos.z);
        }
    }
    
    private void LateUpdate()
    {
        // Make ocean plane follow the target (camera/player ship)
        if (oceanPlane != null && followTarget != null)
        {
            Vector3 targetPos = followTarget.position;
            Vector3 currentPos = oceanPlane.position;
            
            // Smoothly move ocean to follow target (only X and Z, Y stays at sea level)
            Vector3 newPos = new Vector3(
                Mathf.Lerp(currentPos.x, targetPos.x, followSpeed * Time.deltaTime),
                yOffset,
                Mathf.Lerp(currentPos.z, targetPos.z, followSpeed * Time.deltaTime)
            );
            
            oceanPlane.position = newPos;
        }
    }
    
    // This will eventually return wave height at any world position
    public float GetWaterHeight(Vector3 worldPosition)
    {
        // For now, just return sea level
        // We'll add Gerstner wave calculations here in Phase 4
        return yOffset;
    }
}