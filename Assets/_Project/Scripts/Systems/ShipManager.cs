using UnityEngine;
using UnityEngine.SceneManagement;
using GanhHangRong.Core;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Spawns the moving harbor traffic whenever the gameplay scene is loaded.
    /// </summary>
    public class ShipManager : MonoBehaviour
    {
        private const string FerryResourcePath = "Ships/PhuQuocExpress";
        private const string FishingBoatResourcePath = "Ships/FishingBoat";

        private bool hasSpawnedShips;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != Constants.GAMEPLAY_SCENE_NAME)
            {
                return;
            }

            ShipManager existing = FindAnyObjectByType<ShipManager>(FindObjectsInactive.Include);
            if (existing == null)
            {
                new GameObject("[ShipManager]").AddComponent<ShipManager>();
            }
        }

        private void Start()
        {
            if (gameObject.scene.name != Constants.GAMEPLAY_SCENE_NAME || hasSpawnedShips)
            {
                return;
            }

            hasSpawnedShips = true;

            SpawnShip(FerryResourcePath, "TauMauDo_1",
                new Vector3(-50f, -1.4f, 30f), new Vector3(60f, -1.4f, 30f),
                3.5f, 10f, new Vector3(-90f, -90f, 0f));

            SpawnShip(FishingBoatResourcePath, "ThuyenDanhCa_1",
                new Vector3(50f, -1.4f, 65f), new Vector3(-40f, -1.4f, 65f),
                2f, 4.5f, new Vector3(-90f, -90f, 0f));

            SpawnShip(FishingBoatResourcePath, "ThuyenDanhCa_2",
                new Vector3(-60f, -1.4f, 75f), new Vector3(40f, -1.4f, 75f),
                2.2f, 4.5f, new Vector3(-90f, -90f, 0f));

            SpawnShip(FishingBoatResourcePath, "ThuyenDanhCa_3",
                new Vector3(70f, -1.4f, 85f), new Vector3(-70f, -1.4f, 85f),
                1.8f, 4.5f, new Vector3(-90f, -90f, 0f));

            SpawnShip(FerryResourcePath, "TauMauDo_2",
                new Vector3(80f, -1.4f, 65f), new Vector3(-80f, -1.4f, 65f),
                4f, 10f, new Vector3(-90f, -90f, 0f));
        }

        private static void SpawnShip(
            string resourcePath,
            string shipName,
            Vector3 start,
            Vector3 end,
            float speed,
            float targetHeight,
            Vector3 visualRotation)
        {
            if (GameObject.Find(shipName) != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[ShipManager] Missing Resources/{resourcePath} prefab.");
                return;
            }

            GameObject parent = new GameObject(shipName);
            parent.transform.position = start;

            GameObject visual = Instantiate(prefab, parent.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(visualRotation);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (TryCalculateBounds(renderers, out Bounds bounds) && bounds.size.y > 0.001f)
            {
                float scale = targetHeight / bounds.size.y;
                visual.transform.localScale *= scale;

                if (TryCalculateBounds(renderers, out bounds))
                {
                    float bottomCorrection = parent.transform.position.y - bounds.min.y - 0.2f;
                    visual.transform.position += Vector3.up * bottomCorrection;
                }
            }

            Environment.ShipPatrol patrol = parent.AddComponent<Environment.ShipPatrol>();
            patrol.startPos = start;
            patrol.endPos = end;
            patrol.speed = speed;
        }

        private static bool TryCalculateBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool foundRenderer = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return foundRenderer;
        }
    }
}
