using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Pathfinding
{
    [RequireComponent(typeof(PathRenderer))]
    public class InitializePathfinding : MonoBehaviour
    {
        //Options
        public int2 size;
        public VisualMode visualMode;
        [Space(5)]
        public bool showGrid;
        public bool showPaths;
        public bool canMoveDiag;
        [Space(10)]
        public Color gizmoPathColor;

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

        //Instancing settings
        [HideInInspector] public Material instanceCellMaterial;
        [HideInInspector] public Material instanceCellBlockedMaterial;
        [HideInInspector] public Mesh instancedMeshWalkable;
        [HideInInspector] public Mesh instancedMeshBlocked;
        [HideInInspector] public float instancedSpacing = 1.1f;
        [HideInInspector] public float instancedScale = 1;

        private const int instancesLimit = 1023; //DrawMeshInstanced can only draw a maximum of 1023 instances at once.
        private Matrix4x4[][] matricesWalkable;
        private Matrix4x4[][] matricesBlocked;

        private PathRenderer pathRenderer;
        
        int previousSize; // Prevent GUI Errors
        public enum VisualMode { Gizmo, Instancing, Both };


        private void Awake()
        {
            World.Active.GetExistingSystem<PathfindingSystem>().canMoveDiag = canMoveDiag;
            CreateGrid();
            
            pathRenderer = GetComponent<PathRenderer>();
            addRandomBlockedNode = true;
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
                start.x = Mathf.Clamp(start.x, 0, size.x);
                end.x   = Mathf.Clamp(end.x, 0, size.x);
                
                start.y = Mathf.Clamp(start.y, 0, size.y);
                end.y   = Mathf.Clamp(end.y, 0, size.y);
                
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

            //Instancing.
            if (visualMode == VisualMode.Instancing || visualMode == VisualMode.Both)
            {
                if (showGrid) VisualizeGrid();
            }
        }
        
        //Display grid info with instancing.
        private void VisualizeGrid()
        {
            for (int i = 0; i < matricesWalkable.Length; i++)
            {
                Graphics.DrawMeshInstanced(instancedMeshWalkable, 0, instanceCellMaterial, 
                    matricesWalkable[i], matricesWalkable[i].Length);
            }
            
            for (int i = 0; i < matricesBlocked.Length; i++)
            {
                Graphics.DrawMeshInstanced(instancedMeshBlocked, 0, instanceCellBlockedMaterial, 
                    matricesBlocked[i], matricesBlocked[i].Length);
            }
        }
        
        //Update transform data for instances and split it. Must be called every time grid is changed.
        public void InitMatrices()
        {
            var cells = RequiredExtensions.cells;
            //Initially it's unknown how much of blocked cells there is, so let's just count them,
            //so we could render them separately later.
            List<Queue<Matrix4x4>> queueMatricesWalkable = new List<Queue<Matrix4x4>>();
            List<Queue<Matrix4x4>> queueMatricesBlocked = new List<Queue<Matrix4x4>>();

            int walkableCount = 0;
            int walkableListIndex = 0;
            int blockedCount = 0;
            int blockedListIndex = 0;
                
            queueMatricesWalkable.Add(new Queue<Matrix4x4>());
            queueMatricesBlocked.Add(new Queue<Matrix4x4>());
            
            Vector3 position   = new Vector3(0, 0, 0);
            Vector3 scale      = new Vector3(instancedScale, instancedScale, instancedScale);

            float spacing = (instancedSpacing * instancedScale); //Make cell spacing relative to established in inspector scale.
            
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                position.x = x * spacing;
                position.y = y * spacing;
                position.z = 0;

                var blocked = cells[GetIndex(new int2(x, y))].Blocked;

                //Add a matrix to the queue at corresponding list, since cells are use different materials marking it's type.
                if (!blocked)
                {
                    queueMatricesWalkable[walkableListIndex].Enqueue(Matrix4x4.TRS(position, Quaternion.identity, scale));
                    walkableCount++;

                    //If we get to the limit, then add and use next queue.
                    if (walkableCount >= instancesLimit)
                    {
                        walkableCount = 0; 
                        walkableListIndex++;
                        queueMatricesWalkable.Add(new Queue<Matrix4x4>());
                    }
                }
                else
                {
                    queueMatricesBlocked[blockedListIndex].Enqueue(Matrix4x4.TRS(position, Quaternion.identity, scale));
                    blockedCount++;

                    if (blockedCount >= instancesLimit)
                    {
                        blockedCount = 0;
                        blockedListIndex++;
                        queueMatricesBlocked.Add(new Queue<Matrix4x4>());
                    }
                }
            }
            
            //Finally convert everything to the array of arrays.
            walkableListIndex++; 
            blockedListIndex++;
            matricesWalkable = new Matrix4x4[walkableListIndex][];
            matricesBlocked = new Matrix4x4[blockedListIndex][];
            
            for (int i = 0; i < walkableListIndex; i++)    matricesWalkable[i] = queueMatricesWalkable[i].ToArray();
            for (int i = 0; i < blockedListIndex; i++)    matricesBlocked[i] = queueMatricesBlocked[i].ToArray();
            
        }

        //Display existing paths with provided line renderer.
        private void VisualizePaths()
        {
            EntityManager entityManager = World.Active.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Waypoint>());
            if (query.CalculateEntityCount() > 0)
            {
                var actualGroup = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
                pathRenderer.Clear();
                
                float spacing = (instancedSpacing * instancedScale); //Make cell spacing relative to established in inspector scale.

                foreach (Entity entity in actualGroup)
                {
                    var buffer = entityManager.GetBuffer<Waypoint>(entity);
                    
                    if (buffer.Length > 0)
                    {
                        
                        //TODO draw lines properly. Right now it's looks wrong.
                        if (false)
                        {
                            var lineRenderer = pathRenderer.GetLineRenderer();
                            Queue<Vector3> positions = new Queue<Vector3>();
                        
                            for (int i = 0; i < buffer.Length - 1; i++)
                            {
                                positions.Enqueue((buffer[i].waypoints - .5f) * spacing);
                                positions.Enqueue((buffer[i + 1].waypoints - .5f) * spacing);
                            }
                        
                            lineRenderer.SetPositions(positions.ToArray());
                        }
                    }
                    
                    
                }
                actualGroup.Dispose();
            }
        }

        public void UpdatePaths()
        {
            VisualizePaths();
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
            
            InitMatrices();
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
            
            InitMatrices(); 
        }

        void OnDrawGizmos()
        {
            if ((visualMode == VisualMode.Instancing) || (!showGrid && !showPaths)) return;

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
                                Gizmos.color = gizmoPathColor;

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

//Making better testing workflow.
//TODO Using mouse for obstacles placing and new path creation.
//TODO Perlin noise for obstacles map generation.
//TODO Tools panel with some options, like "Clear all"? Saving&Loading obstacles&path scenarios to file?
//TODO Scale camera with current grid size.

//Research
//TODO Check deadlock at high obstacles density and with negative values in path positions.
//TODO Is using entities for each waypoint is fine? Wouldn't it be more convenient to store whole path data on associated entity?
//TODO Check for simulation of multiple grids at once.
