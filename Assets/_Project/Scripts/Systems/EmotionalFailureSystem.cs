using UnityEngine;
using GanhHangRong.Core;
using System.Collections;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Hệ thống đánh giá hiệu suất của người chơi và chuyển đổi cảm xúc (âm thanh, ánh sáng, NPC spawn)
    /// </summary>
    public class EmotionalFailureSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light globalLight;

        private float checkTimer = 0f;
        private Coroutine lightDimCoroutine;

        private void Update()
        {
            if (!GameManager.Instance.IsPlaying) return;
            
            // Chỉ kiểm tra vào ban đêm (khi bán hàng)
            var timeManager = FindAnyObjectByType<Economy.DayNightCycle>();
            if (timeManager == null || timeManager.CurrentTimeOfDay != TimeOfDay.Night) 
                return;

            checkTimer += Time.deltaTime;
            if (checkTimer >= Constants.EMOTIONAL_UPDATE_INTERVAL)
            {
                checkTimer = 0f;
                EvaluateEmotionalState();
            }
        }

        private void EvaluateEmotionalState()
        {
            var stats = FindAnyObjectByType<Player.PlayerStats>();
            if (stats == null) return;

            int served = stats.CustomersServedToday;
            EmotionalLevel newLevel = EmotionalLevel.Normal;

            if (served >= Constants.CUSTOMERS_THRESHOLD_GOOD) newLevel = EmotionalLevel.Hopeful;
            else if (served >= Constants.CUSTOMERS_THRESHOLD_OK) newLevel = EmotionalLevel.Normal;
            else if (served >= Constants.CUSTOMERS_THRESHOLD_BAD) newLevel = EmotionalLevel.Struggling;
            else if (served > 0) newLevel = EmotionalLevel.Lonely;
            else newLevel = EmotionalLevel.Desperate;

            if (newLevel != GameManager.Instance.CurrentEmotionalLevel)
            {
                GameManager.Instance.SetEmotionalLevel(newLevel);
                AdjustAtmosphere(newLevel);
            }
        }

        private void AdjustAtmosphere(EmotionalLevel level)
        {
            float targetDim = 1f;
            
            switch (level)
            {
                case EmotionalLevel.Hopeful: targetDim = 1.1f; break; // Sáng sủa
                case EmotionalLevel.Normal: targetDim = 1f; break;
                case EmotionalLevel.Struggling: targetDim = 0.8f; break;
                case EmotionalLevel.Lonely: targetDim = Constants.LIGHT_DIM_LONELY; break;
                case EmotionalLevel.Desperate: targetDim = Constants.LIGHT_DIM_DESPERATE; break; // Rất mờ
            }

            if (globalLight != null)
            {
                if (lightDimCoroutine != null) StopCoroutine(lightDimCoroutine);
                lightDimCoroutine = StartCoroutine(SmoothDimLight(targetDim));
            }
            
            // NPCSpawner và AmbienceController đã đăng ký lắng nghe sự kiện EmotionalLevelChanged ở EventManager
        }

        private IEnumerator SmoothDimLight(float targetMultiplier)
        {
            if (globalLight == null || Weather.WeatherManager.Instance == null) yield break;

            float startIntensity = globalLight.intensity;
            // Lấy cường độ gốc từ WeatherPreset
            float baseIntensity = Weather.WeatherManager.Instance.CurrentPreset.ambientLightIntensity;
            float targetIntensity = baseIntensity * targetMultiplier;
            float duration = 10f; // Chuyển đổi rất chậm để cảm xúc ngấm dần
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
                yield return null;
            }
        }
    }
}
