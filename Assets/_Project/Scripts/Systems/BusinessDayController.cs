using GanhHangRong.Core;
using GanhHangRong.Economy;
using GanhHangRong.Interaction;
using GanhHangRong.NPC;
using GanhHangRong.Player;
using GanhHangRong.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Điều phối vòng ngày. Không sở hữu logic pha chế; chỉ quyết định khi nào quán được mở,
    /// sinh khách, đóng cửa và chuyển ngày.
    /// </summary>
    [DisallowMultipleComponent]
    public class BusinessDayController : MonoBehaviour
    {
        private static BusinessDayController instance;

        public static BusinessDayController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<BusinessDayController>();
                }

                return instance;
            }
            private set => instance = value;
        }

        public static bool HasInstance => Instance != null;

        [Header("Mốc thời gian")]
        [SerializeField] private float wakeHour = 6f;
        [SerializeField] private float lateWakeHour = 7f;
        [SerializeField] private float preparationHour = 8f;
        [SerializeField] private float closingHour = 22f;

        [Header("Điểm tham chiếu tùy chọn")]
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private TeaCart teaCart;
        [SerializeField] private SleepBed sleepBed;
        [SerializeField] private NPCSpawner npcSpawner;
        [SerializeField] private PlayerController player;
        [SerializeField] private float wakeOffsetFromBed = 1.8f;

        [Header("Runtime")]
        [SerializeField] private BusinessDayPhase currentPhase = BusinessDayPhase.PreOpen;
        [SerializeField] private bool lateReturnPenalty;

        private bool initialized;
        private bool awaitingOpenConfirmation;
        private bool awaitingEarlyCloseConfirmation;
        private bool closingNoticeShown;
        private float previousHour;
        private float nextClosingCheckTime;
        private bool gameplaySceneActive;
        private int gameplaySceneHandle = -1;

        public BusinessDayPhase CurrentPhase => currentPhase;
        public bool IsManagingGameLoop => gameplaySceneActive;
        public bool CanEditMenu => !IsManagingGameLoop || currentPhase == BusinessDayPhase.Preparation;
        public bool CanServeCustomers => IsManagingGameLoop &&
            (currentPhase == BusinessDayPhase.Trading || currentPhase == BusinessDayPhase.Closing);
        public bool ShouldSpawnCustomers => IsManagingGameLoop && currentPhase == BusinessDayPhase.Trading &&
            dayNightCycle != null && dayNightCycle.CurrentHour >= preparationHour && dayNightCycle.CurrentHour < closingHour;
        public bool CanSleep => IsManagingGameLoop && currentPhase == BusinessDayPhase.AfterHours;
        public bool CanCloseAtClosingPoint => IsManagingGameLoop &&
            currentPhase == BusinessDayPhase.Trading &&
            dayNightCycle != null && dayNightCycle.CurrentHour >= closingHour;
        public bool HasPendingConfirmation => awaitingOpenConfirmation || awaitingEarlyCloseConfirmation;
        public bool HasLateReturnPenalty => lateReturnPenalty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameLoopController()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsGameplaySceneLoaded(scene) || !GameManager.HasInstance) return;

            GameObject host = GameManager.Instance.gameObject;
            BusinessDayController controller = host.GetComponent<BusinessDayController>();
            if (controller == null)
            {
                if (Instance != null && Instance.gameObject != host)
                {
                    Instance.enabled = false;
                    Instance = null;
                }
                controller = host.AddComponent<BusinessDayController>();
            }
            controller.ActivateForScene(scene);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (GetComponent<DailyBusinessLedger>() == null)
            {
                gameObject.AddComponent<DailyBusinessLedger>();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            ActivateForScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private static bool IsGameplaySceneLoaded(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene != SceneManager.GetActiveScene()) return false;

            return FindAnyObjectByType<DayNightCycle>(FindObjectsInactive.Include) != null &&
                   FindAnyObjectByType<TeaCart>(FindObjectsInactive.Include) != null &&
                   FindAnyObjectByType<SleepBed>(FindObjectsInactive.Include) != null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ActivateForScene(scene);
        }

        private void ActivateForScene(Scene scene)
        {
            bool shouldManage = IsGameplaySceneLoaded(scene);
            gameplaySceneActive = shouldManage;
            if (!shouldManage)
            {
                gameplaySceneHandle = -1;
                return;
            }

            bool enteredNewGameplayScene = gameplaySceneHandle != scene.handle;
            gameplaySceneHandle = scene.handle;
            ResolveReferences();
            if (enteredNewGameplayScene || !initialized)
            {
                initialized = false;
                StartFreshDay(false);
            }
        }

        private void Update()
        {
            if (!IsManagingGameLoop || !GameManager.Instance.IsPlaying) return;
            ResolveReferences();
            if (dayNightCycle == null) return;

            HandleConfirmationInput();

            float hour = dayNightCycle.CurrentHour;
            if (currentPhase == BusinessDayPhase.PreOpen && hour >= preparationHour && hour < closingHour)
            {
                BeginPreparation();
            }

            if (currentPhase == BusinessDayPhase.Trading && hour >= closingHour && !closingNoticeShown)
            {
                closingNoticeShown = true;
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã 22:00. Đến biển ĐÓNG QUÁN cạnh xe và nhấn F để đóng cửa.");
            }

            if (currentPhase == BusinessDayPhase.Closing && Time.unscaledTime >= nextClosingCheckTime)
            {
                nextClosingCheckTime = Time.unscaledTime + 0.5f;
                if (GetActiveCustomerCount() == 0)
                {
                    EnterAfterHours();
                }
            }

            bool crossedMidnight = previousHour > 23f && hour < 1f;
            previousHour = hour;
            if (crossedMidnight && currentPhase != BusinessDayPhase.DaySummary)
            {
                HandleMidnightTimeout();
            }
        }

        public bool TryHandleCartInteractionOverride()
        {
            if (!IsManagingGameLoop) return false;

            if (currentPhase == BusinessDayPhase.PreOpen)
            {
                BeginPreparation();
                return true;
            }

            return false;
        }

        public bool TryCloseShopAtClosingPoint()
        {
            if (!CanCloseAtClosingPoint) return false;

            CloseShop(false);
            return true;
        }

        public string GetCartPrompt(string defaultPrompt)
        {
            if (!IsManagingGameLoop) return defaultPrompt;

            switch (currentPhase)
            {
                case BusinessDayPhase.PreOpen:
                    return "Nhấn F để bắt đầu chuẩn bị quán lúc 08:00";
                case BusinessDayPhase.Preparation:
                    return "F: tương tác xe | Tab: thực đơn | Space: mở cửa";
                case BusinessDayPhase.Trading:
                    return defaultPrompt;
                case BusinessDayPhase.Closing:
                    return defaultPrompt;
                case BusinessDayPhase.AfterHours:
                    return "Quán đã đóng - hãy trở về nhà nghỉ ngơi";
                default:
                    return defaultPrompt;
            }
        }

        public bool RequestEarlyCloseFromMenu()
        {
            if (!IsManagingGameLoop || currentPhase != BusinessDayPhase.Trading) return false;
            if (teaCart != null && teaCart.IsPlayerInteracting)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy thoát khỏi góc pha chế trước khi đóng quán.");
                return false;
            }

            awaitingEarlyCloseConfirmation = true;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đóng quán sớm sẽ bỏ lỡ doanh thu. Nhấn Space để xác nhận hoặc Esc để hủy.");
            return true;
        }

        public void CompleteDayBySleeping()
        {
            if (!CanSleep) return;
            ShowDaySummary(false);
        }

        public void StartNextDayFromSummary()
        {
            ResolveReferences();
            if (GameManager.HasInstance && GameManager.Instance.IsPaused)
            {
                GameManager.Instance.ResumeGame();
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.ConsumeCalendarDayOffset();
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.AdvanceDay();
            }

            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                if (lateReturnPenalty)
                {
                    stats.ModifyFatigue(-Constants.PLAYER_FATIGUE_MAX * 0.5f);
                    stats.ModifyStress(-Constants.PLAYER_STRESS_MAX * 0.5f);
                }
                else
                {
                    stats.ModifyFatigue(-Constants.PLAYER_FATIGUE_MAX);
                    stats.ModifyStress(-Constants.PLAYER_STRESS_MAX);
                }
                stats.RefillIce();
            }

            StartFreshDay(lateReturnPenalty);
            lateReturnPenalty = false;

            SaveManager saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SaveGame();
            }
        }

        public void RestoreState(float hour, BusinessDayPhase phase, bool savedLatePenalty)
        {
            ResolveReferences();
            initialized = true;
            lateReturnPenalty = savedLatePenalty;
            if (dayNightCycle != null)
            {
                dayNightCycle.SkipToHour(hour);
                dayNightCycle.SetRunning(phase != BusinessDayPhase.Preparation && phase != BusinessDayPhase.DaySummary);
                previousHour = dayNightCycle.CurrentHour;
            }
            SetPhase(phase);
        }

        private void StartFreshDay(bool startLate)
        {
            initialized = true;
            awaitingOpenConfirmation = false;
            awaitingEarlyCloseConfirmation = false;
            closingNoticeShown = false;
            ResolveReferences();

            float startHour = startLate ? lateWakeHour : wakeHour;
            if (dayNightCycle != null)
            {
                dayNightCycle.SkipToHour(startHour);
                dayNightCycle.SetRunning(true);
                previousHour = startHour;
            }

            SetPhase(BusinessDayPhase.PreOpen);
            TabMenuUI.MarkMenuNeedsDailySave();
            TeleportPlayerHome();
            EventManager.TriggerDialogueLine("Hoàng Hôn", startLate
                ? "Dậy muộn vì về nhà sau 00:00. Hôm nay phải chuẩn bị nhanh hơn."
                : "06:00 sáng. Hãy kiểm tra nguyên liệu và đến quán trước giờ mở cửa.");
        }

        private void BeginPreparation()
        {
            if (currentPhase != BusinessDayPhase.PreOpen) return;
            if (dayNightCycle != null)
            {
                dayNightCycle.SkipToHour(preparationHour);
                dayNightCycle.SetRunning(false);
                previousHour = preparationHour;
            }
            SetPhase(BusinessDayPhase.Preparation);
            TabMenuUI.MarkMenuNeedsDailySave();
            EventManager.TriggerDialogueLine("Hoàng Hôn", "08:00. Thời gian đã tạm dừng để chuẩn bị. Mở Tab, chọn tối đa 3 món và lưu thực đơn.");
        }

        private void HandleConfirmationInput()
        {
            if (Keyboard.current == null || TabMenuUI.IsAnyMenuOpen) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (awaitingOpenConfirmation || awaitingEarlyCloseConfirmation)
                {
                    awaitingOpenConfirmation = false;
                    awaitingEarlyCloseConfirmation = false;
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã hủy xác nhận.");
                }
                return;
            }

            if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;
            if (teaCart != null && teaCart.IsPlayerInteracting) return;

            if (currentPhase == BusinessDayPhase.Preparation)
            {
                if (!TabMenuUI.HasSavedServingMenu || TabMenuUI.GetActiveServingOrderIds().Count == 0)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy mở Tab, chọn ít nhất 1 món và nhấn Lưu trước khi mở cửa.");
                    return;
                }

                if (!awaitingOpenConfirmation)
                {
                    awaitingOpenConfirmation = true;
                    EventManager.TriggerDialogueLine("Hoàng Hôn", $"Mở cửa với {TabMenuUI.GetActiveServingOrderIds().Count} món? Nhấn Space lần nữa để xác nhận hoặc Esc để hủy.");
                    return;
                }

                OpenShop();
                return;
            }

            if (currentPhase == BusinessDayPhase.Trading && awaitingEarlyCloseConfirmation)
            {
                CloseShop(true);
            }
        }

        private void OpenShop()
        {
            awaitingOpenConfirmation = false;
            SetPhase(BusinessDayPhase.Trading);
            if (dayNightCycle != null) dayNightCycle.SetRunning(true);
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Quán đã mở cửa. Khách sẽ gọi các món trong thực đơn đã lưu.");
        }

        private void CloseShop(bool early)
        {
            awaitingEarlyCloseConfirmation = false;
            SetPhase(BusinessDayPhase.Closing);
            EventManager.TriggerDialogueLine("Hoàng Hôn", early
                ? "Đã đóng quán sớm. Hãy hoàn tất các đơn còn lại rồi trở về nhà."
                : "Quán đã đóng cửa. Không nhận thêm khách mới; hãy hoàn tất các đơn còn lại.");

            if (GetActiveCustomerCount() == 0)
            {
                EnterAfterHours();
            }
        }

        private void EnterAfterHours()
        {
            if (currentPhase == BusinessDayPhase.AfterHours) return;
            SetPhase(BusinessDayPhase.AfterHours);
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã phục vụ xong khách cuối. Hãy trở về nhà và tương tác với giường để kết thúc ngày.");
        }

        private void HandleMidnightTimeout()
        {
            lateReturnPenalty = true;
            if (npcSpawner != null)
            {
                npcSpawner.ClearAllCustomers();
            }
            TeleportPlayerHome();

            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats != null)
            {
                stats.ModifyFatigue(20f);
            }
            ShowDaySummary(true);
        }

        private void ShowDaySummary(bool automaticReturn)
        {
            if (currentPhase == BusinessDayPhase.DaySummary) return;
            if (dayNightCycle != null) dayNightCycle.SetRunning(false);
            SetPhase(BusinessDayPhase.DaySummary);

            if (automaticReturn)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã quá 00:00. Bạn được đưa về nhà và sẽ thức dậy muộn hơn vào ngày mai.");
            }

            if (GameManager.HasInstance && !GameManager.Instance.IsPaused)
            {
                GameManager.Instance.PauseGame();
            }

            DaySummaryUI summary = FindAnyObjectByType<DaySummaryUI>(FindObjectsInactive.Include);
            if (summary != null)
            {
                summary.Show();
            }
            else
            {
                StartNextDayFromSummary();
            }
        }

        private int GetActiveCustomerCount()
        {
            if (npcSpawner != null) return npcSpawner.ActiveCustomerCount;
            return FindObjectsByType<NPCController>(FindObjectsInactive.Exclude).Length;
        }

        private void TeleportPlayerHome()
        {
            ResolveReferences();
            if (player == null || sleepBed == null) return;

            Vector3 target = sleepBed.transform.position + sleepBed.transform.forward * wakeOffsetFromBed + Vector3.up * 0.2f;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = target;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            else
            {
                player.transform.position = target;
            }
            player.transform.rotation = Quaternion.LookRotation(-sleepBed.transform.forward, Vector3.up);
        }

        private void ResolveReferences()
        {
            if (dayNightCycle == null) dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            if (teaCart == null) teaCart = FindAnyObjectByType<TeaCart>();
            if (sleepBed == null) sleepBed = FindAnyObjectByType<SleepBed>();
            if (npcSpawner == null) npcSpawner = FindAnyObjectByType<NPCSpawner>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();
        }

        private void SetPhase(BusinessDayPhase phase)
        {
            if (currentPhase == phase) return;
            currentPhase = phase;
            EventManager.TriggerBusinessDayPhaseChanged(currentPhase);
        }
    }
}
