using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pathfinding
{
    public class InitializePathfinding : MonoBehaviour
    {
        //Options
        public int2 size;
        public bool showGrid;
        public bool showPaths;
        public bool canMoveDiag;
        public Color pathColor;

        //Manual Path
        [HideInInspector] public Vector2Int start;
        [HideInInspector] public Vector2Int end;
        [HideInInspector] public bool searchManualPath;


        //Random Path
        [HideInInspector] public int numOfRandomPaths;
        [HideInInspector] public bool searchRandomPaths;

        //Manual blocked
        List<int2> blockedNodes = new List<int2>();
        [HideInInspector] public Vector2Int blockedNode;
        [HideInInspector] public bool addManualBlockedNode;

        //Random blocked
        [HideInInspector] public int numbOfRandomBlockedNodes = 1;
        [HideInInspector] public bool addRandomBlockedNode;

        int previousSize; // Prevent GUI Errors

        private void Awake()
        {
            World.Active.GetExistingSystem<PathfindingSystem>().canMoveDiag = canMoveDiag;
            CreateGrid();
        }    

        private void Update()
        {
            if(size.x * size.y != previousSize)
            {
                CreateGrid();
            }

            if(addManualBlockedNode)
            {
                blockedNodes.Add(new int2(blockedNode.x, blockedNode.y));
                addManualBlockedNode = false;
                CreateGrid();
            }

            if(searchManualPath)
            {
                CreateSearcher(new int2(start.x, start.y), new int2(end.x, end.y));
                searchManualPath = false;
            }

            if (addRandomBlockedNode)
            {
                addRandomBlockedNode = false;
                CreateBlockedNodes();
            }

            if(searchRandomPaths)
            {
                searchRandomPaths = false;

                for (int i = 0; i < numOfRandomPaths; i++)
                {
                    CreateSearcher(new int2(Random.Range(0, size.x), Random.Range(0, size.y)), new int2(Random.Range(0, size.x), Random.Range(0, size.y)));
                }
            }
        }

        public void CreateBlockedNodes()
        {
            EntityManager entityManager = World.Active.EntityManager;
            var cells = RequiredExtensions.cells;

            for (int i = 0; i < numbOfRandomBlockedNodes; i++)
            {
                int randomCell = Random.Range(0, size.x * size.y);

                cells[randomCell] = new Cell { blocked = Convert.ToByte(true), Height = 0 };
            }
        }

        public void CreateSearcher(int2 s, int2 e)
        {
            EntityManager entityManager = World.Active.EntityManager;
            var pathSearcher = entityManager.CreateEntity(typeof(PathRequest), typeof(Translation), typeof(NavigationCapabilities));
            var trans = entityManager.GetComponentData<Translation>(pathSearcher);
            trans.Value = new float3(s.x, s.y, 0);
            entityManager.SetComponentData<Translation>(pathSearcher, trans);
            
            entityManager.AddBuffer<Waypoint>(pathSearcher);
            PathRequest pathRequest = new PathRequest
            {
                Entity = pathSearcher,
                start = s,
                end = e,
                Destination = NodeToWorldPosition(e),
                NavigateToBestIfIncomplete = true,
                NavigateToNearestIfBlocked = true
            };

            entityManager.SetComponentData(pathSearcher, pathRequest);
        }

        private void OnDisable()
        {
            RequiredExtensions.cells.Dispose();
        }

        public void CreateGrid()
        {
            if(RequiredExtensions.cells.IsCreated)
                RequiredExtensions.cells.Dispose();
            RequiredExtensions.cells = new Unity.Collections.NativeArray<Cell>(size.x * size.y, Unity.Collections.Allocator.Persistent);

            previousSize = size.x * size.y;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Cell cell = new Cell
                    {
                        blocked = Convert.ToByte(blockedNodes.Contains(new int2(x, y)))
                    };
                    RequiredExtensions.cells[GetIndex(new int2(x, y))] = cell;
                }
            }
            World.Active.GetExistingSystem<PathfindingSystem>().worldSize = size;
        }

        void OnDrawGizmos()
        {
            if (!showGrid && !showPaths) return;

            if (Application.isPlaying)
            {
                if (showGrid && size.x * size.y == previousSize)
                {
                    var cells = RequiredExtensions.cells;

                    for (int x = 0; x < size.x; x++)
                    {
                        for (int y = 0; y < size.y; y++)
                        {
                            Gizmos.color = cells[GetIndex(new int2(x, y))].Blocked ? Color.grey : Color.white;
                            Gizmos.DrawCube(NodeToWorldPosition(new int2(x, y)), new Vector3(.90f, .90f));
                        }
                    }
                }

                if (showPaths)
                {
                    EntityManager entityManager = World.Active.EntityManager;
                    var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Waypoint>());
                    if (query.CalculateEntityCount() > 0)
                    {
                        var actualGroup = query.ToEntityArray(Unity.Collections.Allocator.TempJob);

                        foreach (Entity entity in actualGroup)
                        {
                            var buffer = entityManager.GetBuffer<Waypoint>(entity);

                            if (buffer.Length > 0)
                            {
                                Gizmos.color = pathColor;

                                for (int i = 0; i < buffer.Length - 1; i++)
                                {
                                    Gizmos.DrawLine(buffer[i].waypoints - .5f, buffer[i + 1].waypoints - .5f);
                                }
                            }
                        }
                        actualGroup.Dispose();
                    }
                }                
            }                        
        }

        public float3 NodeToWorldPosition(int2 i) => new float3(i.x, i.y, 0);

        int GetIndex(int2 i)
        {
            if (size.y >= size.x)
                return (i.x * size.y) + i.y;
            else
                return (i.y * size.x) + i.x;
        }
    }
}