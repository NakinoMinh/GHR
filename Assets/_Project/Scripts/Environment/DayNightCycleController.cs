using GanhHangRong.Economy;
using UnityEngine;

namespace GanhHangRong.Environment
{
    [ExecuteAlways]
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
        [SerializeField] private Vector3 moonRotationOffset = new Vector3(90f, -10f, 0f);

        [Header("Hien thi mat troi / mat trang")]
        [SerializeField] private Vector3 visualCenter = new Vector3(76f, 0f, -18f);
        [SerializeField] private bool hideVisualBelowHorizon = true;
        [SerializeField, Min(10f)] private float celestialVisualDistance = 650f;
        [SerializeField, Min(1f)] private float sunVisualSize = 42f;
        [SerializeField, Min(1f)] private float moonVisualSize = 38f;

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
        private Material moonShadowMaterial;
        private Material cloudMaterial;

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
            ConfigureCoastalNightTiming();
            EnsureCelestialVisuals();
            EnsureWhiteClouds();
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

            ApplyDirectionalLight(sunLight, sunTransform, sunValue, sunColorGradient.Evaluate(normalizedTime), sunRotationOffset, normalizedTime);
            ApplyDirectionalLight(moonLight, moonTransform, moonValue, moonColorGradient.Evaluate(normalizedTime), moonRotationOffset, normalizedTime);
            ApplyCelestialVisual(sunVisual, sunLight, sunTransform, sunValue, sunColorGradient.Evaluate(normalizedTime), sunVisualSize);
            ApplyCelestialVisual(moonVisual, moonLight, moonTransform, moonValue, moonColorGradient.Evaluate(normalizedTime), moonVisualSize);
            UpdateCrescentMask(normalizedTime);
            UpdateClouds(normalizedTime);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColorGradient.Evaluate(normalizedTime) * ambientIntensity;

            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColorGradient.Evaluate(normalizedTime);
                RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, Mathf.Clamp01(fogDensityCurve.Evaluate(normalizedTime)));
            }
        }

        private void ApplyDirectionalLight(Light targetLight, Transform targetTransform, float intensity, Color color, Vector3 rotationOffset, float normalizedTime)
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
            pivot.rotation = Quaternion.Euler(normalizedTime * 360f + rotationOffset.x, rotationOffset.y, rotationOffset.z);
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
            bool isAboveHorizon = !hideVisualBelowHorizon || visual.position.y >= -15f;
            visual.gameObject.SetActive(isAboveHorizon && intensity > 0.01f);

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
                ReplaceWithCelestialSphere(ref sunVisual, "RealisticSun", new Color(1f, 0.78f, 0.32f, 1f), sunVisualSize, ref sunVisualMaterial);
            }
            else
            {
                sunVisual.localScale = Vector3.one * sunVisualSize;
            }

            if (moonVisual == null || !IsSphereVisual(moonVisual))
            {
                ReplaceWithCelestialSphere(ref moonVisual, "CrescentMoon", new Color(0.82f, 0.86f, 0.9f, 1f), moonVisualSize, ref moonVisualMaterial);
            }
            else
            {
                moonVisual.localScale = Vector3.one * moonVisualSize;
            }

            EnsureCrescentMask();
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
                DestroySafely(collider);
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

        private void ConfigureCoastalNightTiming()
        {
            moonRotationOffset = new Vector3(90f, -10f, 0f);
            sunIntensityCurve = CreateSunCurve();
            moonIntensityCurve = CreateMoonCurve();
        }

        private void EnsureCrescentMask()
        {
            if (moonVisual == null)
            {
                return;
            }

            Transform mask = moonVisual.Find("CrescentShadow");
            if (mask == null || !IsSphereVisual(mask))
            {
                if (mask != null)
                {
                    DestroySafely(mask.gameObject);
                }

                GameObject maskObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                maskObject.name = "CrescentShadow";
                maskObject.transform.SetParent(moonVisual, false);

                Collider collider = maskObject.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroySafely(collider);
                }

                Renderer renderer = maskObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    moonShadowMaterial = CreateCelestialMaterial("CrescentShadow_Material", new Color(0.05f, 0.08f, 0.16f, 1f));
                    renderer.sharedMaterial = moonShadowMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                mask = maskObject.transform;
            }

            mask.localPosition = new Vector3(0.28f, 0.02f, -0.04f);
            mask.localRotation = Quaternion.identity;
            mask.localScale = new Vector3(0.92f, 1.03f, 1.03f);
        }

        private void UpdateCrescentMask(float normalizedTime)
        {
            if (moonVisual == null)
            {
                return;
            }

            Transform mask = moonVisual.Find("CrescentShadow");
            if (mask == null)
            {
                return;
            }

            Color skyColor = fogColorGradient.Evaluate(normalizedTime);
            Renderer renderer = mask.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
                if (material != null)
                {
                    material.color = skyColor;
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", skyColor);
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", skyColor * 0.8f);
                    }
                }
            }

            mask.gameObject.SetActive(moonVisual.gameObject.activeSelf);
        }

        private void EnsureWhiteClouds()
        {
            Transform cloudRoot = transform.Find("WhiteClouds");
            if (cloudRoot == null)
            {
                GameObject root = new GameObject("WhiteClouds");
                root.transform.SetParent(transform, false);
                cloudRoot = root.transform;
            }

            if (cloudMaterial == null)
            {
                cloudMaterial = CreateCelestialMaterial("SoftWhiteCloud_Material", new Color(1f, 0.96f, 0.88f, 0.88f));
            }

            Vector3[] positions =
            {
                visualCenter + new Vector3(-170f, 135f, 420f),
                visualCenter + new Vector3(40f, 165f, 480f),
                visualCenter + new Vector3(220f, 130f, 360f),
                visualCenter + new Vector3(-60f, 210f, 520f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Transform cloud = cloudRoot.Find("Cloud_" + (i + 1).ToString("00"));
                if (cloud == null)
                {
                    GameObject cloudObject = new GameObject("Cloud_" + (i + 1).ToString("00"));
                    cloudObject.transform.SetParent(cloudRoot, false);
                    cloud = cloudObject.transform;
                    BuildCloudCluster(cloud, i);
                }

                cloud.position = positions[i];
                cloud.localRotation = Quaternion.Euler(0f, -18f + i * 9f, 0f);
            }
        }

        private void BuildCloudCluster(Transform cloud, int index)
        {
            Vector3[] offsets =
            {
                new Vector3(-1.3f, -0.05f, 0f),
                new Vector3(-0.35f, 0.25f, 0.1f),
                new Vector3(0.65f, 0.15f, -0.1f),
                new Vector3(1.45f, -0.08f, 0.05f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Puff_" + (i + 1).ToString("00");
                puff.transform.SetParent(cloud, false);
                puff.transform.localPosition = offsets[i] * 18f;
                puff.transform.localScale = new Vector3(34f + i * 7f, 15f + (i % 2) * 8f, 12f);

                Collider collider = puff.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroySafely(collider);
                }

                Renderer renderer = puff.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = cloudMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            cloud.localScale = Vector3.one * (1f + index * 0.12f);
        }

        private void UpdateClouds(float normalizedTime)
        {
            Transform cloudRoot = transform.Find("WhiteClouds");
            if (cloudRoot == null)
            {
                return;
            }

            Color cloudTint = Color.Lerp(new Color(0.95f, 0.94f, 1f, 0.72f), Color.white, sunIntensityCurve.Evaluate(normalizedTime));
            if (cloudMaterial != null)
            {
                cloudMaterial.color = cloudTint;
                if (cloudMaterial.HasProperty("_BaseColor"))
                {
                    cloudMaterial.SetColor("_BaseColor", cloudTint);
                }
            }

            for (int i = 0; i < cloudRoot.childCount; i++)
            {
                Transform cloud = cloudRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    float drift = Mathf.Sin(Time.time * 0.04f + i * 1.7f) * 8f;
                    Vector3 position = cloud.position;
                    position.x += drift * Time.deltaTime;
                    cloud.position = position;
                }
            }
        }

        private static void DestroySafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
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

        private static AnimationCurve CreateSunCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0f),
                new Keyframe(0.27f, 0.25f),
                new Keyframe(0.38f, 1f),
                new Keyframe(0.62f, 1f),
                new Keyframe(0.75f, 0.45f),
                new Keyframe(0.7708f, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateMoonCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.82f),
                new Keyframe(0.20f, 0.7f),
                new Keyframe(0.25f, 0f),
                new Keyframe(0.7708f, 0f),
                new Keyframe(0.790f, 0.35f),
                new Keyframe(0.86f, 0.78f),
                new Keyframe(1f, 0.82f));
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
                    new GradientColorKey(new Color(1f, 0.58f, 0.34f), 0.23f),
                    new GradientColorKey(new Color(1f, 0.93f, 0.78f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.52f, 0.25f), 0.72f)
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
                    new GradientColorKey(new Color(0.5f, 0.62f, 1f), 0f),
                    new GradientColorKey(new Color(0.78f, 0.86f, 1f), 1f)
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
