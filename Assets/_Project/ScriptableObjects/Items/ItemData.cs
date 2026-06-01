using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Economy
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Gánh Hàng Rong/Economy/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public string description;
        public int buyPrice;
        public int sellPrice;
        
        // Bỏ qua Icon/Sprite hiện tại vì chưa có assets
    }
}
