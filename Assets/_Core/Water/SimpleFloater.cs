using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleFloater : MonoBehaviour
{
    [SerializeField] private CrimsonWater water;
    [SerializeField] private float buoyancy = 30f;
    [SerializeField] private float alignToNormal = 2f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!water) water = CrimsonWater.Instance;
        rb.useGravity = true;
        rb.angularDrag = 1f;
        rb.drag = 0.2f;
    }

    private void FixedUpdate()
    {
        if (!water) return;

        Vector3 pos = transform.position;
        if (water.SampleHeightAndNormal(pos, out float h, out Vector3 n))
        {
            float depth = h - pos.y; // positive if below surface
            if (depth > 0f)
            {
                // Upward force proportional to submersion
                rb.AddForce(n * buoyancy * depth, ForceMode.Acceleration);

                // Align to normal
                Quaternion target = Quaternion.FromToRotation(transform.up, n) * transform.rotation;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, alignToNormal * Time.fixedDeltaTime));
            }
        }
    }
}