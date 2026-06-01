using UnityEngine;

namespace GanhHangRong.Weather
{
    /// <summary>
    /// Quản lý thêm particle sương mù cuộn trên mặt đất/biển (ngoài sương mù của render settings).
    /// </summary>
    public class FogController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private float maxFogEmission = 50f;

        private ParticleSystem.EmissionModule fogEmission;

        private void Awake()
        {
            if (fogParticles != null)
            {
                fogEmission = fogParticles.emission;
            }
        }

        private void Update()
        {
            if (WeatherManager.HasInstance && WeatherManager.Instance.CurrentPreset != null && fogParticles != null)
            {
                float targetDensity = WeatherManager.Instance.CurrentPreset.fogDensity;
                
                // Map fog density từ render settings sang emission rate
                // (Giả sử density chạy từ 0 đến 0.05)
                float normalizedDensity = Mathf.Clamp01(targetDensity / 0.05f); 
                
                fogEmission.rateOverTime = normalizedDensity * maxFogEmission;
                
                if (normalizedDensity > 0.01f && !fogParticles.isPlaying)
                {
                    fogParticles.Play();
                }
                else if (normalizedDensity <= 0.01f && fogParticles.isPlaying)
                {
                    fogParticles.Stop();
                }
            }
        }
    }
}
