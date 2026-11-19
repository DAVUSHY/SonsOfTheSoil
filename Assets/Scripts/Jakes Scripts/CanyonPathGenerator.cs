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

    [Header("Branch Settings")]

    public bool generateBranches = true;

    [Min(1)]
    public int branchCount = 1;

    [Min(2)]
    public int branchPointCount = 5;

    [Min(1f)]
    public float branchLength = 60f;

    [Range(0f, 0.5f)]
    public float branchWindingAmount = 0.2f;

    [Header("Branch Reconnection")]
    [Range(0f, 1f)]
    public float reconnectionChance = 0.4f;

    private List<Vector3> pathPoints = new List<Vector3>();

    private List<Vector3> smoothPath = new List<Vector3>();

    private List<List<Vector3>> branches = new List<List<Vector3>>();

    public IReadOnlyList<List<Vector3>> Branches => branches;

    public IReadOnlyList<Vector3> SmoothPath => smoothPath;

    [Min(0f)]
    public float branchEdgeMargin = 20f;

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
        GenBranches();
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

        Gizmos.color = Color.cyan;

        foreach (List<Vector3> branch in branches)
        {
            for (int i = 0; i < branch.Count - 1; i++)
            {
                Vector3 start = branch[i];
                Vector3 end = branch[i + 1];

                start.y = previewHeight;
                end.y = previewHeight;

                Gizmos.DrawLine(
                    transform.TransformPoint(start),
                    transform.TransformPoint(end)
                );
            }
        }

        Gizmos.color = Color.yellow;

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

    private void GenBranches()
    {
        branches.Clear();

        if (!generateBranches || smoothPath.Count < 2)
            return;

        System.Random random = new System.Random(seed + 1);

        for (int branch = 0; branch < branchCount; branch++)
        {
            List<Vector3> branchPoints = new List<Vector3>();

            int startIndex = random.Next(
                smoothPath.Count / 4,
                smoothPath.Count * 3 / 4
            );

            Vector3 start = smoothPath[startIndex];

            // Find the direction of the main canyon.
            Vector3 tangent = (
                 smoothPath[startIndex + 1] -
                 smoothPath[startIndex - 1]
                 ).normalized;

            Vector3 perpendicular = new Vector3(
                -tangent.z,
                0f,
                tangent.x
            ).normalized;

            bool reconnect = random.NextDouble() < reconnectionChance;

            // Randomly choose which side the branch goes.
            if (random.Next(0, 2) == 0)
                perpendicular = -perpendicular;

            // Calculate how far we can extend before reaching the edge.
            float maxLength = GetMaxBranchLength(
                start,
                perpendicular
            );

            float actualLength = Mathf.Min(
                branchLength,
                maxLength
            );

            // Skip branches that would be too short.
            if (actualLength < 10f)
                continue;

            // A sideways direction for the branch.
            Vector3 sideways = new Vector3(
                -perpendicular.z,
                0f,
                perpendicular.x
            ).normalized;

            Vector3 end = Vector3.zero;

            if (reconnect)
            {
                // Select a later point on the main canyon.
                int endIndex = Mathf.Min(
                    startIndex + Mathf.Max(10, smoothPath.Count / 5),
                    smoothPath.Count - 1
                );

                end = smoothPath[endIndex];
            }

            for (int i = 0; i < branchPointCount; i++)
            {
                float t = i / (float)(branchPointCount - 1);

                Vector3 point;

                if (reconnect)
                {
                    // Connect two points on the main canyon.
                    point = Vector3.Lerp(start, end, t);

                    // Push the middle of the branch away from the main canyon.
                    float separation = Mathf.Sin(t * Mathf.PI);

                    point += perpendicular * branchLength * 0.5f * separation;
                }
                else
                {
                    // Existing dead-end branch behaviour.
                    point = start + perpendicular * (t * actualLength);
                }

                // Add some natural winding.
                if (i > 0 && i < branchPointCount - 1)
                {
                    float randomOffset =
                        ((float)random.NextDouble() * 2f - 1f) *
                        actualLength *
                        branchWindingAmount;

                    point += sideways *
                        randomOffset *
                        Mathf.Sin(t * Mathf.PI);
                }

                // Keep intermediate points inside the map.
                // Preserve the junction positions exactly.
                if (i > 0 && i < branchPointCount - 1)
                {
                    point.x = Mathf.Clamp(
                        point.x,
                        branchEdgeMargin,
                        worldX - branchEdgeMargin
                    );

                    point.z = Mathf.Clamp(
                        point.z,
                        branchEdgeMargin,
                        worldZ - branchEdgeMargin
                    );
                }

                branchPoints.Add(point);
            }

            // Smooth the branch.
            List<Vector3> smoothBranch = SmoothPoints(branchPoints);

            // Keep smoothed points within the map too.
            // Catmull-Rom can overshoot between control points.
            for (int i = 1; i < smoothBranch.Count - 1; i++)
            {
                Vector3 point = smoothBranch[i];

                point.x = Mathf.Clamp(
                    point.x,
                    branchEdgeMargin,
                    worldX - branchEdgeMargin
                );

                point.z = Mathf.Clamp(
                    point.z,
                    branchEdgeMargin,
                    worldZ - branchEdgeMargin
                );

                smoothBranch[i] = point;
            }

            branches.Add(smoothBranch);
        }
    }

    private float GetMaxBranchLength(
    Vector3 start,
    Vector3 direction)
    {
        float maxLength = float.MaxValue;

        float minX = branchEdgeMargin;
        float maxX = worldX - branchEdgeMargin;

        float minZ = branchEdgeMargin;
        float maxZ = worldZ - branchEdgeMargin;

        if (direction.x > 0f)
        {
            maxLength = Mathf.Min(
                maxLength,
                (maxX - start.x) / direction.x
            );
        }
        else if (direction.x < 0f)
        {
            maxLength = Mathf.Min(
                maxLength,
                (minX - start.x) / direction.x
            );
        }

        if (direction.z > 0f)
        {
            maxLength = Mathf.Min(
                maxLength,
                (maxZ - start.z) / direction.z
            );
        }
        else if (direction.z < 0f)
        {
            maxLength = Mathf.Min(
                maxLength,
                (minZ - start.z) / direction.z
            );
        }

        return Mathf.Max(0f, maxLength);
    }

    private List<Vector3> SmoothPoints(List<Vector3> points)
    {
        List<Vector3> result = new List<Vector3>();

        if (points.Count < 2)
            return result;

        int resolution = 10;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[Mathf.Max(i - 1, 0)];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;

                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        result.Add(points[points.Count - 1]);

        return result;
    }
}