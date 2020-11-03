using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DirectionalPerlin
{
    public static Vector3 Directional2D(Vector2 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector2((pos.x + offset) * frequency , (pos.y + offset) * frequency + offset);
        return Perlin.PointOnUnitCircle(coord);
    }

    public static Vector3 Directional3D(Vector3 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector3((pos.x + offset) * frequency, (pos.y + offset) * frequency, (pos.z + offset) * frequency);
        return Perlin.PointOnUnitSphere(coord);
    }
}
