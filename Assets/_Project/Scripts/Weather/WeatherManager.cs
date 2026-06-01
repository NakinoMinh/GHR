using UnityEngine;
using GanhHangRong.Core;
using System.Collections;
using System.Collections.Generic;

namespace GanhHangRong.Weather
{
    /// <summary>
    /// Quản lý trạng thái thời tiết toàn cục, thực hiện chuyển đổi mượt mà.
    /// </summary>
    public class WeatherManager : Singleton<WeatherManager>
    {
        [Header("Presets")]
        [SerializeField] private List<WeatherPreset> weatherPresets = new List<WeatherPreset>();
        [SerializeField] private WeatherType startingWeather = WeatherType.Clear;

        [Header("Lighting")]
        [SerializeField] private Light globalLight; // Directional Light

        private WeatherPreset currentPreset;
        private Coroutine transitionCoroutine;

        public WeatherPreset CurrentPreset => currentPreset;

        protected override void OnSingletonAwake()
        {
            if (globalLight == null)
            {
                globalLight = FindAnyObjectByType<Light>();
            }
            
            SetWeatherImmediate(startingWeather);
        }

        private void OnEnable()
        {
            EventManager.OnNewDay += RollRandomWeather;
        }

        private void OnDisable()
        {
            EventManager.OnNewDay -= RollRandomWeather;
        }

        public void ChangeWeather(WeatherType type, float duration = Constants.WEATHER_TRANSITION_DURATION)
        {
            WeatherPreset targetPreset = weatherPresets.Find(p => p.weatherType == type);
            if (targetPreset == null)
            {
                Debug.LogWarning($"Không tìm thấy WeatherPreset cho {type}");
                return;
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionWeather(targetPreset, duration));
            
            EventManager.TriggerWeatherChanged(type);
        }

        private void SetWeatherImmediate(WeatherType type)
        {
            WeatherPreset targetPreset = weatherPresets.Find(p => p.weatherType == type);
            if (targetPreset == null) return;

            currentPreset = targetPreset;
            ApplyWeatherVisuals(currentPreset);
            EventManager.TriggerRainIntensityChanged(currentPreset.rainIntensity);
        }

        private IEnumerator TransitionWeather(WeatherPreset target, float duration)
        {
            WeatherPreset start = currentPreset;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Lerp Light
                if (globalLight != null)
                {
                    globalLight.color = Color.Lerp(start.ambientLightColor, target.ambientLightColor, t);
                    globalLight.intensity = Mathf.Lerp(start.ambientLightIntensity, target.ambientLightIntensity, t);
                }

                // Lerp Fog
                RenderSettings.fogColor = Color.Lerp(start.fogColor, target.fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(start.fogDensity, target.fogDensity, t);

                // Gửi event cho các hệ thống khác (mưa, gió)
                float currentRainIntensity = Mathf.Lerp(start.rainIntensity, target.rainIntensity, t);
                EventManager.TriggerRainIntensityChanged(currentRainIntensity);

                yield return null;
            }

            currentPreset = target;
            ApplyWeatherVisuals(currentPreset);
            transitionCoroutine = null;
        }

        private void ApplyWeatherVisuals(WeatherPreset preset)
        {
            if (globalLight != null)
            {
                globalLight.color = preset.ambientLightColor;
                globalLight.intensity = preset.ambientLightIntensity;
            }
            RenderSettings.fog = true;
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        private void RollRandomWeather()
        {
            // Logic đơn giản để chọn thời tiết ngẫu nhiên mỗi ngày mới
            int random = Random.Range(0, 100);
            if (random < 50) ChangeWeather(WeatherType.Clear, 2f);
            else if (random < 70) ChangeWeather(WeatherType.Foggy, 5f);
            else if (random < 90) ChangeWeather(WeatherType.LightRain, 5f);
            else ChangeWeather(WeatherType.HeavyRain, 5f);
        }
    }
}
