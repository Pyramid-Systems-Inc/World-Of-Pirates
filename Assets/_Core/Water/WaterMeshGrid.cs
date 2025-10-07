using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterMeshGrid : MonoBehaviour
{
    [Min(2)] public int segments = 200;   // segments per side
    public float size = 1000f;            // total size (meters)

    private MeshFilter mf;

    [ContextMenu("Regenerate Grid")]
    public void Regenerate()
    {
        if (!mf) mf = GetComponent<MeshFilter>();

        int vcount = (segments + 1) * (segments + 1);
        int icount = segments * segments * 6;

        var mesh = new Mesh { name = "WaterGrid" };
        mesh.indexFormat = IndexFormat.UInt32;



        var verts = new Vector3[vcount];
        var uvs = new Vector2[vcount];
        var tris = new int[icount];

        int idx = 0;
        float half = size * 0.5f;
        for (int z = 0; z <= segments; z++)
        {
            for (int x = 0; x <= segments; x++)
            {
                float fx = (float)x / segments;
                float fz = (float)z / segments;
                verts[idx] = new Vector3(Mathf.Lerp(-half, half, fx), 0f, Mathf.Lerp(-half, half, fz));
                uvs[idx] = new Vector2(fx, fz);
                idx++;
            }
        }

        int ti = 0;
        for (int z = 0; z < segments; z++)
        {
            for (int x = 0; x < segments; x++)
            {
                int i0 = z * (segments + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (segments + 1);
                int i3 = i2 + 1;

                tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1;
                tris[ti++] = i1; tris[ti++] = i2; tris[ti++] = i3;
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0, true);
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    private void OnEnable()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mf.sharedMesh) Regenerate();
    }

    private void OnValidate()
    {
        segments = Mathf.Max(2, segments);
    }
}