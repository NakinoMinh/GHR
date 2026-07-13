using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Systems
{
    public class TrafficVehicle : MonoBehaviour
    {
        public enum VehicleType { Car, Taxi, Bus, Motorbike }
        public VehicleType vehicleType;
        public float speed = 5f;
        public float rotationSpeed = 5f;

        private TrafficWaypoint currentTarget;
        private bool isPaused = false;
        private CityTrafficManager manager;
        private int laneSide; // -1 for left, 1 for right (used by manager to track counts)

        public void Initialize(CityTrafficManager mgr, TrafficWaypoint startNode, int side, float spd)
        {
            manager = mgr;
            laneSide = side;
            speed = spd;
            transform.position = startNode.transform.position;
            SelectNextTarget(startNode);
            isPaused = false;
        }

        private void Update()
        {
            if (isPaused || currentTarget == null) return;

            Vector3 targetPos = currentTarget.transform.position;
            targetPos.y = transform.position.y; // Keep Y flat if roads aren't perfectly aligned

            // Move towards
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // Rotate towards
            Vector3 direction = targetPos - transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Check arrival
            if (Vector3.Distance(transform.position, targetPos) < 0.2f)
            {
                OnWaypointReached(currentTarget);
            }
        }

        private void OnWaypointReached(TrafficWaypoint reachedNode)
        {
            if (reachedNode.isBusStop && vehicleType == VehicleType.Bus)
            {
                StartCoroutine(WaitAtBusStop(reachedNode));
            }
            else
            {
                SelectNextTarget(reachedNode);
            }
        }

        private IEnumerator WaitAtBusStop(TrafficWaypoint reachedNode)
        {
            isPaused = true;
            yield return new WaitForSeconds(5f);
            isPaused = false;
            SelectNextTarget(reachedNode);
        }

        private void SelectNextTarget(TrafficWaypoint currentNode)
        {
            if (currentNode.nextWaypoints == null || currentNode.nextWaypoints.Count == 0)
            {
                // End of path, despawn
                Despawn();
                return;
            }

            // Pick random next waypoint
            int r = Random.Range(0, currentNode.nextWaypoints.Count);
            currentTarget = currentNode.nextWaypoints[r];
            
            // Clean up missing references if any
            if (currentTarget == null)
            {
                currentNode.nextWaypoints.RemoveAt(r);
                if (currentNode.nextWaypoints.Count > 0)
                {
                    SelectNextTarget(currentNode);
                }
                else
                {
                    Despawn();
                }
            }
        }

        private void Despawn()
        {
            Destroy(gameObject); // OnDestroy will handle manager notification
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnVehicleDespawned(this, laneSide);
                manager = null; // Prevent double-counting if called manually
            }
        }
    }
}
