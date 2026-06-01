using UnityEngine;

namespace GanhHangRong.Weather
{
    /// <summary>
    /// Quản lý sức gió, thay đổi hướng của particle mưa hoặc tác động lên shader cây cỏ (nếu có).
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        [SerializeField] private Vector2 baseWindDirection = new Vector2(-1f, 0f); // Mặc định thổi từ phải qua trái (từ biển vào bờ)
        [SerializeField] private float windStrengthMultiplier = 5f;
        
        private ParticleSystem targetParticles;

        public void Initialize(ParticleSystem rainParticles)
        {
            if (rainParticles != null)
            {
                targetParticles = rainParticles;
                var forceModule = targetParticles.forceOverLifetime;
                forceModule.enabled = true;
            }
        }

        private void Update()
        {
            if (WeatherManager.HasInstance && WeatherManager.Instance.CurrentPreset != null)
            {
                float strength = WeatherManager.Instance.CurrentPreset.windStrength;
                
                if (targetParticles != null)
                {
                    var forceModule = targetParticles.forceOverLifetime;
                    forceModule.x = baseWindDirection.x * strength * windStrengthMultiplier;
                    forceModule.y = baseWindDirection.y * strength * windStrengthMultiplier;
                }
                
                // Truyền tham số gió vào shader (nước, lá cây, v.v.)
                Shader.SetGlobalVector("_WindParams", new Vector4(baseWindDirection.x * strength, 0, 0, 0));
            }
        }
    }
}
