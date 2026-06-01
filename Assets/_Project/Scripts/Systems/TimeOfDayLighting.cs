using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Điều khiển ánh sáng môi trường thay đổi mượt mà theo từng buổi trong ngày.
    /// </summary>
    public class TimeOfDayLighting : MonoBehaviour
    {
        [Header("Lighting")]
        [SerializeField] private Light globalLight;
        
        [Header("Colors (Buổi)")]
        [SerializeField] private Color colorEarlyMorning = new Color(1f, 0.8f, 0.7f); // Cam hồng
        [SerializeField] private Color colorMorning = new Color(1f, 0.95f, 0.8f);     // Vàng ấm
        [SerializeField] private Color colorAfternoon = new Color(1f, 1f, 0.95f);     // Trắng nắng
        [SerializeField] private Color colorEvening = new Color(1f, 0.6f, 0.4f);      // Cam đỏ
        [SerializeField] private Color colorNight = new Color(0.2f, 0.2f, 0.5f);      // Xanh tím đêm
        [SerializeField] private Color colorLateNight = new Color(0.1f, 0.1f, 0.2f);  // Xanh đen

        [Header("Intensity")]
        [SerializeField] private float intensityMorning = 0.7f;
        [SerializeField] private float intensityAfternoon = 1f;
        [SerializeField] private float intensityEvening = 0.5f;
        [SerializeField] private float intensityNight = 0.15f;

        private void Start()
        {
            if (globalLight == null)
            {
                globalLight = FindAnyObjectByType<Light>();
            }
        }

        private void OnEnable()
        {
            EventManager.OnHourChanged += UpdateLighting;
        }

        private void OnDisable()
        {
            EventManager.OnHourChanged -= UpdateLighting;
        }

        private void UpdateLighting(float hour)
        {
            if (globalLight == null) return;

            // Xử lý Lerp giữa các mốc thời gian
            if (hour >= 5f && hour < 7f)
            {
                float t = (hour - 5f) / 2f;
                globalLight.color = Color.Lerp(colorEarlyMorning, colorMorning, t);
                globalLight.intensity = Mathf.Lerp(0.4f, intensityMorning, t);
            }
            else if (hour >= 7f && hour < 11f)
            {
                float t = (hour - 7f) / 4f;
                globalLight.color = Color.Lerp(colorMorning, colorAfternoon, t);
                globalLight.intensity = Mathf.Lerp(intensityMorning, intensityAfternoon, t);
            }
            else if (hour >= 11f && hour < 17f)
            {
                float t = (hour - 11f) / 6f;
                globalLight.color = colorAfternoon; // Trưa giữ nguyên màu
                globalLight.intensity = Mathf.Lerp(intensityAfternoon, 0.8f, t);
            }
            else if (hour >= 17f && hour < 20f)
            {
                float t = (hour - 17f) / 3f;
                globalLight.color = Color.Lerp(colorAfternoon, colorEvening, t);
                globalLight.intensity = Mathf.Lerp(0.8f, intensityEvening, t);
            }
            else if (hour >= 20f && hour < 24f)
            {
                float t = (hour - 20f) / 4f;
                globalLight.color = Color.Lerp(colorEvening, colorNight, t);
                globalLight.intensity = Mathf.Lerp(intensityEvening, intensityNight, t);
            }
            else if (hour >= 0f && hour < 1f)
            {
                // Từ 00:00 đến 01:00 vẫn là màu đêm
                globalLight.color = colorNight;
                globalLight.intensity = intensityNight;
            }
            else if (hour >= 1f && hour < 5f)
            {
                float t = (hour - 1f) / 4f;
                globalLight.color = Color.Lerp(colorNight, colorLateNight, t);
                globalLight.intensity = Mathf.Lerp(intensityNight, 0.08f, t);
            }
        }
    }
}
