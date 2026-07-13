using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Player;

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
            SyncSuppliesAndFurniture(item, amount);
        }

        private void SyncSuppliesAndFurniture(ItemData item, int amount)
        {
            if (item == null) return;
            string id = item.Id.ToLowerInvariant();
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();

            if (id == "hu_ca_phe" || id == "ca_phe")
            {
                if (stats != null) stats.AddCoffee(150 * amount);
            }
            else if (id == "tra" || id == "hu_tra")
            {
                if (stats != null) stats.AddSupplies(100 * amount, 0, 0);
            }
            else if (id == "nuoc_sach" || id == "nuoc" || id == "binh_nuoc")
            {
                Interaction.CartItem.AddBottleWater(30f * amount);
            }
            else if (id == "duong" || id == "hu_duong")
            {
                if (stats != null) stats.AddSupplies(0, 200 * amount, 0);
            }
            else if (id == "ly_nuoc_sach" || id == "ly_nuoc" || id == "ly_cups")
            {
                if (stats != null) stats.AddSupplies(0, 0, 10 * amount);
            }
            else if (id == "ban_doi" || id == "ban_bon" || id == "ghe_nhua" || id.StartsWith("ban_") || id.StartsWith("ghe_"))
            {
                if (Interaction.FurniturePlacementManager.Instance != null)
                {
                    Interaction.FurniturePlacementManager.Instance.EnterPlacementMode(item.Id);
                }
            }
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

        public bool RemoveItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;
            InventoryItemStack stack = FindStack(itemId);
            if (stack == null || stack.amount < amount) return false;
            stack.amount -= amount;
            if (stack.amount <= 0) items.Remove(stack);
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

        public void TriggerMoneyChangedEvent(int newMoney)
        {
            currentMoney = newMoney;
            MoneyChanged?.Invoke(currentMoney);
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

            // Đồng bộ sang PlayerStats
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                stats.SyncMoneyFromInventory(currentMoney);
            }

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

            // Đồng bộ sang PlayerStats
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                stats.SyncMoneyFromInventory(currentMoney);
            }
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
                PlayerStats stats = FindAnyObjectByType<PlayerStats>();
                if (stats != null)
                {
                    currentMoney = stats.Money;
                }
                MoneyChanged?.Invoke(currentMoney);
                EventManager.TriggerMoneyChanged(currentMoney);
                return;
            }

            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(PlayerPrefs.GetString(SaveKey));
            if (saveData == null)
            {
                Debug.LogWarning("Không đọc được save túi đồ. Dùng dữ liệu mặc định.", this);
                currentMoney = startingMoney;
                PlayerStats stats = FindAnyObjectByType<PlayerStats>();
                if (stats != null)
                {
                    currentMoney = stats.Money;
                }
                return;
            }

            Dictionary<string, ItemData> knownItems = BuildKnownItemLookup();
            items.Clear();
            currentMoney = Mathf.Max(0, saveData.money);

            PlayerStats activeStats = FindAnyObjectByType<PlayerStats>();
            if (activeStats != null)
            {
                currentMoney = activeStats.Money;
            }

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
