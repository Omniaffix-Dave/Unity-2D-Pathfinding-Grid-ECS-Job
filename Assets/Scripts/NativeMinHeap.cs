
namespace Pathfinding
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainer]
    public unsafe struct NativeMinHeap : IDisposable
    {
        private readonly Allocator allocator;

        [NativeDisableUnsafePtrRestriction]
        private void* buffer;

        private int capacity;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        private int head;

        private int length;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMinHeap"/> struct.
        /// </summary>
        /// <param name="capacity"> The capacity of the min heap. </param>
        /// <param name="allocator"> The allocator. </param>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown if allocator not set, capacity is negative or the size > maximum integer value. </exception>
        public NativeMinHeap(int capacity, Allocator allocator)
        {
            var size = (long)UnsafeUtility.SizeOf<MinHeapNode>() * capacity;
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Length must be >= 0");
            }

            if (size > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    $"Length * sizeof(T) cannot exceed {int.MaxValue} bytes");
            }

            this.buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<MinHeapNode>(), allocator);
            this.capacity = capacity;
            this.allocator = allocator;
            this.head = -1;
            this.length = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, 1, allocator);
#endif
        }

        /// <summary>
        /// Does the heap still have remaining nodes.
        /// </summary>
        /// <returns>
        /// True if the min heap still has at least one remaining node, otherwise false.
        /// </returns>
        public bool HasNext()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
            return this.head >= 0;
        }

        /// <summary>
        /// Add a node to the heap which will be sorted.
        /// </summary>
        /// <param name="node"> The node to add. </param>
        /// <exception cref="IndexOutOfRangeException"> Throws if capacity reached. </exception>
        public void Push(MinHeapNode node)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (this.length == this.capacity)
            {
                throw new IndexOutOfRangeException("Capacity Reached");
            }

            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
            if (this.head < 0)
            {
                this.head = this.length;
            }
            else if (node.ExpectedCost < this.Get(this.head).ExpectedCost)
            {
                node.Next = this.head;
                this.head = this.length;
            }
            else
            {
                var currentPtr = this.head;
                var current = this.Get(currentPtr);

                while (current.Next >= 0 && this.Get(current.Next).ExpectedCost <= node.ExpectedCost)
                {
                    currentPtr = current.Next;
                    current = this.Get(current.Next);
                }

                node.Next = current.Next;
                current.Next = this.length;

                UnsafeUtility.WriteArrayElement(this.buffer, currentPtr, current);
            }

            UnsafeUtility.WriteArrayElement(this.buffer, this.length, node);
            this.length += 1;
        }

        /// <summary>
        /// Take the top node off the heap.
        /// </summary>
        /// <returns>The current node of the heap.</returns>
        public MinHeapNode Pop()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            var result = this.head;
            this.head = this.Get(this.head).Next;
            return this.Get(result);
        }

        /// <summary>
        /// Clear the heap by resetting the head and length.
        /// </summary>
        /// <remarks>Does not clear memory.</remarks>
        public void Clear()
        {
            this.head = -1;
            this.length = 0;
        }

        /// <summary>
        /// Dispose of the heap by freeing up memory.
        /// </summary>
        /// <exception cref="InvalidOperationException"> Memory hasn't been allocated. </exception>
        public void Dispose()
        {
            if (!UnsafeUtility.IsValidAllocator(this.allocator))
            {
                return;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
            UnsafeUtility.Free(this.buffer, this.allocator);
            this.buffer = null;
            this.capacity = 0;
        }

        public NativeMinHeap Slice(int start, int length)
        {
            var stride = UnsafeUtility.SizeOf<MinHeapNode>();

            return new NativeMinHeap()
            {
                buffer = (byte*)((IntPtr)this.buffer + stride * start),
                capacity = length,
                length = 0,
                head = -1,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = this.m_Safety,
#endif
            };
        }

        private MinHeapNode Get(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= this.length)
            {
                this.FailOutOfRangeError(index);
            }

            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif

            return UnsafeUtility.ReadArrayElement<MinHeapNode>(this.buffer, index);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.capacity}' Length.");
        }
#endif
    }

    /// <summary>
    /// The min heap node.
    /// </summary>
    public struct MinHeapNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinHeapNode"/> struct.
        /// </summary>
        /// <param name="position"> The position. </param>
        /// <param name="expectedCost"> The expected cost. </param>
        /// <param name="distanceToGoal">Remaining distance to the goal</param>
        public MinHeapNode(int2 position, float expectedCost, float distanceToGoal)
        {
            this.Position = position;
            this.ExpectedCost = expectedCost;
            this.DistanceToGoal = distanceToGoal;
            this.Next = -1;
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public int2 Position { get; }

        /// <summary>
        /// Gets the expected cost.
        /// </summary>
        public float ExpectedCost { get; }

        /// <summary>
        /// Gets the expected cost.
        /// </summary>
        public float DistanceToGoal { get; }

        /// <summary>
        /// Gets or sets the next node in the heap.
        /// </summary>
        public int Next { get; set; }
    }
}