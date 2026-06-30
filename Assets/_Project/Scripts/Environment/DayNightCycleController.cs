using GanhHangRong.Economy;
using UnityEngine;

namespace GanhHangRong.Environment
{
    public class DayNightCycleController : MonoBehaviour
    {
        [Header("Nguon thoi gian")]
        [SerializeField] private DayNightCycle gameTimeSource;
        [SerializeField] private TimeOfDayManager timeManager;

        [Header("Mat troi va mat trang")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private Transform sunTransform;
        [SerializeField] private Transform moonTransform;
        [SerializeField] private Transform sunVisual;
        [SerializeField] private Transform moonVisual;
        [SerializeField] private Vector3 sunRotationOffset = new Vector3(-90f, 170f, 0f);
        [SerializeField] private Vector3 moonRotationOffset = new Vector3(-90f, -10f, 0f);

        [Header("Hien thi mat troi / mat trang")]
        [SerializeField] private Vector3 visualCenter = new Vector3(76f, 0f, 2.5f);
        [SerializeField] private Vector3 sunSeaDirection = Vector3.forward;
        [SerializeField] private bool hideVisualBelowHorizon = true;
        [SerializeField, Min(10f)] private float celestialVisualDistance = 900f;
        [SerializeField, Min(1f)] private float sunVisualSize = 42f;
        [SerializeField, Min(1f)] private float moonVisualSize = 28f;

        [Header("Cuong do anh sang")]
        [SerializeField] private AnimationCurve sunIntensityCurve = CreateSunCurve();
        [SerializeField] private AnimationCurve moonIntensityCurve = CreateMoonCurve();
        [SerializeField, Min(0f)] private float maxSunIntensity = 1.25f;
        [SerializeField, Min(0f)] private float maxMoonIntensity = 0.22f;

        [Header("Mau sac theo ngay dem")]
        [SerializeField] private Gradient sunColorGradient = CreateSunGradient();
        [SerializeField] private Gradient moonColorGradient = CreateMoonGradient();
        [SerializeField] private Gradient ambientColorGradient = CreateAmbientGradient();
        [SerializeField] private Gradient fogColorGradient = CreateFogGradient();
        [SerializeField, Min(0f)] private float ambientIntensity = 1f;

        [Header("Fog")]
        [SerializeField] private bool controlFog = true;
        [SerializeField, Min(0f)] private float dayFogDensity = 0.006f;
        [SerializeField, Min(0f)] private float nightFogDensity = 0.014f;
        [SerializeField] private AnimationCurve fogDensityCurve = CreateFogDensityCurve();

        private bool warnedMissingTimeManager;
        private Material sunVisualMaterial;
        private Material moonVisualMaterial;

        private void Reset()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
            gameTimeSource = FindAnyObjectByType<DayNightCycle>();
            if (sunLight == null)
            {
                sunLight = RenderSettings.sun;
            }
        }

        private void OnEnable()
        {
            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeOfDayManager>();
            }

            if (gameTimeSource == null)
            {
                gameTimeSource = FindAnyObjectByType<DayNightCycle>();
            }

            CacheLightTransforms();
            EnsureCelestialVisuals();
        }

        private void Update()
        {
            if (gameTimeSource == null && timeManager == null)
            {
                if (!warnedMissingTimeManager)
                {
                    Debug.LogWarning($"{nameof(DayNightCycleController)} can DayNightCycle hoac TimeOfDayManager de cap nhat anh sang.", this);
                    warnedMissingTimeManager = true;
                }

                return;
            }

            ApplyLighting(GetNormalizedTime());
        }

