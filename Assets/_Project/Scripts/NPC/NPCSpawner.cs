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
        [System.Serializable]
        private struct CustomerTrafficWindow
        {
            [Range(0f, 24f)] public float startHour;
            [Range(0f, 24f)] public float endHour;
            [Min(0.1f)] public float intervalMultiplier;

            public CustomerTrafficWindow(float startHour, float endHour, float intervalMultiplier)
            {
                this.startHour = startHour;
                this.endHour = endHour;
                this.intervalMultiplier = intervalMultiplier;
            }
        }

        [Header("Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform[] exitPoints;
        [SerializeField] private List<NPCProfile> availableProfiles;

        [Header("Game loop pacing")]
        [Tooltip("Thời gian tối thiểu khách sẽ chờ được phục vụ (giây).")]
        [SerializeField, Min(0f)] private float minimumCustomerWaitSeconds = 25f;
        [Tooltip("Hệ số nhân thời gian khách chờ được phục vụ.")]
        [SerializeField, Min(0.1f)] private float customerPatienceMultiplier = 1.5f;

        [Header("Customer traffic by hour")]
        [SerializeField] private float defaultTrafficMultiplier = 1.05f;
        [SerializeField] private CustomerTrafficWindow[] trafficSchedule =
        {
            new CustomerTrafficWindow(11f, 13f, 0.65f),
            new CustomerTrafficWindow(14f, 16f, 1.4f),
            new CustomerTrafficWindow(17f, 20f, 0.65f),
            new CustomerTrafficWindow(20f, 22f, 0.9f)
        };
        
        private float spawnTimer = 0f;
        private int currentCustomerCount = 0;
        private Economy.DayNightCycle dayNightCycle;
        private CustomerSeat[] cachedSeats;
        private readonly List<CustomerSeat> availableSeatBuffer = new List<CustomerSeat>();

        public int ActiveCustomerCount => currentCustomerCount;

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
            CacheSeats();
        }

        private void Update()
        {
            if (!GameManager.HasInstance || !GameManager.Instance.IsPlaying || 
                GameManager.Instance.CurrentPhase != GamePhase.Playing) 
                return;

            if (!Systems.BusinessDayController.HasInstance ||
                !Systems.BusinessDayController.Instance.IsManagingGameLoop ||
                !Systems.BusinessDayController.Instance.ShouldSpawnCustomers)
            {
                spawnTimer = 0f;
                return;
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

            if (Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop && dayNightCycle != null)
            {
                interval *= GetTrafficMultiplier(dayNightCycle.CurrentHour);
            }

            return interval;
        }

        private void TrySpawnNPC()
        {
            if (cachedSeats == null || cachedSeats.Length == 0)
            {
                CacheSeats();
            }

            availableSeatBuffer.Clear();
            for (int i = 0; i < cachedSeats.Length; i++)
            {
                CustomerSeat seat = cachedSeats[i];
                if (seat == null) continue;
                if (!seat.IsOccupied && !seat.IsPlayerOnly && seat.PlacedCupObj == null)
                {
                    availableSeatBuffer.Add(seat);
                }
            }

            if (availableSeatBuffer.Count == 0) return;
            if (exitPoints == null || exitPoints.Length == 0) return;

            CustomerSeat emptySeat = availableSeatBuffer[Random.Range(0, availableSeatBuffer.Count)];
            emptySeat.ReserveSeat();

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Transform exitPoint = exitPoints[Random.Range(0, exitPoints.Length)];
            
            NPCProfile profile = availableProfiles[Random.Range(0, availableProfiles.Count)];
            float speed = Random.Range(Constants.NPC_WALK_SPEED_MIN, Constants.NPC_WALK_SPEED_MAX);

            // Tạo NPC trực tiếp bằng code (không cần prefab)
            GameObject npcObj = new GameObject($"NPC_{profile.npcType}");
            npcObj.transform.position = spawnPoint.position;
            
            var controller = npcObj.AddComponent<NPCController>();
            controller.Initialize(profile, emptySeat, exitPoint, speed, customerPatienceMultiplier, minimumCustomerWaitSeconds);
            currentCustomerCount++;
        }

        private float GetTrafficMultiplier(float hour)
        {
            if (trafficSchedule != null)
            {
                for (int i = 0; i < trafficSchedule.Length; i++)
                {
                    CustomerTrafficWindow window = trafficSchedule[i];
                    bool inWindow = window.startHour <= window.endHour
                        ? hour >= window.startHour && hour < window.endHour
                        : hour >= window.startHour || hour < window.endHour;
                    if (inWindow)
                    {
                        return Mathf.Max(0.1f, window.intervalMultiplier);
                    }
                }
            }

            return Mathf.Max(0.1f, defaultTrafficMultiplier);
        }

        private void CacheSeats()
        {
            cachedSeats = FindObjectsByType<CustomerSeat>(FindObjectsInactive.Exclude);
        }

        private void OnCustomerRemoved(NPCType type)
        {
            currentCustomerCount--;
            if (currentCustomerCount < 0) currentCustomerCount = 0;
        }

        public void ClearAllCustomers()
        {
            NPCController[] customers = FindObjectsByType<NPCController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < customers.Length; i++)
            {
                if (customers[i] != null) Destroy(customers[i].gameObject);
            }
            currentCustomerCount = 0;
            spawnTimer = 0f;
        }
    }
}
