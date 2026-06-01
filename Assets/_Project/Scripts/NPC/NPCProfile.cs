using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.NPC
{
    [CreateAssetMenu(fileName = "NewNPCProfile", menuName = "Gánh Hàng Rong/NPC/Profile")]
    public class NPCProfile : ScriptableObject
    {
        public NPCType npcType;
        public string npcName; // Có thể để trống để sinh ngẫu nhiên
        
        [Header("Behavior")]
        [Tooltip("Thời gian chờ tối thiểu (giây)")]
        public float minPatience = Constants.NPC_PATIENCE_MIN;
        [Tooltip("Thời gian chờ tối đa (giây)")]
        public float maxPatience = Constants.NPC_PATIENCE_MAX;
        
        [Tooltip("Xác suất sẽ để lại tiền tip (0-1)")]
        [Range(0f, 1f)]
        public float tipChance = Constants.NPC_TIP_CHANCE;
        
        [Tooltip("Thời gian ngồi uống trà (giây)")]
        public float minDrinkTime = Constants.NPC_DRINK_TIME_MIN;
        public float maxDrinkTime = Constants.NPC_DRINK_TIME_MAX;

        [Header("Appearance")]
        public Color[] possibleColorTints = new Color[] { Color.white };
        // Tham chiếu Sprite hoặc Mesh ở đây nếu có (hiện tại bỏ qua vì dùng primitive)
    }
}
