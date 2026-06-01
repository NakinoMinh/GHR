using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Weather
{
    /// <summary>
    /// Điều khiển hệ thống Particle cho mưa, liên kết với độ ẩm đường (WetGround shader).
    /// </summary>
    public class RainSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem splashParticles; // Hạt mưa chạm đất

        [Header("Settings")]
        [SerializeField] private float maxEmissionRate = 2000f;
        // [SerializeField] private float rainWetnessSpeed = 0.5f; // Tốc độ làm ướt đường

        private ParticleSystem.EmissionModule rainEmission;
        private ParticleSystem.EmissionModule splashEmission;

        private void Awake()
        {
            if (rainParticles != null) rainEmission = rainParticles.emission;
            if (splashParticles != null) splashEmission = splashParticles.emission;
        }

        private void OnEnable()
        {
            EventManager.OnRainIntensityChanged += HandleRainIntensity;
        }

        private void OnDisable()
        {
            EventManager.OnRainIntensityChanged -= HandleRainIntensity;
        }

        private void HandleRainIntensity(float intensity)
        {
            if (rainParticles == null) return;

            float targetEmission = intensity * maxEmissionRate;
            
            rainEmission.rateOverTime = targetEmission;
            
            if (splashParticles != null)
            {
                splashEmission.rateOverTime = targetEmission * 0.1f; // Splash ít hơn mưa một chút
            }

            if (intensity > 0 && !rainParticles.isPlaying)
            {
                rainParticles.Play();
                if (splashParticles != null) splashParticles.Play();
            }
            else if (intensity <= 0 && rainParticles.isPlaying)
            {
                rainParticles.Stop();
                if (splashParticles != null) splashParticles.Stop();
            }

            // Cập nhật giá trị Wetness cho vật liệu đường
            // Trong thực tế sẽ gửi vào shader global
            Shader.SetGlobalFloat("_GlobalWetness", intensity);
        }
    }
}
