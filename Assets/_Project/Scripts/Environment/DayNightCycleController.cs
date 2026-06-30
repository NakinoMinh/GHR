using UnityEngine;

namespace GanhHangRong.Environment
{
    public class DayNightCycleController : MonoBehaviour
    {
        [Header("Nguon thoi gian")]
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
        [SerializeField] private Vector3 visualCenter = new Vector3(76f, 0f, -18f);
        [SerializeField, Min(1f)] private float visualOrbitRadius = 80f;
        [SerializeField, Min(0f)] private float visualHorizonHeight = 8f;
        [SerializeField] private float visualSeaSideOffset = -55f;
        [SerializeField] private float visualForwardOffset = -95f;
        [SerializeField, Min(1f)] private float visualVerticalRadius = 70f;
        [SerializeField] private bool hideVisualBelowHorizon = true;

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

        private void Reset()
        {
            timeManager = FindAnyObjectByType<TimeOfDayManager>();
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

            CacheLightTransforms();
        }

        private void Update()
        {
            if (timeManager == null)
            {
                if (!warnedMissingTimeManager)
                {
                    Debug.LogWarning($"{nameof(DayNightCycleController)} can TimeOfDayManager de cap nhat anh sang.", this);
                    warnedMissingTimeManager = true;
                }

                return;
            }

            ApplyLighting(timeManager.NormalizedTime);
        }

        public void ApplyLighting(float normalizedTime)
        {
            normalizedTime = Mathf.Repeat(normalizedTime, 1f);

            float sunValue = Mathf.Clamp01(sunIntensityCurve.Evaluate(normalizedTime)) * maxSunIntensity;
            float moonValue = Mathf.Clamp01(moonIntensityCurve.Evaluate(normalizedTime)) * maxMoonIntensity;

            ApplyDirectionalLight(sunLight, sunTransform, sunValue, sunColorGradient.Evaluate(normalizedTime), sunRotationOffset, normalizedTime);
            ApplyDirectionalLight(moonLight, moonTransform, moonValue, moonColorGradient.Evaluate(normalizedTime), moonRotationOffset, normalizedTime);
            ApplyCelestialVisual(sunVisual, normalizedTime, 0f, sunValue, sunColorGradient.Evaluate(normalizedTime));
            ApplyCelestialVisual(moonVisual, normalizedTime, 180f, moonValue, moonColorGradient.Evaluate(normalizedTime));

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

        private void ApplyCelestialVisual(Transform visual, float normalizedTime, float phaseDegrees, float intensity, Color color)
        {
            if (visual == null)
            {
                return;
            }

            // 06:00 nam o chan troi, 12:00 len cao, 18:00 xuong chan troi phia bien.
            float angle = normalizedTime * 360f - 90f + phaseDegrees;
            float radians = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians) * visualOrbitRadius;
            float y = Mathf.Sin(radians) * visualVerticalRadius + visualHorizonHeight;
            float sunsetBlend = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) / 90f);
            float sunriseBlend = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) / 90f);
            float horizonBlend = Mathf.Max(sunsetBlend, sunriseBlend);
            float z = Mathf.Lerp(0f, visualForwardOffset, horizonBlend);

            visual.position = visualCenter + new Vector3(x + visualSeaSideOffset * sunsetBlend, y, z);
            visual.gameObject.SetActive(!hideVisualBelowHorizon || y >= visualHorizonHeight - 0.01f || intensity > 0.01f);

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
