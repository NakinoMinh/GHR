using UnityEngine;

namespace GanhHangRong.Environment
{
    public class ShipPatrol : MonoBehaviour
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public float speed = 2f;
        
        private bool movingToEnd = true;

        void Update()
        {
            Vector3 target = movingToEnd ? endPos : startPos;
            Vector3 dir = (target - transform.position).normalized;
            dir.y = 0;

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 60f * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, target) < 1f)
            {
                movingToEnd = !movingToEnd;
            }
        }
    }
}
