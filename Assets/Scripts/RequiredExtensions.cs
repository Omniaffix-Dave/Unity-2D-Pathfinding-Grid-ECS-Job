
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pathfinding
{
    public static class RequiredExtensions
    {
        public static NativeArray<Cell> cells;

        public static void Reverse<T>(this DynamicBuffer<T> buffer)
            where T : struct
        {
            var length = buffer.Length;
            var index1 = 0;

            for (var index2 = length - 1; index1 < index2; --index2)
            {
                var obj = buffer[index1];
                buffer[index1] = buffer[index2];
                buffer[index2] = obj;
                ++index1;
            }
        }

        public static int2 FloorToInt(this float2 f2) => (int2)math.floor(f2);

    }
}