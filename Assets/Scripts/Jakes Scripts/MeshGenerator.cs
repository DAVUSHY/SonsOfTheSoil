using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{

    [Header("Generation Settings")]
    public int worldSeed = 12345;
    public bool randomiseSeed = true;

    [Header("Terrain Settings")]

    [Min(0.001f)]
    public float noiseScale = 0.05f;

    [Min(0f)]
    public float heightMultiplier = 20f;
    
    [Min(1)]
    public int worldX = 1;

    [Min(1)]
    public int worldZ = 1;

    [Header("Canyon Settings")]
    [Min(0f)]
    public float canyonFloorHalfWidth = 5f;

    [Min(0.01f)]
    public float canyonWallWidth = 15f;

    [Header("Wider Canyon Areas")]

    [Min(0f)]
    public float extraWidth = 10f;

    [Min(0.1f)]
    public float areaSpacing = 30f;

    [Min(0.1f)]
    public float areaLength = 15f;

    public float canyonFloorHeight = -5f;

    [Header("Navigation Settings")]

    [Range(0f, 90f)]
    public float maxWalkableSlope = 30f;

    [Header("Terrain Appearance")]
    public Color grassColor = new Color(0.25f, 0.45f, 0.18f);
    public Color rockColor = new Color(0.35f, 0.35f, 0.33f);

    [Range(0f, 90f)]
    public float rockSlopeStart = 30f;

    [Range(0f, 90f)]
    public float rockSlopeEnd = 60f;

    public bool isGenerated { get; private set; }

    private CanyonPathGenerator canyonGenerator;

    

    private Mesh mesh;
    private int[] triangles;
    private Vector3[] vertices;

    private float noiseOffsetX;
    private float noiseOffsetZ;

    private List<Vector3> walkableGizmoPos = new List<Vector3>();

    private void Start()
    {

        if (randomiseSeed)
            worldSeed = Random.Range(0, int.MaxValue);

        System.Random random = new System.Random(worldSeed);

        noiseOffsetX = random.Next(-100000, 100000);
        noiseOffsetZ = random.Next(-100000, 100000);

        canyonGenerator = GetComponent<CanyonPathGenerator>();

        canyonGenerator.worldX = worldX;
        canyonGenerator.worldZ = worldZ;

        canyonGenerator.seed = worldSeed;
        canyonGenerator.GeneratePath();

        mesh = new Mesh
        {
            name = "Generated Mesh",
            indexFormat = IndexFormat.UInt32
        };

        GetComponent<MeshFilter>().mesh = mesh;

        GenerateMesh();
        UpdateMesh();
        GenerateTerrainColors();
        GenerateWalkableGizmos();

        isGenerated = true;
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
                    (x + noiseOffsetX) * noiseScale,
                    (z + noiseOffsetZ) * noiseScale
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
        float minDistance = DistanceToPath(
            point,
            canyonGenerator.SmoothPath
        );

        foreach (var branch in canyonGenerator.Branches)
        {
            float branchDistance = DistanceToPath(point, branch);

            minDistance = Mathf.Min(minDistance, branchDistance);
        }

        return minDistance;
    }

    private float CalculateCanyonHeight(
    float x,
    float z,
    float originalHeight)
    {
        float distance = DistanceToCanyon(new Vector2(x, z));

        float floorHalfWidth = GetCanyonHalfWidth(z);

        float t = Mathf.InverseLerp(
            floorHalfWidth,
            floorHalfWidth + canyonWallWidth,
            distance
        );

        float influence = 1f - Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Lerp(
            originalHeight,
            canyonFloorHeight,
            influence
        );
    }

    private float GetCanyonHalfWidth(float z)
    {
        float cycle = z % areaSpacing;

        float distanceFromCentre = Mathf.Abs(
            cycle - areaSpacing / 2f
        );

        float t = Mathf.Clamp01(
            1f - distanceFromCentre / areaLength
        );

        float expansion = Mathf.SmoothStep(0f, 1f, t);

        return canyonFloorHalfWidth + extraWidth * expansion;
    }

    private float GetSlope(int x, int z)
    {
        int verticesPerRow = worldX + 1;

        int index = z * verticesPerRow + x;

        Vector3 current = vertices[index];

        float maxSlope = 0f;

        // Check the four neighbouring vertices.
        Vector2Int[] directions =
        {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

        foreach (Vector2Int direction in directions)
        {
            int neighbourX = x + direction.x;
            int neighbourZ = z + direction.y;

            if (neighbourX < 0 || neighbourX > worldX ||
                neighbourZ < 0 || neighbourZ > worldZ)
            {
                continue;
            }

            int neighbourIndex =
                neighbourZ * verticesPerRow + neighbourX;

            Vector3 neighbour = vertices[neighbourIndex];

            float horizontalDistance = Vector2.Distance(
                new Vector2(current.x, current.z),
                new Vector2(neighbour.x, neighbour.z)
            );

            float heightDifference = Mathf.Abs(
                neighbour.y - current.y
            );

            float slope = Mathf.Atan2(
                heightDifference,
                horizontalDistance
            ) * Mathf.Rad2Deg;

            maxSlope = Mathf.Max(maxSlope, slope);
        }

        return maxSlope;
    }

    public bool IsWalkable(int x, int z)
    {
        if (vertices == null ||
            x < 0 || x > worldX ||
            z < 0 || z > worldZ)
        {
            return false;
        }

        float distance = DistanceToCanyon(
            new Vector2(x, z)
        );

        float floorHalfWidth = GetCanyonHalfWidth(z);

        bool insideCanyon = distance <= floorHalfWidth;

        float slope = GetSlope(x, z);

        bool slopeIsWalkable = slope <= maxWalkableSlope;

        return insideCanyon && slopeIsWalkable;
    }

    [Header("Debug Settings")]
    public bool showWalkableArea = false;

    private void OnDrawGizmos()
    {
        if (!showWalkableArea || walkableGizmoPos == null)
            return;

        Gizmos.color = Color.green;

        foreach (Vector3 position in walkableGizmoPos)
        {
            Gizmos.DrawSphere(
                transform.TransformPoint(position),
                1f
            );
        }
    }

    private float DistanceToPath(
    Vector2 point,
    IReadOnlyList<Vector3> path)
    {
        float minDistanceSquared = float.MaxValue;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = new Vector2(path[i].x, path[i].z);
            Vector2 b = new Vector2(path[i + 1].x, path[i + 1].z);

            Vector2 ab = b - a;

            float lengthSquared = ab.sqrMagnitude;

            if (lengthSquared < 0.0001f)
                continue;

            // Skip segments that cannot beat our current distance.
            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);
            float minY = Mathf.Min(a.y, b.y);
            float maxY = Mathf.Max(a.y, b.y);

            float dx = Mathf.Max(
                Mathf.Max(minX - point.x, 0f),
                point.x - maxX
            );

            float dy = Mathf.Max(
                Mathf.Max(minY - point.y, 0f),
                point.y - maxY
            );

            if (dx * dx + dy * dy >= minDistanceSquared)
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

    private void GenerateTerrainColors()
    {
        Color[] colors = new Color[vertices.Length];

        for (int z = 0; z <= worldZ; z++)
        {
            for (int x = 0; x <= worldX; x++)
            {
                int index = z * (worldX + 1) + x;

                float slope = GetSlope(x, z);

                float blend = Mathf.InverseLerp(
                    rockSlopeStart,
                    rockSlopeEnd,
                    slope
                );

                colors[index] = Color.Lerp(
                    grassColor,
                    rockColor,
                    blend
                );
            }
        }

        mesh.colors = colors;
    }

    private void GenerateWalkableGizmos()
    {
        walkableGizmoPos.Clear();

        for (int z = 0; z <= worldZ; z += 5)
        {
            for (int x = 0; x <= worldX; x += 5)
            {
                if (IsWalkable(x, z))
                {
                    walkableGizmoPos.Add(
                        new Vector3(x, vertices[z * (worldX + 1) + x].y, z)
                    );
                }
            }
        }
    }
}


