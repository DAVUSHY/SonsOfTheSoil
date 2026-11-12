using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{

    [Header("Terrain Settings")]

    [Min(0.001f)]
    public float noiseScale = 0.05f;

    [Min(0f)]
    public float heightMultiplier = 20f;

    [Header("Canyon Settings")]
    [Min(0f)]
    public float canyonFloorHalfWidth = 5f;

    [Min(0.01f)]
    public float canyonWallWidth = 15f;

    public float canyonFloorHeight = -5f;

    private CanyonPathGenerator canyonGenerator;

    [Min(1)]
    public int worldX = 1;

    [Min(1)]
    public int worldZ = 1;

    private Mesh mesh;
    private int[] triangles;
    private Vector3[] vertices;

    private void Start()
    {
        canyonGenerator = GetComponent<CanyonPathGenerator>();

        canyonGenerator.GeneratePath();

        mesh = new Mesh
        {
            name = "Generated Mesh"
        };

        GetComponent<MeshFilter>().mesh = mesh;

        GenerateMesh();
        UpdateMesh();
    }

    private void GenerateMesh()
    {
        int verticesPerRow = worldX + 1;

        triangles = new int[worldX * worldZ * 6];
        vertices = new Vector3[(worldX + 1) * (worldZ + 1)];

        int vertexIndex = 0;

        // Generate every grid vertex, including the final row and column.
        for (int z = 0; z <= worldZ; z++)
        {
            for (int x = 0; x <= worldX; x++)
            {
                float noise = Mathf.PerlinNoise(
                    x * noiseScale,
                    z * noiseScale
                );

                float originalHeight = noise * heightMultiplier;

                float finalHeight = CalculateCanyonHeight(
                    x,
                    z,
                    originalHeight
                );

                vertices[vertexIndex] = new Vector3(
                    x,
                    finalHeight,
                    z
                );

                vertexIndex++;
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < worldZ; z++)
        {
            for (int x = 0; x < worldX; x++)
            {
                int bottomLeft = z * verticesPerRow + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + verticesPerRow;
                int topRight = topLeft + 1;

                // First triangle.
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                // Second triangle.
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        // Vertices must be assigned before triangle indices.
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private float DistanceToCanyon(Vector2 point)
    {
        var path = canyonGenerator.SmoothPath;

        float minDistanceSquared = float.MaxValue;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = new Vector2(path[i].x, path[i].z);
            Vector2 b = new Vector2(path[i + 1].x, path[i + 1].z);

            Vector2 ab = b - a;

            float lengthSquared = ab.sqrMagnitude;

            if (lengthSquared < 0.0001f)
                continue;

            float t = Mathf.Clamp01(
                Vector2.Dot(point - a, ab) / lengthSquared
            );

            Vector2 closest = a + ab * t;

            float distanceSquared = (point - closest).sqrMagnitude;

            minDistanceSquared = Mathf.Min(
                minDistanceSquared,
                distanceSquared
            );
        }

        return Mathf.Sqrt(minDistanceSquared);
    }

    private float CalculateCanyonHeight(
    float x,
    float z,
    float originalHeight)
    {
        float distance = DistanceToCanyon(new Vector2(x, z));

        float t = Mathf.InverseLerp(
     canyonFloorHalfWidth,
     canyonFloorHalfWidth + canyonWallWidth,
     distance
 );

        float influence = 1f - Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Lerp(
            originalHeight,
            canyonFloorHeight,
            influence
        );
    }
}
