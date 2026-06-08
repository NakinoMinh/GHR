using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Player
{
    /// <summary>
    /// Thống kê nhân vật — tiền, mệt mỏi, stress, mức đá, kho đồ.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Thống Kê")]
        [SerializeField] private int money = Constants.STARTING_MONEY;
        [SerializeField] private float fatigue = 0f;
        [SerializeField] private float stress = 0f;
        [SerializeField] private float iceLevel = Constants.ICE_MAX;

        [Header("Kho Đồ")]
        [SerializeField] private int teaSupply = 1000;
        [SerializeField] private int sugarSupply = 2000;
        [SerializeField] private int coffeeSupply = 1500;
        [SerializeField] private int cupSupply = 20;

        // Properties
        public int Money => money;
        public float Fatigue => fatigue;
        public float Stress => stress;
        public float IceLevel => iceLevel;
        public int TeaSupply => teaSupply;
        public int SugarSupply => sugarSupply;
        public int CoffeeSupply => coffeeSupply;
        public int CupSupply => cupSupply;

        // Tracking cho hệ thống cảm xúc
        private int customersServedToday = 0;
        private int moneyEarnedToday = 0;
        [SerializeField] private int totalCustomersServed = 0;
        [SerializeField] private int totalMoneyEarned = 0;
        public int CustomersServedToday => customersServedToday;
        public int MoneyEarnedToday => moneyEarnedToday;
        public int TotalCustomersServed => totalCustomersServed;
        public int TotalMoneyEarned => totalMoneyEarned;

        private void OnEnable()
        {
            EventManager.OnNewDay += ResetDailyStats;
            EventManager.OnCustomerLeftSad += OnCustomerSad;
        }

        private void OnDisable()
        {
            EventManager.OnNewDay -= ResetDailyStats;
            EventManager.OnCustomerLeftSad -= OnCustomerSad;
        }

        private void Update()
        {
            if (!GameManager.HasInstance || !GameManager.Instance.IsPlaying) return;

            var controller = GetComponent<PlayerController>();
            if (controller != null && controller.CurrentState == PlayerState.Sitting)
            {
                // Hồi phục khi ngồi nghỉ
                ModifyFatigue(-Constants.PLAYER_FATIGUE_REST_RATE * Time.deltaTime / 60f);
                ModifyStress(-Constants.PLAYER_STRESS_REST_RATE * Time.deltaTime / 60f);
            }
            else
            {
                // Mệt mỏi tăng dần
                ModifyFatigue(Constants.PLAYER_FATIGUE_RATE * Time.deltaTime / 60f);
            }

            // Đá tan dần
            float meltRate = Constants.ICE_MELT_RATE;
            ModifyIceLevel(-meltRate * Time.deltaTime / 60f);
        }

        // ═══════════════════════════════════════════
        // TIỀN
        // ═══════════════════════════════════════════
        public bool CanAfford(int amount) => money >= amount;

        public void AddMoney(int amount)
        {
            money += amount;
            moneyEarnedToday += amount;
            totalMoneyEarned += amount;
            EventManager.TriggerMoneyChanged(money);
            EventManager.TriggerMoneyEarned(amount);
        }

        public bool SpendMoney(int amount)
        {
            if (!CanAfford(amount)) return false;
            money -= amount;
            EventManager.TriggerMoneyChanged(money);
            EventManager.TriggerMoneySpent(amount);
            return true;
        }

        // ═══════════════════════════════════════════
        // MỆT MỎI
        // ═══════════════════════════════════════════
        public void ModifyFatigue(float amount)
        {
            float prev = fatigue;
            fatigue = Mathf.Clamp(fatigue + amount, 0f, Constants.PLAYER_FATIGUE_MAX);
            if (!Mathf.Approximately(prev, fatigue))
                EventManager.TriggerFatigueChanged(fatigue);
        }

        // ═══════════════════════════════════════════
        // STRESS
        // ═══════════════════════════════════════════
        public void ModifyStress(float amount)
        {
            float prev = stress;
            stress = Mathf.Clamp(stress + amount, 0f, Constants.PLAYER_STRESS_MAX);
            if (!Mathf.Approximately(prev, stress))
                EventManager.TriggerStressChanged(stress);
        }

        private void OnCustomerSad(NPCType type)
        {
            ModifyStress(Constants.PLAYER_STRESS_RATE_SAD_CUSTOMER);
        }

        // ═══════════════════════════════════════════
        // ĐÁ
        // ═══════════════════════════════════════════
        public void ModifyIceLevel(float amount)
        {
            float prev = iceLevel;
            iceLevel = Mathf.Clamp(iceLevel + amount, 0f, Constants.ICE_MAX);
            if (!Mathf.Approximately(prev, iceLevel))
                EventManager.TriggerIceLevelChanged(iceLevel);
        }

        public void RefillIce()
        {
            iceLevel = Constants.ICE_MAX;
            EventManager.TriggerIceLevelChanged(iceLevel);
        }

        // ═══════════════════════════════════════════
        // KHO ĐỒ
        // ═══════════════════════════════════════════
        public bool HasSuppliesForTea()
        {
            return Interaction.CartItem.HasPreparedTea;
        }

        public void UseTeaSupplies()
        {
            Interaction.CartItem.HasPreparedTea = false;
        }

        public void TakeOneCup()
        {
            cupSupply = Mathf.Max(0, cupSupply - 1);
        }

        public void ConsumeTea(int amount)
        {
            teaSupply = Mathf.Max(0, teaSupply - amount);
        }

        public void AddSupplies(int tea, int sugar, int cups)
        {
            teaSupply = Mathf.Min(1000, teaSupply + tea);
            sugarSupply = Mathf.Min(2000, sugarSupply + sugar);
            cupSupply += cups;
        }

        public void AddCoffee(int coffee)
        {
            coffeeSupply = Mathf.Min(1500, coffeeSupply + coffee);
        }

        public void RecordCustomerServed()
        {
            customersServedToday++;
            totalCustomersServed++;
            ModifyStress(Constants.PLAYER_STRESS_RATE_SERVE); // Giảm stress khi phục vụ thành công
        }

        public void RestoreFromSave(int savedMoney, float savedFatigue, int savedTea, int savedSugar, int savedCoffee, int savedCups, int savedTotalCustomers, int savedTotalMoneyEarned)
        {
            money = Mathf.Max(0, savedMoney);
            fatigue = Mathf.Clamp(savedFatigue, 0f, Constants.PLAYER_FATIGUE_MAX);
            teaSupply = Mathf.Clamp(savedTea, 0, 1000);
            sugarSupply = Mathf.Clamp(savedSugar, 0, 2000);
            coffeeSupply = Mathf.Clamp(savedCoffee, 0, 1500);
            cupSupply = Mathf.Max(0, savedCups);
            totalCustomersServed = Mathf.Max(0, savedTotalCustomers);
            totalMoneyEarned = Mathf.Max(0, savedTotalMoneyEarned);

            EventManager.TriggerMoneyChanged(money);
            EventManager.TriggerFatigueChanged(fatigue);
            EventManager.TriggerIceLevelChanged(iceLevel);
        }

        private void ResetDailyStats()
        {
            customersServedToday = 0;
            moneyEarnedToday = 0;
        }
    }
}