        public void ApplyLighting(float normalizedTime)
        {
            normalizedTime = Mathf.Repeat(normalizedTime, 1f);

            float sunValue = Mathf.Clamp01(sunIntensityCurve.Evaluate(normalizedTime)) * maxSunIntensity;
            float moonValue = Mathf.Clamp01(moonIntensityCurve.Evaluate(normalizedTime)) * maxMoonIntensity;
            Color sunColor = Color.Lerp(sunColorGradient.Evaluate(normalizedTime), GetRealisticSunColor(normalizedTime), 0.85f);
            Color moonColor = Color.Lerp(moonColorGradient.Evaluate(normalizedTime), GetRealisticMoonColor(normalizedTime), 0.85f);

            ApplyDirectionalLight(sunLight, sunTransform, sunValue, sunColor, sunRotationOffset, normalizedTime, true);
            ApplyDirectionalLight(moonLight, moonTransform, moonValue, moonColor, moonRotationOffset, normalizedTime);
            ApplyCelestialVisual(sunVisual, sunLight, sunTransform, sunValue, sunColor, sunVisualSize);
            ApplyCelestialVisual(moonVisual, moonLight, moonTransform, moonValue, moonColor, moonVisualSize);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColorGradient.Evaluate(normalizedTime) * ambientIntensity;

            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColorGradient.Evaluate(normalizedTime);
                RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, Mathf.Clamp01(fogDensityCurve.Evaluate(normalizedTime)));
            }
        }

        private void ApplyDirectionalLight(Light targetLight, Transform targetTransform, float intensity, Color color, Vector3 rotationOffset, float normalizedTime, bool alignToSea = false)
        {
            if (targetLight == null)
            {
                return;
            }

            // Chi bat light khi co cuong do de tranh mat troi va mat trang cung sang manh.
            targetLight.enabled = intensity > 0.01f;
            targetLight.intensity = intensity;
            targetLight.color = color;

            Transform pivot = targetTransform != null ? targetTransform : targetLight.transform;
            Quaternion baseRotation = Quaternion.Euler(normalizedTime * 360f + rotationOffset.x, rotationOffset.y, rotationOffset.z);
            if (alignToSea)
            {
                Vector3 apparentSourceDirection = GetSeaAlignedApparentDirection(-(baseRotation * Vector3.forward).normalized);
                pivot.rotation = Quaternion.LookRotation(-apparentSourceDirection, Vector3.up);
                pivot.position = visualCenter + apparentSourceDirection * celestialVisualDistance;
                return;
            }

            pivot.rotation = baseRotation;
        }

        private void ApplyCelestialVisual(Transform visual, Light sourceLight, Transform sourceTransform, float intensity, Color color, float size)
        {
            if (visual == null || sourceLight == null)
            {
                return;
            }

            Transform lightTransform = sourceTransform != null ? sourceTransform : sourceLight.transform;
            Vector3 apparentSourceDirection = -lightTransform.forward.normalized;
            visual.position = visualCenter + apparentSourceDirection * celestialVisualDistance;
            visual.rotation = Quaternion.identity;
            visual.localScale = Vector3.one * size;
            visual.gameObject.SetActive(!hideVisualBelowHorizon || intensity > 0.01f);

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
                if (material != null)
                {
                    material.color = color;
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", color);
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", color * Mathf.Max(0.6f, intensity * 1.4f));
                    }
                }
            }
        }

        private Vector3 GetSeaAlignedApparentDirection(Vector3 apparentSourceDirection)
        {
            Vector3 seaDirection = sunSeaDirection;
            seaDirection.y = 0f;
            if (seaDirection.sqrMagnitude < 0.001f)
            {
                seaDirection = Vector3.forward;
            }

            seaDirection.Normalize();
            float height = Mathf.Clamp(apparentSourceDirection.y, -0.95f, 0.95f);
            float horizontal = Mathf.Sqrt(Mathf.Max(0.001f, 1f - height * height));
            return (seaDirection * horizontal + Vector3.up * height).normalized;
        }

        private void CacheLightTransforms()
        {
            if (sunTransform == null && sunLight != null)
            {
                sunTransform = sunLight.transform;
            }

            if (moonTransform == null && moonLight != null)
            {
                moonTransform = moonLight.transform;
            }
        }

        private float GetNormalizedTime()
        {
            if (gameTimeSource != null)
            {
                return Mathf.Repeat(gameTimeSource.CurrentHour / 24f, 1f);
            }

            return timeManager != null ? timeManager.NormalizedTime : 17f / 24f;
        }

        private void EnsureCelestialVisuals()
        {
            if (sunVisual == null || !IsSphereVisual(sunVisual))
            {
                ReplaceWithCelestialSphere(ref sunVisual, "RealisticSun", new Color(1f, 0.92f, 0.62f, 1f), sunVisualSize, ref sunVisualMaterial);
            }
            else
            {
                sunVisual.localScale = Vector3.one * sunVisualSize;
            }

            if (moonVisual == null || !IsSphereVisual(moonVisual))
            {
                ReplaceWithCelestialSphere(ref moonVisual, "RealisticMoon", new Color(0.82f, 0.82f, 0.78f, 1f), moonVisualSize, ref moonVisualMaterial);
            }
            else
            {
                moonVisual.localScale = Vector3.one * moonVisualSize;
            }
        }

        private static bool IsSphereVisual(Transform visual)
        {
            MeshFilter meshFilter = visual != null ? visual.GetComponent<MeshFilter>() : null;
            return meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.name.Contains("Sphere");
        }

        private void ReplaceWithCelestialSphere(ref Transform visual, string objectName, Color color, float size, ref Material cachedMaterial)
        {
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }

            visual = CreateCelestialSphere(objectName, color, size, ref cachedMaterial);
        }

        private Transform CreateCelestialSphere(string objectName, Color color, float size, ref Material cachedMaterial)
        {
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualObject.name = objectName;
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localScale = Vector3.one * size;

            Collider collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                cachedMaterial = CreateCelestialMaterial(objectName + "_Material", color);
                renderer.sharedMaterial = cachedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return visualObject.transform;
        }

        private Material CreateCelestialMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            material.name = materialName;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
            }

            return material;
        }

        private static Color GetRealisticSunColor(float normalizedTime)
        {
            float noonAmount = Mathf.Clamp01(1f - Mathf.Abs(normalizedTime - 0.5f) / 0.28f);
            Color lowSun = new Color(1f, 0.56f, 0.28f, 1f);
            Color highSun = new Color(1f, 0.96f, 0.82f, 1f);
            return Color.Lerp(lowSun, highSun, noonAmount);
        }

        private static Color GetRealisticMoonColor(float normalizedTime)
        {
            float highMoonAmount = Mathf.Clamp01(Mathf.Abs(normalizedTime - 0.5f) / 0.5f);
            Color lowMoon = new Color(0.62f, 0.64f, 0.64f, 1f);
            Color highMoon = new Color(0.88f, 0.87f, 0.82f, 1f);
            return Color.Lerp(lowMoon, highMoon, highMoonAmount);
        }

        private static AnimationCurve CreateSunCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.22f, 0.25f),
                new Keyframe(0.38f, 1f),
                new Keyframe(0.62f, 1f),
                new Keyframe(0.74f, 0.45f),
                new Keyframe(0.82f, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateMoonCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.75f),
                new Keyframe(0.2f, 0f),
                new Keyframe(0.78f, 0f),
                new Keyframe(0.88f, 0.65f),
                new Keyframe(1f, 0.75f));
        }

        private static AnimationCurve CreateFogDensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.25f, 0.35f),
                new Keyframe(0.5f, 0.15f),
                new Keyframe(0.75f, 0.5f),
                new Keyframe(1f, 1f));
        }

        private static Gradient CreateSunGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.62f, 0.34f), 0.23f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.86f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.58f, 0.3f), 0.72f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateMoonGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.64f, 0.64f), 0f),
                    new GradientColorKey(new Color(0.88f, 0.87f, 0.82f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateAmbientGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.06f, 0.09f, 0.18f), 0f),
                    new GradientColorKey(new Color(0.92f, 0.68f, 0.42f), 0.25f),
                    new GradientColorKey(new Color(0.88f, 0.86f, 0.76f), 0.5f),
                    new GradientColorKey(new Color(0.95f, 0.5f, 0.28f), 0.72f),
                    new GradientColorKey(new Color(0.05f, 0.08f, 0.16f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateFogGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.04f, 0.07f, 0.14f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.6f, 0.46f), 0.28f),
                    new GradientColorKey(new Color(0.68f, 0.78f, 0.76f), 0.5f),
                    new GradientColorKey(new Color(0.62f, 0.36f, 0.28f), 0.75f),
                    new GradientColorKey(new Color(0.04f, 0.07f, 0.14f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
