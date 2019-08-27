using UnityEngine;
using Pathfinding;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace UnityTemplateProjects
{
    public class SimpleCameraController : MonoBehaviour
    {
        public float speed = 3.5f;
        public float speedUp = 3f;

        public Vector2 zoomMinMax = new Vector2(10, 70);
        public Material cursorMaterial, startPointMaterial, endPointMaterial;

        public TextFade help;
        
        static Plane XYPlane = new Plane(Vector3.forward, Vector3.zero);
        private Camera main;
        private Transform tf;

        private Vector3 lastMousePosition = Vector3.negativeInfinity;

        private PathfindingManager pathfindingManager;
        
        
        void OnEnable()
        {
            tf = GetComponent<Transform>();
            main = GetComponent<Camera>();

            pathfindingManager = FindObjectOfType<PathfindingManager>();
        }

        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.up;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }
            return direction;
        }
        
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if(!help.gameObject.activeSelf) {    help.gameObject.SetActive(true);    }
                else
                {
                    help.Hide();
                }
            }
            
            //Zoom
            var mouseWheel = Input.mouseScrollDelta.y;
            if (mouseWheel != 0)
            {
                var size = Mathf.Clamp(main.orthographicSize - (mouseWheel * 3), zoomMinMax.x, zoomMinMax.y);
                main.orthographicSize = size;
            }

            //Quit
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
				#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false; 
				#endif
            }
            
            // Translation
            var translation = GetInputTranslationDirection();

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= speedUp;
            }

            var temp = tf.position;
            temp += ((translation * speed) * Time.smoothDeltaTime);
            tf.position = temp;
            
            DrawObstacles();
        }

        //Use mouse to draw some obstacles
        private void DrawObstacles()
        {
            if (pathfindingManager.visualMode != PathfindingManager.VisualMode.Gizmo)
            {
                float spacing = pathfindingManager.gridScale * pathfindingManager.gridSpacing;
                Vector3 mousePosition = GetMousePositionOnXZPlane();
                mousePosition.x = Mathf.Round(mousePosition.x / spacing) * spacing;
                mousePosition.y = Mathf.Round(mousePosition.y / spacing) * spacing;
                
                Matrix4x4 trs = Matrix4x4.TRS(mousePosition, Quaternion.identity, new Vector3(spacing, spacing, spacing));
                
                Graphics.DrawMesh(pathfindingManager.obstacleMesh, trs, cursorMaterial, 0);
                
                trs.m03 = pathfindingManager.start.x * spacing;
                trs.m13 = pathfindingManager.start.y * spacing;
                Graphics.DrawMesh(pathfindingManager.obstacleMesh, trs, startPointMaterial, 0);
                
                trs.m03 = pathfindingManager.end.x * spacing;
                trs.m13 = pathfindingManager.end.y * spacing;
                Graphics.DrawMesh(pathfindingManager.obstacleMesh, trs, endPointMaterial, 0);
                
                
                if(Input.GetMouseButton(0)) //LMB draw
                {
                    if(mousePosition == lastMousePosition) return;
                    lastMousePosition = mousePosition;
                    
                    mousePosition /= spacing;
                    pathfindingManager.SetObstacle(new int2((int) mousePosition.x, (int) mousePosition.y));
                    pathfindingManager.UpdateMatrices();
                }

                if (Input.GetMouseButton(1)) //RMB erase
                {
                    if(mousePosition == lastMousePosition) return;
                    lastMousePosition = mousePosition;
                    
                    mousePosition /= spacing;
                    pathfindingManager.SetNodeWalkable(new int2((int) mousePosition.x, (int) mousePosition.y));
                    pathfindingManager.UpdateMatrices();
                }

                if (Input.GetMouseButtonDown(2))
                {
                    lastMousePosition = mousePosition;
                    
                    mousePosition /= spacing;
                    pathfindingManager.SetNodeWalkable(new int2((int) mousePosition.x, (int) mousePosition.y));

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        pathfindingManager.start = new Vector2Int((int) mousePosition.x, (int) mousePosition.y);
                    }
                    else
                    {
                        pathfindingManager.end = new Vector2Int((int) mousePosition.x, (int) mousePosition.y);
                    }
                    
                    pathfindingManager.UpdateMatrices();
                }

                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                {
                    pathfindingManager.searchManualPath = true;
                }

                /*if (Input.GetKeyDown(KeyCode.F5)) //F5 to update
                {
                    pathfindingManager.UpdatePathsDisplay();
                    pathfindingManager.UpdateMatrices();
                }*/
                
                if (Input.GetKeyDown(KeyCode.Delete))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        pathfindingManager.ClearObstaclesMap();
                        pathfindingManager.ClearPathfinders();
                    }
                    else
                    {
                        pathfindingManager.ClearPathfinders();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    pathfindingManager.GeneratePerlinNoiseObstacles();
                }
                
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    pathfindingManager.SaveToFile();
                }
                else if (Input.GetKeyDown(KeyCode.F9))
                {
                    pathfindingManager.LoadFromFile();
                }
            }
        }
        
        private Vector3 GetMousePositionOnXZPlane() 
        {
            float distance;
            var ray = main.ScreenPointToRay(Input.mousePosition);
            
            if(XYPlane.Raycast (ray, out distance)) 
            {
                Vector3 hitPoint = ray.GetPoint(distance);

                hitPoint.z = 0;
                return hitPoint;
            }
            
            return Vector3.zero;
        }
    }

}