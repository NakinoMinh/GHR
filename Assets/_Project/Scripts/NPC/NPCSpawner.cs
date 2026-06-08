using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Interaction;
using GanhHangRong.Weather;
using System.Collections.Generic;

namespace GanhHangRong.NPC
{
    /// <summary>
    /// Quản lý việc sinh ra khách hàng dựa trên thời gian, thời tiết và trạng thái cảm xúc.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform[] exitPoints;
        [SerializeField] private List<NPCProfile> availableProfiles;
        
        private float spawnTimer = 0f;
        private int currentCustomerCount = 0;
        private Economy.DayNightCycle dayNightCycle;

        private void OnEnable()
        {
            EventManager.OnCustomerLeftHappy += OnCustomerRemoved;
            EventManager.OnCustomerLeftSad += OnCustomerRemoved;
        }

        private void OnDisable()
        {
            EventManager.OnCustomerLeftHappy -= OnCustomerRemoved;
            EventManager.OnCustomerLeftSad -= OnCustomerRemoved;
        }

        private void Start()
        {
            dayNightCycle = FindAnyObjectByType<Economy.DayNightCycle>();
        }

        private void Update()
        {
            if (!GameManager.HasInstance || !GameManager.Instance.IsPlaying || 
                GameManager.Instance.CurrentPhase != GamePhase.Playing) 
                return;

            // Chỉ spawn khách vào ban đêm hoặc buổi tối từ 18:00 trở đi
            if (dayNightCycle == null)
                dayNightCycle = FindAnyObjectByType<Economy.DayNightCycle>();

            if (dayNightCycle != null)
            {
                float hour = dayNightCycle.CurrentHour;
                // Chỉ spawn khách từ 18:00 đến 01:00 sáng hôm sau
                if (hour < 18f && hour >= 1f)
                {
                    spawnTimer = 0f;
                    return;
                }
            }

            if (currentCustomerCount >= Constants.MAX_CONCURRENT_CUSTOMERS) return;
            if (availableProfiles == null || availableProfiles.Count == 0) return;
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            spawnTimer += Time.deltaTime;

            float currentSpawnInterval = CalculateSpawnInterval();

            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                TrySpawnNPC();
            }
        }

        private float CalculateSpawnInterval()
        {
            float interval = Constants.NPC_SPAWN_INTERVAL_BASE;
            
            // Thời tiết ảnh hưởng
            if (WeatherManager.HasInstance && WeatherManager.Instance.CurrentPreset != null)
            {
                float modifier = WeatherManager.Instance.CurrentPreset.customerSpawnModifier;
                if (modifier > 0) interval /= modifier;
            }

            // Emotional level ảnh hưởng
            switch (GameManager.Instance.CurrentEmotionalLevel)
            {
                case EmotionalLevel.Hopeful: interval *= 0.8f; break;
                case EmotionalLevel.Normal: break;
                case EmotionalLevel.Struggling: interval *= 1.5f; break;
                case EmotionalLevel.Lonely: interval *= 2.5f; break;
                case EmotionalLevel.Desperate: interval *= 4f; break;
            }

            return interval;
        }

        private void TrySpawnNPC()
        {
            // Tìm ghế trống
            var allSeats = FindObjectsByType<CustomerSeat>(FindObjectsInactive.Exclude);
            CustomerSeat emptySeat = null;
            
            List<CustomerSeat> seatList = new List<CustomerSeat>(allSeats);
            for (int i = 0; i < seatList.Count; i++)
            {
                int r = Random.Range(i, seatList.Count);
                var temp = seatList[i];
                seatList[i] = seatList[r];
                seatList[r] = temp;
            }

            foreach (var seat in seatList)
            {
                if (!seat.IsOccupied && !seat.IsPlayerOnly)
                {
                    emptySeat = seat;
                    break;
                }
            }

            if (emptySeat == null) return;
            if (exitPoints == null || exitPoints.Length == 0) return;

            emptySeat.OccupySeat();

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Transform exitPoint = exitPoints[Random.Range(0, exitPoints.Length)];
            
            NPCProfile profile = availableProfiles[Random.Range(0, availableProfiles.Count)];
            float speed = Random.Range(Constants.NPC_WALK_SPEED_MIN, Constants.NPC_WALK_SPEED_MAX);

            // Tạo NPC trực tiếp bằng code (không cần prefab)
            GameObject npcObj = new GameObject($"NPC_{profile.npcType}");
            npcObj.transform.position = spawnPoint.position;
            
            var controller = npcObj.AddComponent<NPCController>();
            controller.Initialize(profile, emptySeat, exitPoint, speed);
            currentCustomerCount++;
        }

        private void OnCustomerRemoved(NPCType type)
        {
            currentCustomerCount--;
            if (currentCustomerCount < 0) currentCustomerCount = 0;
        }
    }
}
