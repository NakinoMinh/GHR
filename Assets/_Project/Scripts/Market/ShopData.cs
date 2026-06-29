using System;
using System.Collections.Generic;
using UnityEngine;

namespace GanhHangRong.Economy
{
    [Serializable]
    public class ShopStockItem
    {
        public ItemData item;

        [Tooltip("Để -1 nếu dùng giá mặc định trong ItemData.")]
        public int priceOverride = -1;

        [Tooltip("Để -1 nếu quầy bán không giới hạn.")]
        public int stockAmount = -1;

        public int GetPrice()
        {
            if (priceOverride >= 0)
            {
                return priceOverride;
            }

            return item != null ? item.BuyPrice : 0;
        }
    }

    [CreateAssetMenu(fileName = "NewShopData", menuName = "Gánh Hàng Rong/Economy/Shop Data")]
    public class ShopData : ScriptableObject
    {
        [Header("Thông tin quầy")]
        public string shopId;
        public string shopName;
        public Sprite shopIcon;

        [Header("Hàng bán")]
        public List<ShopStockItem> itemsForSale = new List<ShopStockItem>();

        public string Id => string.IsNullOrWhiteSpace(shopId) ? name : shopId;
        public string DisplayName => string.IsNullOrWhiteSpace(shopName) ? name : shopName;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(shopId))
            {
                shopId = name;
            }
        }
    }
}
