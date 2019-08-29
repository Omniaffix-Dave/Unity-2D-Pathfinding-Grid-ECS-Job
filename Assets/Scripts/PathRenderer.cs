using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding
{
    //Basically just a pool of LineRenderers.
    public class PathRenderer : MonoBehaviour
    {
        public int        poolSize = 50;
        public GameObject lineRendererPrefab;

        private Queue<LineRenderer> pool = new Queue<LineRenderer>();
        private Queue<LineRenderer> used = new Queue<LineRenderer>();

        private void Awake()
        {
            InstantiateSome(count: poolSize);
        }

        //Will return one object out of pool and create new one if there is not enough.
        public LineRenderer GetLineRenderer()
        {
            if (pool.Count <= 0) InstantiateSome();
            
            var temp = pool.Dequeue();
            temp.SetPositions(new Vector3[1]{ new Vector3() });
            temp.gameObject.SetActive(true);
            used.Enqueue(temp);
            
            return temp;
        }
        
        private void InstantiateSome(int count = 1)
        {
            var tf = GetComponent<Transform>();
            
            for (int i = 0; i < count; i++)
            {
                var temp = Instantiate(lineRendererPrefab, tf);
                pool.Enqueue(temp.GetComponent<LineRenderer>());
                temp.SetActive(false);
            }
        }

        //Return all used objects to pool.
        public void Clear()
        {
            int count = used.Count;
            for (int i = 0; i < count; i++)
            {
                var temp = used.Dequeue();
                temp.gameObject.SetActive(false);
                pool.Enqueue(temp);
            }
        }
    }
}
