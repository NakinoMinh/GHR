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
        [SerializeField] private GameObject[] portNpcModelPrefabs;
        [SerializeField] private RuntimeAnimatorController[] portNpcAnimatorControllers;
        [SerializeField] private Material yellowTaxiMaterial;
        [SerializeField] private Material greenTaxiMaterial;
        [SerializeField] private int maxActiveTaxis = 2;

        private readonly List<Coroutine> runningRoutes = new List<Coroutine>();
        private Material taxiBlackMaterial;
        private Material taxiWhiteMaterial;
        private Material taxiGlassMaterial;
        private Material taxiYellowAccentMaterial;
        private Material taxiGreenAccentMaterial;

        private void Awake()
        {
            UpgradePortPassengerModels();
            RebuildTaxiModels();
        }

        private void OnEnable()
        {
            UpgradePortPassengerModels();
            RebuildTaxiModels();

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
            ReplacePassengerPlaceholder(passenger.transform, Random.Range(0, int.MaxValue), 0.35f);

            float duration = 2.5f;
            float elapsed = 0f;
            Vector3 start = route.passengerSpawn.position;
            Vector3 end = route.passengerExit.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                passenger.transform.position = Vector3.Lerp(start, end, t);
                Vector3 direction = end - start;
                if (direction.sqrMagnitude > 0.001f)
                {
                    passenger.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
                yield return null;
            }

            Destroy(passenger, route.passengerDespawnSeconds);
        }

        private static void FaceTravelDirection(TaxiRoute route, float targetX)
        {
            float yaw = targetX >= route.taxi.position.x ? 90f : -90f;
            route.taxi.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void UpgradePortPassengerModels()
        {
            if (portNpcModelPrefabs == null || portNpcModelPrefabs.Length == 0) return;

            Transform searchRoot = transform.root != null ? transform.root : transform;
            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("PierPassenger_", System.StringComparison.Ordinal))
                {
                    ReplacePassengerPlaceholder(child, Mathf.Abs(child.GetSiblingIndex()), 0f);
                }
            }
        }

        private void ReplacePassengerPlaceholder(Transform passengerRoot, int modelSeed, float animatorSpeed)
        {
            if (passengerRoot == null) return;
            if (portNpcModelPrefabs == null || portNpcModelPrefabs.Length == 0) return;

            int safeSeed = modelSeed == int.MinValue ? 0 : Mathf.Abs(modelSeed);
            Transform existingModel = passengerRoot.Find("NPCModel");
            if (existingModel != null)
            {
                ConfigurePassengerAnimator(existingModel.gameObject, GetPortNpcAnimatorController(safeSeed), animatorSpeed);
                return;
            }

            GameObject prefab = portNpcModelPrefabs[safeSeed % portNpcModelPrefabs.Length];
            RuntimeAnimatorController animatorController = GetPortNpcAnimatorController(safeSeed);
            if (prefab == null) return;

            HideLegacyPassengerRenderers(passengerRoot);

            GameObject model = Instantiate(prefab, passengerRoot);
            model.name = "NPCModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            ConfigurePassengerAnimator(model, animatorController, animatorSpeed);
            FitModelToStandingHeight(model.transform, passengerRoot, 1.86f);
        }

        private RuntimeAnimatorController GetPortNpcAnimatorController(int modelSeed)
        {
            if (portNpcAnimatorControllers == null || portNpcAnimatorControllers.Length == 0) return null;

            int safeSeed = modelSeed == int.MinValue ? 0 : Mathf.Abs(modelSeed);
            return portNpcAnimatorControllers[safeSeed % portNpcAnimatorControllers.Length];
        }

        private static void HideLegacyPassengerRenderers(Transform passengerRoot)
        {
            foreach (Renderer renderer in passengerRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }

        private static void ConfigurePassengerAnimator(GameObject model, RuntimeAnimatorController animatorController, float animatorSpeed)
        {
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null) return;

            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = animatorSpeed;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == "State")
                {
                    animator.SetInteger("State", 0);
                    break;
                }
            }
        }

        private static void FitModelToStandingHeight(Transform model, Transform anchor, float targetHeight)
        {
            if (model == null || anchor == null) return;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.y > 0.001f)
            {
                float scale = targetHeight / bounds.size.y;
                model.localScale *= scale;
            }

            renderers = model.GetComponentsInChildren<Renderer>(true);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localOffset = anchor.InverseTransformVector(anchor.position - new Vector3(bounds.center.x, anchor.position.y, bounds.center.z));
            model.localPosition += localOffset;

            float groundDelta = anchor.position.y - bounds.min.y;
            model.localPosition += anchor.InverseTransformVector(Vector3.up * groundDelta);
        }

        private void RebuildTaxiModels()
        {
            if (routes == null) return;

            for (int i = 0; i < routes.Length; i++)
            {
                TaxiRoute route = routes[i];
                if (route == null || route.taxi == null) continue;

                DisableLegacyTaxiRenderers(route.taxi);

                if (route.taxi.Find("TaxiModel_Rebuilt") != null) continue;

                Material bodyMaterial = route.taxi.name.Contains("Green")
                    ? greenTaxiMaterial
                    : yellowTaxiMaterial;

                BuildTaxiModel(route.taxi, bodyMaterial, route.taxi.name.Contains("Green"));
            }
        }

        private static void DisableLegacyTaxiRenderers(Transform taxi)
        {
            foreach (Renderer renderer in taxi.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }

        private void BuildTaxiModel(Transform taxi, Material bodyMaterial, bool isGreenTaxi)
        {
            GameObject root = new GameObject("TaxiModel_Rebuilt");
            root.transform.SetParent(taxi, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Material black = GetRuntimeMaterial(ref taxiBlackMaterial, "Taxi_Black_Runtime", new Color(0.02f, 0.02f, 0.025f));
            Material white = GetRuntimeMaterial(ref taxiWhiteMaterial, "Taxi_White_Runtime", new Color(0.95f, 0.92f, 0.82f));
            Material glass = GetRuntimeMaterial(ref taxiGlassMaterial, "Taxi_Glass_Runtime", new Color(0.25f, 0.42f, 0.52f));

            CreateBox(root.transform, "Body", new Vector3(0f, 0.48f, 0f), new Vector3(1.75f, 0.56f, 3.35f), bodyMaterial);
            CreateBox(root.transform, "Cabin", new Vector3(0f, 0.95f, -0.2f), new Vector3(1.32f, 0.72f, 1.42f), bodyMaterial);
            CreateBox(root.transform, "FrontWindow", new Vector3(0f, 1.05f, 0.54f), new Vector3(1.18f, 0.38f, 0.08f), glass);
            CreateBox(root.transform, "RearWindow", new Vector3(0f, 1.05f, -0.96f), new Vector3(1.18f, 0.38f, 0.08f), glass);
            CreateBox(root.transform, "LeftWindow", new Vector3(-0.68f, 1.03f, -0.2f), new Vector3(0.08f, 0.38f, 0.9f), glass);
            CreateBox(root.transform, "RightWindow", new Vector3(0.68f, 1.03f, -0.2f), new Vector3(0.08f, 0.38f, 0.9f), glass);
            CreateBox(root.transform, "RoofTaxiSign", new Vector3(0f, 1.38f, -0.18f), new Vector3(0.78f, 0.22f, 0.38f), white);
            CreateBox(root.transform, "FrontBumper", new Vector3(0f, 0.42f, 1.78f), new Vector3(1.55f, 0.2f, 0.12f), black);
            CreateBox(root.transform, "RearBumper", new Vector3(0f, 0.42f, -1.78f), new Vector3(1.55f, 0.2f, 0.12f), black);
            CreateBox(root.transform, "TaxiLabel", new Vector3(0f, 0.62f, 1.86f), new Vector3(0.85f, 0.18f, 0.06f), white);

            CreateWheel(root.transform, "Wheel_FL", new Vector3(-0.93f, 0.25f, 1.05f), black);
            CreateWheel(root.transform, "Wheel_FR", new Vector3(0.93f, 0.25f, 1.05f), black);
            CreateWheel(root.transform, "Wheel_RL", new Vector3(-0.93f, 0.25f, -1.1f), black);
            CreateWheel(root.transform, "Wheel_RR", new Vector3(0.93f, 0.25f, -1.1f), black);

            Color lampColor = isGreenTaxi ? new Color(1f, 0.95f, 0.3f) : new Color(0.1f, 0.75f, 0.25f);
            Material accent = isGreenTaxi
                ? GetRuntimeMaterial(ref taxiGreenAccentMaterial, "Taxi_Green_Accent_Runtime", lampColor)
                : GetRuntimeMaterial(ref taxiYellowAccentMaterial, "Taxi_Yellow_Accent_Runtime", lampColor);
            CreateBox(root.transform, "HeadLight_L", new Vector3(-0.45f, 0.58f, 1.84f), new Vector3(0.32f, 0.16f, 0.05f), accent);
            CreateBox(root.transform, "HeadLight_R", new Vector3(0.45f, 0.58f, 1.84f), new Vector3(0.32f, 0.16f, 0.05f), accent);
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateWheel(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.34f, 0.16f, 0.34f);

            Collider collider = wheel.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer renderer = wheel.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material GetRuntimeMaterial(ref Material material, string name, Color color)
        {
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }
    }
}
