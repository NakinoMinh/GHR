using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Weather;
using System.Collections.Generic;

namespace GanhHangRong.Audio
{
    [System.Serializable]
    public class AmbientLayer
    {
        public string layerName;
        public AudioSource source;
        public float maxVolume = 1f;
        [HideInInspector] public float targetVolume = 0f;
    }

    /// <summary>
    /// Điều khiển bầu không khí (biển, gió, mưa, côn trùng) dựa trên thời tiết và trạng thái cảm xúc.
    /// </summary>
    public class AmbienceController : MonoBehaviour
    {
        [SerializeField] private List<AmbientLayer> layers = new List<AmbientLayer>();
        [SerializeField] private float fadeSpeed = 1f;

        private void OnEnable()
        {
            EventManager.OnWeatherChanged += HandleWeatherChanged;
            EventManager.OnTimeOfDayChanged += HandleTimeChanged;
            EventManager.OnEmotionalLevelChanged += HandleEmotionChanged;
        }

        private void OnDisable()
        {
            EventManager.OnWeatherChanged -= HandleWeatherChanged;
            EventManager.OnTimeOfDayChanged -= HandleTimeChanged;
            EventManager.OnEmotionalLevelChanged -= HandleEmotionChanged;
        }

        private void Update()
        {
            foreach (var layer in layers)
            {
                if (layer.source == null) continue;

                // Lerp volume về target
                float currentVol = layer.source.volume;
                if (!Mathf.Approximately(currentVol, layer.targetVolume))
                {
                    layer.source.volume = Mathf.MoveTowards(currentVol, layer.targetVolume, fadeSpeed * Time.deltaTime);
                }

                if (layer.source.volume > 0 && !layer.source.isPlaying)
                {
                    layer.source.Play();
                }
                else if (layer.source.volume == 0 && layer.source.isPlaying)
                {
                    layer.source.Stop();
                }
            }
        }

        public void SetLayerVolume(string name, float normalizedVolume)
        {
            var layer = layers.Find(l => l.layerName == name);
            if (layer != null)
            {
                layer.targetVolume = Mathf.Clamp01(normalizedVolume) * layer.maxVolume;
            }
        }

        private void HandleWeatherChanged(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear:
                    SetLayerVolume("Rain", 0f);
                    SetLayerVolume("Wind", 0.2f);
                    SetLayerVolume("Ocean", 0.5f);
                    break;
                case WeatherType.LightRain:
                    SetLayerVolume("Rain", 0.5f);
                    SetLayerVolume("Wind", 0.3f);
                    SetLayerVolume("Ocean", 0.6f);
                    break;
                case WeatherType.HeavyRain:
                    SetLayerVolume("Rain", 1f);
                    SetLayerVolume("Wind", 0.8f);
                    SetLayerVolume("Ocean", 0.8f);
                    SetLayerVolume("Insects", 0f); // Mưa to lấn át côn trùng
                    break;
                case WeatherType.SeaWind:
                    SetLayerVolume("Wind", 1f);
                    SetLayerVolume("Ocean", 1f);
                    break;
            }
        }

        private void HandleTimeChanged(TimeOfDay time)
        {
            if (time == TimeOfDay.Night || time == TimeOfDay.LateNight)
            {
                if (WeatherManager.HasInstance && WeatherManager.Instance.CurrentPreset.rainIntensity < 0.5f)
                {
                    SetLayerVolume("Insects", 0.8f);
                }
                SetLayerVolume("Harbor", 0.2f);
            }
            else
            {
                SetLayerVolume("Insects", 0f);
                SetLayerVolume("Harbor", 0.8f); // Bến tàu tấp nập ban ngày
            }
        }

        private void HandleEmotionChanged(EmotionalLevel level)
        {
            // Trạng thái tuyệt vọng làm tiếng gió gào to hơn, không khí vắng lặng hơn
            if (level == EmotionalLevel.Lonely || level == EmotionalLevel.Desperate)
            {
                SetLayerVolume("Wind", Mathf.Max(GetLayerTarget("Wind"), 0.6f));
                SetLayerVolume("Harbor", 0f);
            }
        }

        private float GetLayerTarget(string name)
        {
            var layer = layers.Find(l => l.layerName == name);
            return layer != null ? (layer.targetVolume / layer.maxVolume) : 0f;
        }
    }
}
