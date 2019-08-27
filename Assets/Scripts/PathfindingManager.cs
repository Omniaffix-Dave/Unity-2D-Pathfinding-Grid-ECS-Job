using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Pathfinding
{
    [RequireComponent(typeof(PathRenderer))]
    public class PathfindingManager : MonoBehaviour
    {
        public int iterationLimit = 1000;
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
        [HideInInspector] public int randomPathsCount;
        [HideInInspector] public bool searchRandomPaths;

        //Manual blocked
        List<int2> obstacleNodes = new List<int2>();
        [HideInInspector] public Vector2Int obstacleNode;
        [HideInInspector] public bool addObstacleManually;

        //Random blocked
        [HideInInspector] public int randomObstaclesCount = 1;
        [HideInInspector] public bool addRandomObstacles;

        //Instancing settings
        [HideInInspector] public Material nodeMaterial;
        [HideInInspector] public Material obstacleMaterial;
        [HideInInspector] public Mesh walkableMesh;
        [HideInInspector] public Mesh obstacleMesh;
        [HideInInspector] public float gridSpacing = 1.1f;
        [HideInInspector] public float gridScale = 1;

        [HideInInspector] public float noiseLevel = 0.35f;
        [HideInInspector] public float noiseScale = 10;
        
        private const int instancesLimit = 1023; //DrawMeshInstanced can only draw a maximum of 1023 instances at once.
        private Matrix4x4[][] matricesWalkable;
        private Matrix4x4[][] matricesObstacles;

        private PathRenderer pathRenderer;
        private List<Vector3> obstaclesToAdd = new List<Vector3>();
        private EntityManager entityManager;
        private PathfindingSystem pathfindingSystem;

        private bool pathfindingNow = false;
        
        int previousSize; // Prevent GUI Errors
        public enum VisualMode { Gizmo, Instancing, Both };

        

        private void Awake()
        {
            pathfindingSystem = World.Active.GetExistingSystem<PathfindingSystem>(); 
            pathfindingSystem.canMoveDiag = canMoveDiag;
            entityManager = World.Active.EntityManager;
            
            CreateGrid();
            
            pathRenderer = GetComponent<PathRenderer>();
            
            GeneratePerlinNoiseObstacles();
        }    

        private void Update()
        {
            pathfindingSystem.IterationLimit = iterationLimit;
            
            if (pathfindingSystem.numberOfRequests > 0)
            {
                pathfindingNow = true;
            }
            else if (pathfindingNow)
            {
                pathfindingNow = false;
                UpdatePathsDisplay();
            }
            
            if(size.x * size.y != previousSize)
            {
                CreateGrid();
            }

            if(addObstacleManually)
            {
                obstacleNodes.Add(new int2(obstacleNode.x, obstacleNode.y));
                addObstacleManually = false;
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

            if (addRandomObstacles)
            {
                addRandomObstacles = false;
                SetRandomObstacles();
            }

            if(searchRandomPaths)
            {
                searchRandomPaths = false;
                var nodes = RequiredExtensions.nodes;

                for (int i = 0; i < randomPathsCount; i++)
                {
                    int2 startPosition = new int2(Random.Range(0, size.x), Random.Range(0, size.y));
                    if (nodes[GetIndex(startPosition)].Obstacle) continue;
                    
                    int2 endPosition = new int2(Random.Range(0, size.x), Random.Range(0, size.y));
                    CreateSearcher(startPosition, endPosition);
                }
            }

            //Instancing.
            if (visualMode == VisualMode.Instancing || visualMode == VisualMode.Both)
            {
                if (showGrid) DisplayGrid();
            }
        }
        
        //Display grid info with instancing.
        private void DisplayGrid()
        {
            //Walkable nodes might be replaced with single large quad.
            for (int i = 0; i < matricesWalkable.Length; i++)
            {
                Graphics.DrawMeshInstanced(walkableMesh, 0, nodeMaterial, 
                    matricesWalkable[i], matricesWalkable[i].Length, null, ShadowCastingMode.Off);
            }
            
            for (int i = 0; i < matricesObstacles.Length; i++)
            {
                Graphics.DrawMeshInstanced(obstacleMesh, 0, obstacleMaterial, 
                    matricesObstacles[i], matricesObstacles[i].Length);
            }
        }

        //Update transform data for instances and split it. Must be called every time grid is changed.
        public void UpdateMatrices()
        {
            var nodes = RequiredExtensions.nodes;
            //Initially it's unknown how much of blocked nodes there is, so let's just count them,
            //so we could render them separately later.
            List<Queue<Matrix4x4>> queueMatricesWalkable = new List<Queue<Matrix4x4>>();
            List<Queue<Matrix4x4>> queueMatricesObstacles = new List<Queue<Matrix4x4>>();

            int walkableCount = 0;
            int walkableListIndex = 0;
            int obstaclesCount = 0;
            int obstaclesListIndex = 0;
                
            queueMatricesWalkable.Add(new Queue<Matrix4x4>());
            queueMatricesObstacles.Add(new Queue<Matrix4x4>());
            
            Vector3 position   = new Vector3(0, 0, 0);
            Vector3 scale      = new Vector3(gridScale, gridScale, gridScale);

            float spacing = (gridSpacing * gridScale); //Make node spacing relative to established in inspector scale.
            
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                position.x = x * spacing;
                position.y = y * spacing;
                position.z = 0;

                var obstacle = nodes[GetIndex(new int2(x, y))].Obstacle;

                //Add a matrix to the queue at corresponding list, since nodes are use different materials marking it's type.
                if (!obstacle)
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
                    queueMatricesObstacles[obstaclesListIndex].Enqueue(Matrix4x4.TRS(position, Quaternion.identity, scale));
                    obstaclesCount++;

                    if (obstaclesCount >= instancesLimit)
                    {
                        obstaclesCount = 0;
                        obstaclesListIndex++;
                        queueMatricesObstacles.Add(new Queue<Matrix4x4>());
                    }
                }
            }
            
            //Finally convert everything to the array of arrays.
            walkableListIndex++; 
            obstaclesListIndex++;
            matricesWalkable = new Matrix4x4[walkableListIndex][];
            matricesObstacles = new Matrix4x4[obstaclesListIndex][];
            
            for (int i = 0; i < walkableListIndex; i++)    matricesWalkable[i] = queueMatricesWalkable[i].ToArray();
            for (int i = 0; i < obstaclesListIndex; i++)    matricesObstacles[i] = queueMatricesObstacles[i].ToArray();
            
        }

        //Display existing paths with provided line renderer.
        private void DisplayPaths()
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Waypoint>());
            if (query.CalculateEntityCount() > 0)
            {
                var actualGroup = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
                pathRenderer.Clear();
                
                float spacing = (gridSpacing * gridScale); //Make node spacing relative to established in inspector scale.
                float3 offset = new Vector3(spacing * .5f, spacing * .5f, 0); //Ground path at the center of nodes.
                
                foreach (Entity entity in actualGroup)
                {
                    var pathRequest = entityManager.GetComponentData<PathRequest>(entity);
                    var buffer = entityManager.GetBuffer<Waypoint>(entity);
                    
                    var lineRenderer = pathRenderer.GetLineRenderer();
                    Queue<Vector3> positions = new Queue<Vector3>();
                    float3 start = (new float3(pathRequest.start.x, pathRequest.start.y, 0) * spacing);
                    float3 end = (new float3(pathRequest.end.x, pathRequest.end.y, 0) * spacing) - offset; 
                    
                    positions.Enqueue(start);
                    
                    if (buffer.Length > 0)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            positions.Enqueue(((buffer[i].waypoints) * spacing) - offset);
                            //positions.Enqueue((buffer[i + 1].waypoints - .5f) * spacing);
                        }
                    }
                    
                    //positions.Enqueue(end);
                    lineRenderer.positionCount = positions.Count;
                    lineRenderer.SetPositions(positions.ToArray());
                }
                actualGroup.Dispose();
            }
        }

        public void UpdatePathsDisplay()
        {
            //TODO hide paths when showPaths changed automatically.
            if(showPaths) {    DisplayPaths();    }
            else
            {
                pathRenderer.Clear();
            }
        }

        public void SetObstacle(int2 position)
        {
            var nodes = RequiredExtensions.nodes;
            
            nodes[GetIndex(position)] = new Node { obstacle = Convert.ToByte(true), Height = 0 };
            obstacleNodes.Clear();
            
            UpdateMatrices();
            
        }
        
        public void SetNodeWalkable(int2 position)
        {
            var nodes = RequiredExtensions.nodes;
            
            nodes[GetIndex(position)] = new Node { obstacle = Convert.ToByte(false), Height = 0 };
        }
        
        public void SetRandomObstacles()
        {
            var nodes = RequiredExtensions.nodes;

            for (int i = 0; i < randomObstaclesCount; i++)
            {
                int2 targetNode = new int2(Random.Range(0, size.x), Random.Range(0, size.y));

                int randomNode = GetIndex(targetNode);
                nodes[randomNode] = new Node { obstacle = Convert.ToByte(true), Height = 0 };
            }
            
            UpdateMatrices();
        }

        public void GeneratePerlinNoiseObstacles()
        {
            ClearPathfinders();
            ClearObstaclesMap(updateMatrices: false);
            float randomization = Random.value * 10000;

            var nodes = RequiredExtensions.nodes;
            float2 scale = (new float2(1f / size.x, 1f / size.y) * noiseScale); 
            
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                bool obstacle = Mathf.PerlinNoise((scale.x * x) + randomization, (scale.y * y)) <= noiseLevel;
                nodes[GetIndex(new int2(x, y))] = new Node { obstacle = Convert.ToByte(obstacle), Height = 0 };
            }
            
            UpdateMatrices();
        }
        
        
        public void ClearObstaclesMap(bool updateMatrices = true)
        {
            var nodes = RequiredExtensions.nodes;
            
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                nodes[GetIndex(new int2(x, y))] = new Node { obstacle = Convert.ToByte(false), Height = 0 };
            }
            
            if(updateMatrices) UpdateMatrices();
            
        }

        public void ClearPathfinders()
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Waypoint>());
            entityManager.DestroyEntity(query);
            
            pathRenderer.Clear();
        }
        
        public void CreateSearcher(int2 s, int2 e)
        {
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
            RequiredExtensions.nodes.Dispose();
        }

        public void CreateGrid()
        {
            if(RequiredExtensions.nodes.IsCreated)
                RequiredExtensions.nodes.Dispose();
            RequiredExtensions.nodes = new Unity.Collections.NativeArray<Node>(size.x * size.y, Unity.Collections.Allocator.Persistent);

            previousSize = size.x * size.y;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Node node = new Node
                    {
                        obstacle = Convert.ToByte(obstacleNodes.Contains(new int2(x, y)))
                    };
                    RequiredExtensions.nodes[GetIndex(new int2(x, y))] = node;
                }
            }
            World.Active.GetExistingSystem<PathfindingSystem>().worldSize = size;
            
            UpdateMatrices(); 
        }

        void OnDrawGizmos()
        {
            if ((visualMode == VisualMode.Instancing) || (!showGrid && !showPaths)) return;

            if (Application.isPlaying)
            {
                if (showGrid && size.x * size.y == previousSize)
                {
                    var nodes = RequiredExtensions.nodes;

                    for (int x = 0; x < size.x; x++)
                    {
                        for (int y = 0; y < size.y; y++)
                        {
                            Gizmos.color = nodes[GetIndex(new int2(x, y))].Obstacle ? Color.grey : Color.white;
                            Gizmos.DrawCube(NodeToWorldPosition(new int2(x, y)), new Vector3(.90f, .90f));
                        }
                    }
                }

                if (showPaths)
                {
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

        public void SaveToFile()
        {
            string path = Application.dataPath + "/PathfindingSave";
            var nodes = RequiredExtensions.nodes;
            
            BinaryFormatter serializer = new BinaryFormatter();
            var fileStream = File.Create(Application.dataPath + "/PathfindingSave");

            int count = nodes.Length;
            SaveData saveData = new SaveData()
            {
                iterationLimit = iterationLimit,
                start = new SerializableInt2(start.x, start.y),
                end = new SerializableInt2(end.x, end.y),
                size = new SerializableInt2(size.x, size.y),
                nodes = new byte[count]
            };

            for (int i = 0; i < count; i++) saveData.nodes[i] = nodes[i].obstacle;
            Debug.Log(saveData.nodes[0]);
            
            serializer.Serialize(fileStream, saveData);
            fileStream.Close();
            
            Debug.Log("Saved to " + path);
        }

        public void LoadFromFile()
        {
            string path = Application.dataPath + "/PathfindingSave";
            if (File.Exists(path))
            {
                
                BinaryFormatter serializer = new BinaryFormatter();
                var fileStream = File.Open(path, FileMode.Open);

                SaveData saveData = (SaveData) serializer.Deserialize(fileStream);
                fileStream.Close();

                iterationLimit = saveData.iterationLimit;
                
                start = new Vector2Int(saveData.start.x, saveData.start.y);
                end = new Vector2Int(saveData.end.x, saveData.end.y);
                size = new int2(saveData.size.x, saveData.size.y);

                CreateGrid();
                var nodes = RequiredExtensions.nodes;
                int count = saveData.nodes.Length;
                
                for (int i = 0; i < count; i++)
                {
                    nodes[i] = new Node { Height =  0, obstacle = saveData.nodes[i] };
                }
                
                ClearPathfinders();
                UpdateMatrices();
                
                Debug.Log("Loaded.");
            }
        }

        [Serializable]
        private struct SaveData
        {
            public int iterationLimit;
            public SerializableInt2 start;
            public SerializableInt2 end;

            public SerializableInt2 size;
            public byte[] nodes;
        }

        [Serializable]
        struct SerializableInt2
        {
            public int x;
            public int y;

            public SerializableInt2(int x, int y)
            {
                this.x = x;
                this.y = y;
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

//Research
//TODO Check deadlock at high obstacles density and with negative values in path positions.
//TODO Check for simulation of multiple grids at once.
