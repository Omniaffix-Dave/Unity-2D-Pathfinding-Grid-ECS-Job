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
        [FormerlySerializedAs("cursor")] public Material cursorMaterial;
        
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
                float spacing = pathfindingManager.instancedScale * pathfindingManager.instancedSpacing;
                Vector3 mousePosition = GetMousePositionOnXZPlane();
                mousePosition.x = Mathf.Round(mousePosition.x / spacing) * spacing;
                mousePosition.y = Mathf.Round(mousePosition.y / spacing) * spacing;
                
                Matrix4x4 trs = Matrix4x4.TRS(mousePosition, Quaternion.identity, new Vector3(spacing, spacing, spacing));
                
                Graphics.DrawMesh(pathfindingManager.instancedMeshBlocked, trs, cursorMaterial, 0);
                
                if(Input.GetMouseButton(0)) //LMB draw
                {
                    if(mousePosition == lastMousePosition) return;
                    lastMousePosition = mousePosition;
                    
                    mousePosition /= spacing;
                    pathfindingManager.SetBlockedCell(new int2((int) mousePosition.x, (int) mousePosition.y));
                    pathfindingManager.InitMatrices();
                }

                if (Input.GetMouseButton(1)) //RMB erase
                {
                    if(mousePosition == lastMousePosition) return;
                    lastMousePosition = mousePosition;
                    
                    mousePosition /= spacing;
                    pathfindingManager.SetWalkableCell(new int2((int) mousePosition.x, (int) mousePosition.y));
                    pathfindingManager.InitMatrices();
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