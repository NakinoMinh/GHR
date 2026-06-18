using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Systems
{
    public class Chapter1TaxiTraffic : MonoBehaviour
    {
        [System.Serializable]
        private class TaxiRoute
        {
            public Transform taxi;
            public Transform door;
            public Transform passengerSpawn;
            public Transform passengerExit;
            public float startX = -42f;
            public float stopX = -18f;
            public float endX = 72f;
            public float z = -5.4f;
            public float speed = 5f;
            public float waitAtPort = 3f;
            public float cooldown = 18f;
            public float passengerDespawnSeconds = 10f;
        }

        [SerializeField] private TaxiRoute[] routes;
        [SerializeField] private GameObject passengerPrefab;
        [SerializeField] private int maxActiveTaxis = 2;

        private readonly List<Coroutine> runningRoutes = new List<Coroutine>();

        private void OnEnable()
        {
            if (routes == null) return;

            int count = Mathf.Min(maxActiveTaxis, routes.Length);
            for (int i = 0; i < count; i++)
            {
                if (routes[i] != null && routes[i].taxi != null)
                {
                    runningRoutes.Add(StartCoroutine(RunRoute(routes[i], i * 7f)));
                }
            }
        }

        private void OnDisable()
        {
            foreach (Coroutine route in runningRoutes)
            {
                if (route != null) StopCoroutine(route);
            }
            runningRoutes.Clear();
        }

        private IEnumerator RunRoute(TaxiRoute route, float initialDelay)
        {
            yield return new WaitForSeconds(initialDelay);

            while (enabled && route.taxi != null)
            {
                PlaceTaxi(route, route.startX);
                route.taxi.gameObject.SetActive(true);

                yield return DriveTo(route, route.stopX);
                yield return DropPassenger(route);
                yield return new WaitForSeconds(route.waitAtPort);
                yield return DriveTo(route, route.endX);

                route.taxi.gameObject.SetActive(false);
                yield return new WaitForSeconds(route.cooldown + Random.Range(0f, 8f));
            }
        }

        private void PlaceTaxi(TaxiRoute route, float x)
        {
            route.taxi.position = new Vector3(x, route.taxi.position.y, route.z);
            FaceTravelDirection(route, route.stopX);
        }

        private IEnumerator DriveTo(TaxiRoute route, float targetX)
        {
            FaceTravelDirection(route, targetX);
            while (Mathf.Abs(route.taxi.position.x - targetX) > 0.05f)
            {
                Vector3 position = route.taxi.position;
                position.x = Mathf.MoveTowards(position.x, targetX, route.speed * Time.deltaTime);
                position.z = route.z + Mathf.Sin(Time.time * 4f) * 0.025f;
                route.taxi.position = position;
                yield return null;
            }
        }

        private IEnumerator DropPassenger(TaxiRoute route)
        {
            if (passengerPrefab == null || route.passengerSpawn == null || route.passengerExit == null) yield break;

            GameObject passenger = Instantiate(passengerPrefab, route.passengerSpawn.position, Quaternion.identity, transform);
            passenger.name = "TaxiPassenger";
            passenger.SetActive(true);

            float duration = 2.5f;
            float elapsed = 0f;
            Vector3 start = route.passengerSpawn.position;
            Vector3 end = route.passengerExit.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                passenger.transform.position = Vector3.Lerp(start, end, t);
                passenger.transform.rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
                yield return null;
            }

            Destroy(passenger, route.passengerDespawnSeconds);
        }

        private static void FaceTravelDirection(TaxiRoute route, float targetX)
        {
            float yaw = targetX >= route.taxi.position.x ? 90f : -90f;
            route.taxi.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
