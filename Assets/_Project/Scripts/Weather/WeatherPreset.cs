using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Weather
{
    [CreateAssetMenu(fileName = "NewWeatherPreset", menuName = "Gánh Hàng Rong/Thời Tiết/Weather Preset")]
    public class WeatherPreset : ScriptableObject
    {
        public WeatherType weatherType;
        
        [Header("Visuals")]
        public Color ambientLightColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        public float ambientLightIntensity = 1f;
        public float fogDensity = 0.01f;
        public Color fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);
        
        [Header("Rain")]
        [Range(0f, 1f)]
        public float rainIntensity = 0f; // 0 = ko mưa, 1 = mưa to
        public float rainEmissionRate = 0f;
        
        [Header("Wind")]
        [Range(0f, 1f)]
        public float windStrength = 0f;
        
        [Header("Gameplay Modifiers")]
        [Tooltip("Hệ số số lượng khách hàng xuất hiện. 1 = Bình thường, 0.5 = Giảm một nửa")]
        public float customerSpawnModifier = 1f;
        
        [Tooltip("Hệ số tốc độ đá tan. Mưa/lạnh thì đá tan chậm hơn.")]
        public float iceMeltModifier = 1f;
    }
}
