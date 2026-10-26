// BoidSettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BoidSettings", menuName = "Scriptable Objects/BoidSettings")]
public class BoidSettings : ScriptableObject
{
    [Header("Perception")]
    [Min(0.01f)] public float neighbourRadius = 3f;
    [Min(0f)] public float separationRadius = 1f;         // kept inside neighbourRadius by OnValidate
    [Range(0f, 360f)] public float viewAngleDeg = 270f;   // 360 = sees all round

    [Header("Movement")]
    [Min(0.01f)] public float maxSpeed = 5f;
    [Min(0f)] public float minSpeed = 2f;                 // birds can't hover; 0 lets boids stop
    [Min(0f)] public float maxForce = 4f;

    [Header("Rule weights")]
    [Range(0f, 5f)] public float separationWeight = 1.5f;
    [Range(0f, 5f)] public float alignmentWeight = 1f;
    [Range(0f, 5f)] public float cohesionWeight = 1f;
    [Range(0f, 10f)] public float boundsWeight = 2f;      // not one of Reynolds' rules: keeps the flock in view

    [Header("Space")]
    public bool planar = false;                           // true = flock on the XZ plane, false = full 3D like birds

    void OnValidate()
    {
        separationRadius = Mathf.Min(separationRadius, neighbourRadius * 0.95f);
        minSpeed = Mathf.Min(minSpeed, maxSpeed);
    }
}