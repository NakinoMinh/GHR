using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Systems
{
    public class TrafficWaypoint : MonoBehaviour
    {
        public List<TrafficWaypoint> nextWaypoints = new List<TrafficWaypoint>();
        public bool isBusStop = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = isBusStop ? Color.yellow : Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.3f);

            Gizmos.color = Color.green;
            foreach (var next in nextWaypoints)
            {
                if (next != null)
                {
                    Vector3 direction = next.transform.position - transform.position;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Vector3 arrowHead = transform.position + direction * 0.8f;
                        Gizmos.DrawLine(transform.position, next.transform.position);
                        
                        // Draw a simple arrow head
                        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
                        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);
                        Gizmos.DrawRay(arrowHead, right * 0.5f);
                        Gizmos.DrawRay(arrowHead, left * 0.5f);
                    }
                }
            }
        }
    }
}
