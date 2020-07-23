using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Boids_ECS
{
    public struct Position : IComponentData
    {
        public float x, y, z;
    }
}