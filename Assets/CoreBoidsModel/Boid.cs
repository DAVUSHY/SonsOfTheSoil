// Boid.cs
using System.Collections.Generic;
using UnityEngine;

// No Update: BoidManager every booid goes through 2 passes
public class Boid : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }
    BoidSettings settings;

    public void Init(BoidSettings s, Vector3 startVelocity)
    {
        settings = s;
        Velocity = startVelocity;
        Face(Velocity);
    }

    // direction of travel used for the fov 
    public Vector3 Heading =>
        Velocity.sqrMagnitude > 1e-6f ? Velocity.normalized : transform.forward;

    // ---------- Pass 1: decide -> read other boids, writes nothing they can see) ---------- 

    public Vector3 ComputeSteering(List<Boid> neighbours, Bounds bounds)
    {
        Vector3 pos = transform.position;
        Vector3 steer = Vector3.zero;
        steer += Separation(neighbours, pos) * settings.separationWeight;
        steer += Alignment(neighbours) * settings.alignmentWeight;
        steer += Cohesion(neighbours, pos) * settings.cohesionWeight;
        steer += StayInBounds(bounds, pos) * settings.boundsWeight;
        if (settings.planar) steer.y = 0f;
        return steer;
    }

    // Seperation 
    // steer away from flockmates that are too close, closer the flockmate the harder the push ( 1/distance ) 
    Vector3 Separation(List<Boid> neighbours, Vector3 pos) 
    {
        Vector3 away = Vector3.zero;
        float r = settings.separationRadius;
        foreach (Boid n in neighbours)
        {
            Vector3 offset = pos - n.transform.position;
            float d = offset.magnitude;
            if (d > 1e-4f && d < r) away += offset / (d * d);   // unit direction scaled by 1/d 
        }
        return SteerTowards(away);
    }

    // Alignment 
    // steer to match the average heading of flockmates.
    Vector3 Alignment(List<Boid> neighbours)
    {
        Vector3 sum = Vector3.zero;
        foreach (Boid n in neighbours) sum += n.Velocity;
        return SteerTowards(sum);   // no need for average only the direction matters 
    }

    // cohesion
    // steer towards the average position of flockmates 
    Vector3 Cohesion(List<Boid> neighbours, Vector3 pos)
    {
        if (neighbours.Count == 0) return Vector3.zero;
        Vector3 centre = Vector3.zero;
        foreach (Boid n in neighbours) centre += n.transform.position;
        centre /= neighbours.Count;
        return SteerTowards(centre - pos);
    }

    // head back towards the middle once outside the box.
    Vector3 StayInBounds(Bounds b, Vector3 pos)
    {
        if (settings.planar) pos.y = b.center.y;   // ignore height on a plane
        if (b.Contains(pos)) return Vector3.zero;
        return SteerTowards(b.center - pos);
    }

    // Reynolds inspired steering = desired velocity - current velocity capped at max force 
    Vector3 SteerTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 1e-8f) return Vector3.zero;
        Vector3 desired = direction.normalized * settings.maxSpeed;
        return Vector3.ClampMagnitude(desired - Velocity, settings.maxForce);
    }

    // ---------- Pass 2: act ----------  

    public void Apply(Vector3 steer, float dt)
    {
        Vector3 v = Velocity + Vector3.ClampMagnitude(steer, settings.maxForce) * dt;
        if (settings.planar) v.y = 0f;

        float speed = v.magnitude;
        if (speed > settings.maxSpeed)
            v *= settings.maxSpeed / speed;
        else if (speed < settings.minSpeed)
            v = (speed > 1e-4f ? v / speed : Heading) * settings.minSpeed;

        Velocity = v;
        transform.position += v * dt;
        Face(v);
    }

    void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        Vector3 p = transform.position;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p, settings.neighbourRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(p, settings.separationRadius);
        Gizmos.color = Color.white; Gizmos.DrawLine(p, p + Velocity);
    }
}