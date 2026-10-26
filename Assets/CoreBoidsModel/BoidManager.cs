// BoidManager.cs
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    [SerializeField] BoidSettings settings;
    [SerializeField] Boid prefab;
    [SerializeField, Min(0)] int count = 60;
    [SerializeField] int seed = 0;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(30f, 15f, 30f));

    readonly List<Boid> boids = new List<Boid>();
    readonly List<Boid> neighbours = new List<Boid>();   // reused for every boid: read it, don't keep it
    Vector3[] steering;

    void Start()
    {
        // One seeded generator drawn in a fixed order, so the same seed always gives the same flock.
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)rng.NextDouble()),
                settings.planar ? bounds.center.y
                                : Mathf.Lerp(bounds.min.y, bounds.max.y, (float)rng.NextDouble()),
                Mathf.Lerp(bounds.min.z, bounds.max.z, (float)rng.NextDouble()));

            Vector3 dir = RandomDirection(rng);
            float speed = Mathf.Lerp(settings.minSpeed, settings.maxSpeed, (float)rng.NextDouble());

            Boid b = Instantiate(prefab, pos, Quaternion.identity, transform);
            b.name = $"Boid_{i:000}";
            b.Init(settings, dir * speed);
            boids.Add(b);
        }
        steering = new Vector3[boids.Count];
    }

    Vector3 RandomDirection(System.Random rng)
    {
        float a = (float)(rng.NextDouble() * 2.0 * System.Math.PI);
        if (settings.planar) return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

        float y = (float)(rng.NextDouble() * 2.0 - 1.0);   // uniform over the sphere
        float r = Mathf.Sqrt(1f - y * y);
        return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Pass 1: every boid decides from the same snapshot of the flock.
        for (int i = 0; i < boids.Count; i++)
        {
            FindNeighbours(boids[i]);
            steering[i] = boids[i].ComputeSteering(neighbours, bounds);
        }

        // Pass 2: then they all move, so update order doesn't matter.
        for (int i = 0; i < boids.Count; i++)
            boids[i].Apply(steering[i], dt);
    }

    // Reynolds' neighbourhood: within a radius and inside the field of view.
    // Brute force, O(n²): fine for a few hundred boids. Swap in a spatial grid for more.
    void FindNeighbours(Boid self)
    {
        neighbours.Clear();
        float r2 = settings.neighbourRadius * settings.neighbourRadius;
        float cosHalf = Mathf.Cos(settings.viewAngleDeg * 0.5f * Mathf.Deg2Rad);
        bool useAngle = settings.viewAngleDeg < 360f;
        Vector3 pos = self.transform.position;
        Vector3 heading = self.Heading;

        foreach (Boid other in boids)
        {
            if (ReferenceEquals(other, self)) continue;
            Vector3 offset = other.transform.position - pos;
            float d2 = offset.sqrMagnitude;
            if (d2 > r2) continue;
            if (useAngle && Vector3.Dot(heading, offset) < cosHalf * Mathf.Sqrt(d2)) continue;   // in the blind spot
            neighbours.Add(other);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}