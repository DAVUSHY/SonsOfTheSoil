
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshGenerator))]
public class CanWalk : MonoBehaviour
{
    private MeshGenerator meshGenerator;

    private static readonly Vector2Int[] directions =
    {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),

        new Vector2Int( 1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int(-1,  1),
        new Vector2Int(-1, -1)
    };

    private void Start()
    {
        meshGenerator = GetComponent<MeshGenerator>();
        StartCoroutine(ValidateWhenReady());
    }

    private System.Collections.IEnumerator ValidateWhenReady()
    {
        yield return new WaitUntil(() => meshGenerator.isGenerated);

        ValidateConnectivity();
    }

    public void ValidateConnectivity()
    {
        int width = meshGenerator.worldX + 1;
        int height = meshGenerator.worldZ + 1;

        bool[,] walkable = new bool[width, height];
        bool[,] visited = new bool[width, height];

        int totalWalkable = 0;
        Vector2Int start = new Vector2Int(-1, -1);

        // Cache walkability once per vertex.
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                walkable[x, z] = meshGenerator.IsWalkable(x, z);

                if (!walkable[x, z])
                    continue;

                totalWalkable++;

                if (start.x == -1)
                    start = new Vector2Int(x, z);
            }
        }

        if (totalWalkable == 0)
        {
            Debug.LogWarning("No walkable terrain found!");
            return;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        int reachable = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            reachable++;

            foreach (Vector2Int direction in directions)
            {
                int nx = current.x + direction.x;
                int nz = current.y + direction.y;

                if (nx < 0 || nx >= width ||
                    nz < 0 || nz >= height)
                    continue;

                if (!walkable[nx, nz] || visited[nx, nz])
                    continue;

                // Prevent diagonal movement through blocked corners.
                if (direction.x != 0 && direction.y != 0)
                {
                    int sideX = current.x + direction.x;
                    int sideZ = current.y + direction.y;

                    if (!walkable[sideX, current.y] ||
                        !walkable[current.x, sideZ])
                        continue;
                }

                visited[nx, nz] = true;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }

        bool fullyConnected = reachable == totalWalkable;

        if (fullyConnected)
        {
            Debug.Log(
                $"Canyon connectivity: PASS | " +
                $"Walkable: {totalWalkable} | " +
                $"Reachable: {reachable}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"Canyon connectivity: FAIL | " +
                $"Walkable: {totalWalkable} | " +
                $"Reachable: {reachable} | " +
                $"Disconnected: {totalWalkable - reachable}"
            );
        }
    }
}

