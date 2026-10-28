using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    [Min(1)]
    public int worldX = 1;

    [Min(1)]
    public int worldZ = 1;

    private Mesh mesh;
    private int[] triangles;
    private Vector3[] vertices;

    private void Start()
    {
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
                vertices[vertexIndex] = new Vector3(x, 0f, z);
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
}
