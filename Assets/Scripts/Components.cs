using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using System;

namespace Pathfinding
{
    public struct PathRequest : IComponentData
    {
        public Entity Entity;

        public int2 start;
        public int2 end;
        public float3 Destination;
        public bool NavigateToNearestIfBlocked;
        public bool NavigateToBestIfIncomplete;

        public bool fufilled;

    }

    public struct Neighbour
    {
        public readonly float Distance;
        public readonly int2 Offset;

        public Neighbour(int x, int y)
        {
            if (x < -1 || x > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Parameter {nameof(x)} cannot have a magnitude larger than one");
            }

            if (y < -1 || y > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    $"Parameter {nameof(y)} cannot have a magnitude larger than one");
            }

            if (x == 0 && y == 0)
            {
                throw new ArgumentException(
                    nameof(y),
                    $"Paramters {nameof(x)} and {nameof(y)} cannot both be zero");
            }

            this.Offset = new int2(x, y);

            // Penalize diagonal movement
            this.Distance = x != 0 && y != 0 ? 1.41421f : 1;
        }
    }

    [Serializable]
    public struct NavigationCapabilities : IComponentData
    {
        public float MaxSlopeAngle;
        public float MaxClimbHeight;
        public float MaxDropHeight;
    }

    public struct Waypoint : IBufferElementData
    {
        public float3 waypoints;
    }
    public struct Node
    {
        public bool Obstacle
        {
            get { return Convert.ToBoolean(obstacle); }
        }
        public int Height;
        public byte obstacle;
    }
}
