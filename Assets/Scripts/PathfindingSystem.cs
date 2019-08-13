using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Pathfinding
{ 
    public class PathfindingSystem : JobComponentSystem
    {
        NativeArray<Neighbour> neighbours;

        //EndSimulationEntityCommandBufferSystem entityCommandBuffer;
        //EntityQuery gridQuery;

        EntityQuery pathRequests;

        const int IterationLimit = 1000;
        public int2 worldSize;

        protected override void OnCreate()
        {
            //Moved to manual for fast grid resizing test

            //entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            pathRequests = GetEntityQuery(typeof(Waypoint), ComponentType.ReadOnly<PathRequest>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<NavigationCapabilities>());
            pathRequests.SetFilterChanged(typeof(PathRequest));
            //gridQuery = GetEntityQuery(ComponentType.ReadOnly<Cell>());

            neighbours = new NativeArray<Neighbour>(8, Allocator.Persistent)
            {
                [0] = new Neighbour(-1, -1), // Bottom left
                [1] = new Neighbour(0, -1), // Bottom
                [2] = new Neighbour(1, -1), // Bottom Right
                [3] = new Neighbour(-1, 0), // Left
                [4] = new Neighbour(1, 0), // Right
                [5] = new Neighbour(-1, 1), // Top Left
                [6] = new Neighbour(0, 1), // Top
                [7] = new Neighbour(1, 1), // Top Right
            };
        }

        protected override void OnDestroy()
        {
            neighbours.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {            
            int numberOfRequests = pathRequests.CalculateChunkCount();
            if (numberOfRequests == 0) return inputDeps;

            //NativeArray<ArchetypeChunk> gridChunks = gridQuery.CreateArchetypeChunkArray(Allocator.TempJob);

            //Schedule the findPath to build <Waypoints> Job
            FindPathJobChunk findPathJob = new FindPathJobChunk()
            {
                WaypointChunkBuffer = GetArchetypeChunkBufferType<Waypoint>(false),
                PathRequestsChunkComponent = GetArchetypeChunkComponentType<PathRequest>(true),
                //GridChunks = gridChunks,
                //CellTypeRO = GetArchetypeChunkBufferType<Cell>(true),
                CellArray = RequiredExtensions.cells,
                TranslationsChunkComponent = GetArchetypeChunkComponentType<Translation>(true),
                NavigationCapabilitiesChunkComponent = GetArchetypeChunkComponentType<NavigationCapabilities>(true),
                Neighbors = neighbours,
                DimY = worldSize.y,
                DimX = worldSize.x,
                Iterations = IterationLimit,
                NeighborCount = neighbours.Length
            };
            JobHandle jobHandle = findPathJob.Schedule(pathRequests, inputDeps);

            //Schedule the remove <PathSearcher> Job
            //RemoveComponentJob removeComponentJob = new RemoveComponentJob()
            //{
            //    entityCommandBuffer = entityCommandBuffer.CreateCommandBuffer().ToConcurrent(),
            //    WaypointChunkBuffer = GetArchetypeChunkBufferType<Waypoint>(true),
            //    PathRequestsChunkComponent = GetArchetypeChunkComponentType<PathRequest>(true)

            //};
            //jobHandle = removeComponentJob.Schedule(pathRequests, jobHandle);

            //entityCommandBuffer.AddJobHandleForProducer(jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct FindPathJobChunk : IJobChunk
        {
            [ReadOnly] public int DimX;
            [ReadOnly] public int DimY;
            [ReadOnly] public int Iterations;
            [ReadOnly] public int NeighborCount;

            //[ReadOnly, DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> GridChunks;
            //[ReadOnly] public ArchetypeChunkBufferType<Cell> CellTypeRO;

            [ReadOnly] public NativeArray<Cell> CellArray;

            [WriteOnly] public ArchetypeChunkBufferType<Waypoint> WaypointChunkBuffer;
            [ReadOnly] public ArchetypeChunkComponentType<PathRequest> PathRequestsChunkComponent;
            [ReadOnly] public NativeArray<Neighbour> Neighbors;
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationsChunkComponent;
            [ReadOnly] public ArchetypeChunkComponentType<NavigationCapabilities> NavigationCapabilitiesChunkComponent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                //BufferAccessor<Cell> accessor = this.GridChunks[0].GetBufferAccessor(this.CellTypeRO);
                //DynamicBuffer<Cell> grid = accessor[0].Reinterpret<Cell>();


                int size = DimX * DimY;
                BufferAccessor<Waypoint> Waypoints = chunk.GetBufferAccessor(WaypointChunkBuffer);
                NativeArray<PathRequest> PathRequests = chunk.GetNativeArray(PathRequestsChunkComponent);
                NativeArray<Translation> Translations = chunk.GetNativeArray(TranslationsChunkComponent);
                NativeArray<NavigationCapabilities> NavigationCapabilities = chunk.GetNativeArray(NavigationCapabilitiesChunkComponent);

                NativeArray<float> CostSoFar = new NativeArray<float>(size * chunk.Count, Allocator.Temp);
                NativeArray<int2> CameFrom = new NativeArray<int2>(size * chunk.Count, Allocator.Temp);
                NativeMinHeap OpenSet = new NativeMinHeap((Iterations + 1) * Neighbors.Length * chunk.Count, Allocator.Temp);

                for (int i = chunkIndex; i < chunk.Count; i++)
                {
                    NativeSlice<float> costSoFar = CostSoFar.Slice(i * size, size);
                    NativeSlice<int2> cameFrom = CameFrom.Slice(i * size, size);

                    int openSetSize = (Iterations + 1) * NeighborCount;
                    NativeMinHeap openSet = OpenSet.Slice(i * openSetSize, openSetSize);
                    PathRequest request = PathRequests[i];

                    // Clear our shared data
                    //var buffer = costSoFar.GetUnsafePtr();
                    //UnsafeUtility.MemClear(buffer, (long)costSoFar.Length * UnsafeUtility.SizeOf<float>());
                    //openSet.Clear();

                    Translation currentPosition = Translations[i];
                        NavigationCapabilities capability = NavigationCapabilities[i];

                        // cache these as they're used a lot
                        int2 start = currentPosition.Value.xy.FloorToInt();
                        int2 goal = request.end;
                        
                        

                        DynamicBuffer<float3> waypoints = Waypoints[i].Reinterpret<float3>();
                        waypoints.Clear();

                        // Special case when the start is the same point as the goal
                        if (start.Equals(goal))
                        {
                            // We just set the destination as the goal, but need to get the correct height
                            int gridIndex = this.GetIndex(goal);
                            Cell cell = CellArray[gridIndex];
                            float3 point = new float3(request.Destination.x, request.Destination.y, cell.Height);
                            waypoints.Add(point);
                            continue;
                        }

                        var stash = new InstanceStash
                        {
                            Grid = CellArray,
                            CameFrom = cameFrom,
                            CostSoFar = costSoFar,
                            OpenSet = openSet,
                            Request = request,
                            Capability = capability,
                            CurrentPosition = currentPosition,
                            Start = start,
                            Goal = goal,
                            Waypoints = waypoints,
                        };

                        if (this.ProcessPath(ref stash))
                        {
                            this.ReconstructPath(stash);
                        }
                }
                CostSoFar.Dispose();
                CameFrom.Dispose();
                OpenSet.Dispose();
            }

            static float H(float2 p0, float2 p1)
            {
                float dx = p0.x - p1.x;
                float dy = p0.y - p1.y;
                float sqr = (dx * dx) + (dy * dy);
                return math.sqrt(sqr);
            }

            bool ProcessPath(ref InstanceStash stash)
            {
                // Push the start to NativeMinHeap openSet

                float hh = H(stash.Start, stash.Goal);
                MinHeapNode head = new MinHeapNode(stash.Start, hh, hh);
                stash.OpenSet.Push(head);

                int iterations = this.Iterations;

                MinHeapNode closest = head;

                // While we still have potential nodes to explore
                while (stash.OpenSet.HasNext())
                {
                    MinHeapNode current = stash.OpenSet.Pop();

                    if (current.DistanceToGoal < closest.DistanceToGoal)
                        closest = current;

                    // Found our goal
                    if (current.Position.Equals(stash.Goal))
                        return true;

                    // Path might still be obtainable but we've run out of allowed iterations
                    if (iterations == 0)
                    {
                        if (stash.Request.NavigateToBestIfIncomplete)
                        {
                            // Return the best result we've found so far
                            // Need to update goal so we can reconstruct the shorter path
                            stash.Goal = closest.Position;
                            return true;
                        }
                        return false;
                    }

                    iterations--;

                    var initialCost = stash.CostSoFar[this.GetIndex(current.Position)];

                    var fromIndex = this.GetIndex(current.Position);

                    // Loop our potential cells - generally neighbours but could include portals
                    for (var i = 0; i < this.Neighbors.Length; i++)
                    {
                        var neighbour = this.Neighbors[i];
                        var position = current.Position + neighbour.Offset;

                        // Make sure the node isn't outside our grid
                        if (position.x < 0 || position.x >= this.DimX || position.y < 0 || position.y >= this.DimY)
                        {
                            continue;
                        }

                        var index = this.GetIndex(position);

                        // Get the cost of going to this cell
                        var cellCost = this.GetCellCost(stash.Grid, stash.Capability, fromIndex, index, neighbour, true);

                        // Infinity means the cell is un-walkable, skip it
                        if (float.IsInfinity(cellCost))
                        {
                            continue;
                        }

                        var newCost = initialCost + (neighbour.Distance * cellCost);
                        var oldCost = stash.CostSoFar[index];

                        // If we've explored this cell before and it was a better path, ignore this route
                        if (!(oldCost <= 0) && !(newCost < oldCost))
                        {
                            continue;
                        }

                        // Update the costing and best path
                        stash.CostSoFar[index] = newCost;
                        stash.CameFrom[index] = current.Position;

                        // Push the node onto our heap
                        var h = H(position, stash.Goal);
                        var expectedCost = newCost + h;
                        stash.OpenSet.Push(new MinHeapNode(position, expectedCost, h));
                    }
                }

                if (stash.Request.NavigateToNearestIfBlocked)
                {
                    stash.Goal = closest.Position;
                    return true;
                }

                // All routes have been explored without finding a route to destination
                return false;
            }

            void ReconstructPath(InstanceStash stash)
            {
                var current = stash.CameFrom[this.GetIndex(stash.Goal)];
                var from = this.GetPosition(stash.Grid, current);

                stash.Waypoints.Add(from);

                var next = this.GetPosition(stash.Grid, current);

                while (!current.Equals(stash.Start))
                {
                    current = stash.CameFrom[this.GetIndex(current)];
                    var tmp = next;
                    next = this.GetPosition(stash.Grid, current);

                    if (!this.IsWalkable(stash.Grid, from.xy, next.xy))
                    {
                        // skip
                        stash.Waypoints.Add(tmp);
                        from = tmp;
                    }
                }

                stash.Waypoints.Reverse();

                stash.Request.fufilled = true;
            }

            bool IsWalkable(NativeArray<Cell> buffer, float2 from, float2 to)
            {
                const float step = 0.25f;

                var vector = to - from;
                var length = math.length(vector);
                var unit = vector / length;
                var iterations = length / step;
                var currentCell = buffer[this.GetIndex(from.FloorToInt())];

                for (var i = 0; i < iterations; i++)
                {
                    var point = (i * step * unit) + from;

                    var index = this.GetIndex(point.FloorToInt());
                    var cell = buffer[index];
                    if (cell.Blocked)
                    {
                        return false;
                    }

                    if (cell.Height != currentCell.Height)
                    {
                        return false;
                    }
                }
                return true;
            }

            float GetCellCost(NativeArray<Cell> grid, NavigationCapabilities capabilities, int fromIndex, int toIndex, Neighbour neighbour, bool areNeighbours)
            {
                var target = grid[toIndex];
                if (target.Blocked)
                {
                    return float.PositiveInfinity;
                }

                // If we're not neighbours, then we're a portal and can just go straight there
                if (!areNeighbours)
                {
                    return 1;
                }

                var from = grid[fromIndex];

                var heightDiff = target.Height - from.Height;
                var absDiff = math.abs(heightDiff);

                // TODO Should precompute this
                var dropHeight = 0;
                var climbHeight = 0;

                if (heightDiff > 0)
                {
                    climbHeight = absDiff;
                }
                else
                {
                    dropHeight = absDiff;
                }

                var slope = math.degrees(math.atan(absDiff / neighbour.Distance));

                // TODO End precompute
                if ((capabilities.MaxClimbHeight < climbHeight || capabilities.MaxDropHeight < dropHeight) &&
                    capabilities.MaxSlopeAngle < slope)
                {
                    return float.PositiveInfinity;
                }

                return 1;
            }

            float3 GetPosition(NativeArray<Cell> grid, int2 point)
            {
                var index = this.GetIndex(point);
                var cell = grid[index];
                var fPoint = point + new float2(0.5f, 0.5f);
                return new float3(fPoint.x, fPoint.y, cell.Height);
            }

            int GetIndex(int2 i)
            {
                if (DimX > DimY)
                    return (i.y * DimY) + i.x;
                else
                    return (i.x * DimX) + i.y;
            }

            struct InstanceStash
            {
                public Translation CurrentPosition;
                public PathRequest Request;
                public NavigationCapabilities Capability;
                public DynamicBuffer<float3> Waypoints;

                public int2 Start;
                public int2 Goal;

                public NativeArray<Cell> Grid;

                public NativeSlice<float> CostSoFar;
                public NativeSlice<int2> CameFrom;
                public NativeMinHeap OpenSet;
            }
        }

        //struct RemoveComponentJob : IJobChunk
        //{
        //    public EntityCommandBuffer.Concurrent entityCommandBuffer;
        //    [ReadOnly] public ArchetypeChunkBufferType<Waypoint> WaypointChunkBuffer;
        //    [ReadOnly] public ArchetypeChunkComponentType<PathRequest> PathRequestsChunkComponent;

        //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        //    {
        //        BufferAccessor<Waypoint> Waypoints = chunk.GetBufferAccessor(WaypointChunkBuffer);
        //        NativeArray<PathRequest> PathRequests = chunk.GetNativeArray(PathRequestsChunkComponent);



        //        for (int i = 0; i < chunk.Count; i++)
        //        {
        //            if (Waypoints[i].Length > 0)
        //                entityCommandBuffer.RemoveComponent(chunkIndex + i, PathRequests[i].Entity, typeof(PathRequest));
        //        }
        //    }
        //}
    }        
}