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

        Entity cellHolder;

        public int numberToSearch = 1;
        public bool searchAgain;

        public int numberOfRandomBlockedToAdd = 1;
        public bool addRandomBlocked;

        public Color pathColor;

        private void Awake()
        {
            World.Active.GetExistingSystem<PathfindingSystem>().ManualCreate(size);

            CreateGrid();
            CreateSearcher(start, end);
        }    

        private void Update()
        {
            if(addRandomBlocked)
            {
                addRandomBlocked = false;
                CreateBlockedNodes();
            }

            if(searchAgain)
            {
                searchAgain = false;

                for (int i = 0; i < numberToSearch; i++)
                {
                    CreateSearcher(new int2(Random.Range(0, 100), Random.Range(0, 100)), new int2(Random.Range(0, 100), Random.Range(0, 100)));
                }
            }
        }

        public void CreateBlockedNodes()
        {
            EntityManager entityManager = World.Active.EntityManager;
            var cells = entityManager.GetBuffer<Cell>(cellHolder);

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

        public void CreateGrid()
        {
            EntityManager entityManager = World.Active.EntityManager;
            cellHolder = entityManager.CreateEntity();
            DynamicBuffer<Cell> cellBuffer = entityManager.AddBuffer<Cell>(cellHolder);

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Cell cell = new Cell
                    {
                        blocked = Convert.ToByte(blockedNodes.Contains(new int2(x, y)))
                    };
                    cellBuffer.Add(cell);
                }
            }           
        }

        void OnDrawGizmos()
        {

            if (Application.isPlaying)
            {
                EntityManager entityManager = World.Active.EntityManager;

            var cells = entityManager.GetBuffer<Cell>(cellHolder);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Gizmos.color = cells[To1D(new int2(x,y))].Blocked ? Color.grey : Color.white;
                    Gizmos.DrawCube(NodeToWorldPosition(new int2(x, y)), new Vector3(.90f, .90f));
                }
            }

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
        public float3 NodeToWorldPosition(int2 i) => new float3(i.x, i.y, 0);
        public int To1D(int2 i) => (int)(i.x + (i.y * size.y));
    }
}