using System.Collections.Generic;
using GanhHangRong.Core;
using UnityEngine;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Ghi nhận số liệu trong ngày bằng event, tách khỏi UI và thao tác pha chế.
    /// </summary>
    [DisallowMultipleComponent]
    public class DailyBusinessLedger : MonoBehaviour
    {
        public static DailyBusinessLedger Instance { get; private set; }

        [SerializeField] private int revenue;
        [SerializeField] private int expenses;
        [SerializeField] private int deliveryFees;
        [SerializeField] private int happyCustomers;
        [SerializeField] private int lostCustomers;

        private readonly Dictionary<int, int> soldByOrderId = new Dictionary<int, int>();

        public int Revenue => revenue;
        public int Expenses => expenses;
        public int DeliveryFees => deliveryFees;
        public int Profit => revenue - expenses;
        public int HappyCustomers => happyCustomers;
        public int LostCustomers => lostCustomers;
        public float Rating => CalculateRating();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventManager.OnMoneyEarned += RecordRevenue;
            EventManager.OnMoneySpent += RecordExpense;
            EventManager.OnDeliveryFeePaid += RecordDeliveryFee;
            EventManager.OnSaleCompleted += RecordSale;
            EventManager.OnCustomerLeftHappy += RecordHappyCustomer;
            EventManager.OnCustomerLeftSad += RecordLostCustomer;
            EventManager.OnNewDay += ResetForNewDay;
        }

        private void OnDisable()
        {
            EventManager.OnMoneyEarned -= RecordRevenue;
            EventManager.OnMoneySpent -= RecordExpense;
            EventManager.OnDeliveryFeePaid -= RecordDeliveryFee;
            EventManager.OnSaleCompleted -= RecordSale;
            EventManager.OnCustomerLeftHappy -= RecordHappyCustomer;
            EventManager.OnCustomerLeftSad -= RecordLostCustomer;
            EventManager.OnNewDay -= ResetForNewDay;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetForNewDay()
        {
            revenue = 0;
            expenses = 0;
            deliveryFees = 0;
            happyCustomers = 0;
            lostCustomers = 0;
            soldByOrderId.Clear();
        }

        public string GetBestSellingDishName()
        {
            if (soldByOrderId.Count == 0) return "Chưa có";

            int bestId = -1;
            int bestCount = -1;
            foreach (KeyValuePair<int, int> sale in soldByOrderId)
            {
                if (sale.Value > bestCount)
                {
                    bestId = sale.Key;
                    bestCount = sale.Value;
                }
            }
            return ChapterOrderCatalog.GetOrderName(bestId);
        }

        public int GetSoldCount(int orderId)
        {
            return soldByOrderId.TryGetValue(orderId, out int count) ? count : 0;
        }

        private void RecordRevenue(int amount)
        {
            revenue += Mathf.Max(0, amount);
        }

        private void RecordExpense(int amount)
        {
            expenses += Mathf.Max(0, amount);
        }

        private void RecordDeliveryFee(int amount)
        {
            deliveryFees += Mathf.Max(0, amount);
        }

        private void RecordSale(int orderId, int amount)
        {
            soldByOrderId.TryGetValue(orderId, out int count);
            soldByOrderId[orderId] = count + 1;
        }

        private void RecordHappyCustomer(NPCType type)
        {
            happyCustomers++;
        }

        private void RecordLostCustomer(NPCType type)
        {
            lostCustomers++;
        }

        private float CalculateRating()
        {
            int total = happyCustomers + lostCustomers;
            if (total == 0) return 5f;
            return Mathf.Clamp((happyCustomers * 5f + lostCustomers) / total, 1f, 5f);
        }
    }
}
