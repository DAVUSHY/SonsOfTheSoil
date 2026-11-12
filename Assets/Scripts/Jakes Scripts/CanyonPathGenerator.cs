using UnityEngine;
using System.Collections.Generic;

public class CanyonPathGenerator : MonoBehaviour
{
    [Header("Path Settings")]
    public int worldX = 100;
    public int worldZ = 100;

    [Min(2)]
    public int controlPointCount = 8;

    [Range(0f, 0.4f)]
    public float windingAmount = 0.3f;

    public int seed = 12345;

    private List<Vector3> pathPoints = new List<Vector3>();
    private List<Vector3> smoothPath = new List<Vector3>();
    public IReadOnlyList<Vector3> SmoothPath => smoothPath;

    public void GeneratePath()
    {
        pathPoints.Clear();

        System.Random random = new System.Random(seed);

        for (int i = 0; i < controlPointCount; i++)
        {
            float t = (float)i / (controlPointCount - 1);

            float z = t * worldZ;

            float randomOffset =
                ((float)random.NextDouble() * 2f - 1f)
                * worldX * windingAmount;

            float x = worldX / 2f + randomOffset;

            pathPoints.Add(new Vector3(x, 0f, z));
        }
        GenSmoothPath();
    }

    private void OnDrawGizmos()
    {
        if (smoothPath == null || smoothPath.Count < 2)
            return;

        Gizmos.color = Color.red;

        float previewHeight = 20f;

        for (int i = 0; i < smoothPath.Count - 1; i++)
        {
            Vector3 start = smoothPath[i];
            Vector3 end = smoothPath[i + 1];

            start.y = previewHeight;
            end.y = previewHeight;

            Gizmos.DrawLine(
                transform.TransformPoint(start),
                transform.TransformPoint(end)
            );
        }

        foreach (Vector3 point in smoothPath)
        {
            Vector3 previewPoint = point;
            previewPoint.y = previewHeight;

            Gizmos.DrawSphere(
                transform.TransformPoint(previewPoint),
                0.8f
            );
        }
    }

    private Vector3 CatmullRom(
    Vector3 p0,
    Vector3 p1,
    Vector3 p2,
    Vector3 p3,
    float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void GenSmoothPath()
    {
        smoothPath.Clear();

        int resolution = 10;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 p0 = pathPoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pathPoints[i];
            Vector3 p2 = pathPoints[i + 1];
            Vector3 p3 = pathPoints[Mathf.Min(i + 2, pathPoints.Count - 1)];

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;

                Vector3 point = CatmullRom(
                    p0, p1, p2, p3, t
                );

                smoothPath.Add(point);
            }
        }

        smoothPath.Add(pathPoints[pathPoints.Count - 1]);
    }
}