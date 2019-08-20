using UnityEngine;

namespace UnityTemplateProjects
{
    public class SimpleCameraController : MonoBehaviour
    {
        public float speed = 3.5f;
        public float speedUp = 3f;

        public Vector2 zoomMinMax = new Vector2(10, 70);
        
        static Plane XZPlane = new Plane(Vector3.up, Vector3.zero);
        private Camera main;
        private Transform tf;

        void OnEnable()
        {
            tf = GetComponent<Transform>();
            main = GetComponent<Camera>();
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
            var mouseWheel = Input.mouseScrollDelta.y;

            if (mouseWheel != 0)
            {
                var size = Mathf.Clamp(main.orthographicSize - (mouseWheel * 3), zoomMinMax.x, zoomMinMax.y);
                main.orthographicSize = size;
            }

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
        }

        private Vector3 GetMousePositionOnXZPlane() 
        {
            float distance;
            var ray = main.ScreenPointToRay(Input.mousePosition);
            
            if(XZPlane.Raycast (ray, out distance)) 
            {
                Vector3 hitPoint = ray.GetPoint(distance);

                hitPoint.y = 0;
                return hitPoint;
            }
            
            return Vector3.zero;
        }
    }

}