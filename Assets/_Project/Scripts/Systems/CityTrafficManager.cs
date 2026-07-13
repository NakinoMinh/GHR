using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Systems
{
    public class CityTrafficManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject taxiPrefab;
        public GameObject otoPrefab;
        public GameObject busPrefab;
        public GameObject motorbikePrefab;

        [Header("Settings")]
        public int taxiPerSide = 1;
        public int otoPerSide = 2;
        public int busTotal = 2;
        public int motorbikePerSide = 5;

        [Header("Spawn Points (Starting nodes)")]
        public List<TrafficWaypoint> leftLaneSpawns = new List<TrafficWaypoint>();
        public List<TrafficWaypoint> rightLaneSpawns = new List<TrafficWaypoint>();

        // Track counts
        private int activeTaxiLeft, activeTaxiRight;
        private int activeOtoLeft, activeOtoRight;
        private int activeBusLeft, activeBusRight;
        private int activeMotorbikeLeft, activeMotorbikeRight;

        private Coroutine spawnCoroutine;

        private void OnEnable()
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            
            // Pre-populate traffic randomly along paths so the streets aren't empty at start
            PrePopulateTraffic();
        }

        private void OnDisable()
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        }

        private void PrePopulateTraffic()
        {
            for (int i = 0; i < taxiPerSide + otoPerSide + busTotal / 2 + motorbikePerSide; i++)
            {
                TrySpawnVehicleAtRandomNode(-1);
                TrySpawnVehicleAtRandomNode(1);
            }
        }

        private void TrySpawnVehicleAtRandomNode(int side)
        {
            List<TrafficWaypoint> spawns = side == -1 ? leftLaneSpawns : rightLaneSpawns;
            if (spawns.Count == 0) return;
            
            TrafficWaypoint startNode = spawns[Random.Range(0, spawns.Count)];
            // Traverse randomly to find a middle node
            int jumps = Random.Range(1, 10);
            for (int i = 0; i < jumps; i++)
            {
                if (startNode.nextWaypoints != null && startNode.nextWaypoints.Count > 0)
                {
                    startNode = startNode.nextWaypoints[Random.Range(0, startNode.nextWaypoints.Count)];
                }
            }
            
            SpawnLogic(startNode, side);
        }

        private void SpawnLogic(TrafficWaypoint spawnNode, int side)
        {
            if (side == -1)
            {
                if (activeBusLeft < busTotal / 2) SpawnVehicle(busPrefab, TrafficVehicle.VehicleType.Bus, spawnNode, side, 4f, ref activeBusLeft);
                else if (activeTaxiLeft < taxiPerSide) SpawnVehicle(taxiPrefab, TrafficVehicle.VehicleType.Taxi, spawnNode, side, 6f, ref activeTaxiLeft);
                else if (activeOtoLeft < otoPerSide) SpawnVehicle(otoPrefab, TrafficVehicle.VehicleType.Car, spawnNode, side, 6f, ref activeOtoLeft);
                else if (activeMotorbikeLeft < motorbikePerSide) SpawnVehicle(motorbikePrefab, TrafficVehicle.VehicleType.Motorbike, spawnNode, side, 5.5f, ref activeMotorbikeLeft);
            }
            else
            {
                if (activeBusRight < busTotal / 2) SpawnVehicle(busPrefab, TrafficVehicle.VehicleType.Bus, spawnNode, side, 4f, ref activeBusRight);
                else if (activeTaxiRight < taxiPerSide) SpawnVehicle(taxiPrefab, TrafficVehicle.VehicleType.Taxi, spawnNode, side, 6f, ref activeTaxiRight);
                else if (activeOtoRight < otoPerSide) SpawnVehicle(otoPrefab, TrafficVehicle.VehicleType.Car, spawnNode, side, 6f, ref activeOtoRight);
                else if (activeMotorbikeRight < motorbikePerSide) SpawnVehicle(motorbikePrefab, TrafficVehicle.VehicleType.Motorbike, spawnNode, side, 5.5f, ref activeMotorbikeRight);
            }
        }

        private IEnumerator SpawnRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(3f);
            while (true)
            {
                TrySpawnVehicle(-1); // Left lane
                TrySpawnVehicle(1);  // Right lane
                yield return wait;
            }
        }

        private void TrySpawnVehicle(int side)
        {
            List<TrafficWaypoint> spawns = side == -1 ? leftLaneSpawns : rightLaneSpawns;
            if (spawns.Count == 0) return;

            TrafficWaypoint spawnNode = spawns[Random.Range(0, spawns.Count)];
            SpawnLogic(spawnNode, side);
        }

        private void SpawnVehicle(GameObject prefab, TrafficVehicle.VehicleType type, TrafficWaypoint spawnNode, int side, float speed, ref int countTracker)
        {
            if (prefab == null) return;
            
            // Random speed variance
            speed += Random.Range(-0.5f, 1f);

            GameObject obj = Instantiate(prefab, spawnNode.transform.position, Quaternion.identity, transform);
            TrafficVehicle vehicle = obj.GetComponent<TrafficVehicle>();
            if (vehicle == null)
            {
                vehicle = obj.AddComponent<TrafficVehicle>();
            }
            
            vehicle.vehicleType = type;
            vehicle.Initialize(this, spawnNode, side, speed);
            countTracker++;
        }

        public void OnVehicleDespawned(TrafficVehicle vehicle, int side)
        {
            if (side == -1)
            {
                if (vehicle.vehicleType == TrafficVehicle.VehicleType.Bus) activeBusLeft--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Taxi) activeTaxiLeft--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Car) activeOtoLeft--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Motorbike) activeMotorbikeLeft--;
            }
            else
            {
                if (vehicle.vehicleType == TrafficVehicle.VehicleType.Bus) activeBusRight--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Taxi) activeTaxiRight--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Car) activeOtoRight--;
                else if (vehicle.vehicleType == TrafficVehicle.VehicleType.Motorbike) activeMotorbikeRight--;
            }
        }
    }
}
