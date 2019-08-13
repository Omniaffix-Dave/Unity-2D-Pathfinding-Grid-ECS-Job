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
        public int2 start;
        public int2 end;
        public List<int2> blockedNodes;
        public int2 size;
        int previousSize;
        public bool manualSearch;
        public int numberToSearch = 1;
        public bool searchAgain;
        public int numberOfRandomBlockedToAdd = 1;
        public bool addRandomBlocked;
        public bool ShowGrid;
        public bool ShowPaths;
        public Color pathColor;

        private void Awake()
        {
            CreateGrid();
        }    

        private void Update()
        {
            if(size.x * size.y != previousSize)
            {
                CreateGrid();
            }

            if(manualSearch)
            {
                CreateSearcher(start, end);
                manualSearch = false;
            }

            if (addRandomBlocked)
            {
                addRandomBlocked = false;
                CreateBlockedNodes();
            }

            if(searchAgain)
            {
                searchAgain = false;

                for (int i = 0; i < numberToSearch; i++)
                {
                    CreateSearcher(new int2(Random.Range(0, size.x), Random.Range(0, size.x)), new int2(Random.Range(0, size.x), Random.Range(0, size.x)));
                }
            }
        }

        public void CreateBlockedNodes()
        {
            EntityManager entityManager = World.Active.EntityManager;
            var cells = RequiredExtensions.cells;

            for (int i = 0; i < numberOfRandomBlockedToAdd; i++)
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
            if (!ShowGrid && !ShowPaths) return;

            if (Application.isPlaying)
            {
                if (ShowGrid && size.x * size.y == previousSize)
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

                if (ShowPaths)
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
            if (size.x > size.y)
                return (i.y * size.y) + i.x;
            else
                return (i.x * size.x) + i.y;
        }
    }
}