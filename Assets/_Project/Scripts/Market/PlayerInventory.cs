using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Economy
{
    [Serializable]
    public class InventoryItemStack
    {
        public ItemData item;
        public string itemId;
        [Min(0)] public int amount;

        public string Id => item != null ? item.Id : itemId;
    }

    [Serializable]
    internal class InventorySaveData
    {
        public int money;
        public List<InventorySaveEntry> items = new List<InventorySaveEntry>();
    }

    [Serializable]
    internal class InventorySaveEntry
    {
        public string itemId;
        public int amount;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        private const string SaveKey = "GHR_PlayerInventory";

        [Header("Tiền")]
        [SerializeField] private int startingMoney = Constants.STARTING_MONEY;
        [SerializeField] private int currentMoney = Constants.STARTING_MONEY;

        [Header("Item đã biết để load lại reference sau khi mở game")]
        [SerializeField] private List<ItemData> knownItemsForLoad = new List<ItemData>();

        [Header("Túi đồ")]
        [SerializeField] private List<InventoryItemStack> items = new List<InventoryItemStack>();

        public event Action<int> MoneyChanged;
        public event Action InventoryChanged;

        public int CurrentMoney => currentMoney;
        public IReadOnlyList<InventoryItemStack> Items => items;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Đã có PlayerInventory khác trong scene. Object này sẽ tự hủy.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentMoney = Mathf.Max(0, currentMoney);
            LoadData();
        }

        public void AddItem(ItemData item, int amount)
        {
            if (item == null)
            {
                Debug.LogWarning("Không thể AddItem vì ItemData bị null.", this);
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Không thể AddItem với số lượng <= 0.", this);
                return;
            }

            InventoryItemStack stack = FindStack(item);
            if (stack == null)
            {
                stack = new InventoryItemStack
                {
                    item = item,
                    itemId = item.Id,
                    amount = 0
                };
                items.Add(stack);
            }

            stack.item = item;
            stack.itemId = item.Id;
            stack.amount += amount;
            InventoryChanged?.Invoke();
        }

        public bool RemoveItem(ItemData item, int amount)
        {
            if (item == null)
            {
                Debug.LogWarning("Không thể RemoveItem vì ItemData bị null.", this);
                return false;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Không thể RemoveItem với số lượng <= 0.", this);
                return false;
            }

            InventoryItemStack stack = FindStack(item);
            if (stack == null || stack.amount < amount)
            {
                return false;
            }

            stack.amount -= amount;
            if (stack.amount <= 0)
            {
                items.Remove(stack);
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public int GetItemAmount(ItemData item)
        {
            if (item == null)
            {
                return 0;
            }

            InventoryItemStack stack = FindStack(item);
            return stack != null ? stack.amount : 0;
        }

        public int GetItemAmount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            InventoryItemStack stack = FindStack(itemId);
            return stack != null ? stack.amount : 0;
        }

        public bool HasItem(ItemData item, int amount = 1)
        {
            return GetItemAmount(item) >= Mathf.Max(1, amount);
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return GetItemAmount(itemId) >= Mathf.Max(1, amount);
        }

        public bool SpendMoney(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (currentMoney < amount)
            {
                return false;
            }

            currentMoney -= amount;
            MoneyChanged?.Invoke(currentMoney);
            EventManager.TriggerMoneySpent(amount);
            EventManager.TriggerMoneyChanged(currentMoney);
            return true;
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentMoney += amount;
            MoneyChanged?.Invoke(currentMoney);
            EventManager.TriggerMoneyEarned(amount);
            EventManager.TriggerMoneyChanged(currentMoney);
        }

        public void SaveData()
        {
            InventorySaveData saveData = new InventorySaveData
            {
                money = currentMoney
            };

            foreach (InventoryItemStack stack in items)
            {
                if (stack == null || stack.amount <= 0 || string.IsNullOrWhiteSpace(stack.Id))
                {
                    continue;
                }

                saveData.items.Add(new InventorySaveEntry
                {
                    itemId = stack.Id,
                    amount = stack.amount
                });
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        public void LoadData()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                currentMoney = startingMoney;
                MoneyChanged?.Invoke(currentMoney);
                EventManager.TriggerMoneyChanged(currentMoney);
                return;
            }

            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(PlayerPrefs.GetString(SaveKey));
            if (saveData == null)
            {
                Debug.LogWarning("Không đọc được save túi đồ. Dùng dữ liệu mặc định.", this);
                currentMoney = startingMoney;
                return;
            }

            Dictionary<string, ItemData> knownItems = BuildKnownItemLookup();
            items.Clear();
            currentMoney = Mathf.Max(0, saveData.money);

            foreach (InventorySaveEntry entry in saveData.items)
            {
                if (entry == null || entry.amount <= 0 || string.IsNullOrWhiteSpace(entry.itemId))
                {
                    continue;
                }

                knownItems.TryGetValue(entry.itemId, out ItemData item);
                items.Add(new InventoryItemStack
                {
                    item = item,
                    itemId = entry.itemId,
                    amount = entry.amount
                });
            }

            MoneyChanged?.Invoke(currentMoney);
            InventoryChanged?.Invoke();
            EventManager.TriggerMoneyChanged(currentMoney);
        }

        public string GetDebugInventoryText()
        {
            StringBuilder builder = new StringBuilder();
            foreach (InventoryItemStack stack in items)
            {
                if (stack == null || stack.amount <= 0)
                {
                    continue;
                }

                builder.AppendLine($"{stack.Id}: {stack.amount}");
            }

            return builder.ToString();
        }

        private InventoryItemStack FindStack(ItemData item)
        {
            return item == null ? null : FindStack(item.Id);
        }

        private InventoryItemStack FindStack(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return items.Find(stack => stack != null && stack.Id == itemId);
        }

        private Dictionary<string, ItemData> BuildKnownItemLookup()
        {
            Dictionary<string, ItemData> lookup = new Dictionary<string, ItemData>();

            AddKnownItemsToLookup(knownItemsForLoad, lookup);
            foreach (InventoryItemStack stack in items)
            {
                if (stack != null && stack.item != null && !lookup.ContainsKey(stack.item.Id))
                {
                    lookup.Add(stack.item.Id, stack.item);
                }
            }

            return lookup;
        }

        private static void AddKnownItemsToLookup(List<ItemData> source, Dictionary<string, ItemData> lookup)
        {
            if (source == null)
            {
                return;
            }

            foreach (ItemData item in source)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id) || lookup.ContainsKey(item.Id))
                {
                    continue;
                }

                lookup.Add(item.Id, item);
            }
        }
    }
}
