using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Economy
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        // Chứa các helper phương thức nếu cần mua sỉ nguyên liệu
        // Hiện tại PlayerStats đã xử lý phần lớn logic cộng/trừ tiền.
        
        public bool TryBuySupplies(Player.PlayerStats stats, int teaCount, int sugarCount, int cupCount, bool refillIce)
        {
            if (stats == null) return false;

            int totalCost = (teaCount * Constants.TEA_BUY_PRICE) + 
                            (sugarCount * Constants.SUGAR_BUY_PRICE) + 
                            (cupCount * Constants.CUP_BUY_PRICE);
                            
            if (refillIce)
            {
                totalCost += Constants.ICE_BUY_PRICE;
            }

            if (stats.SpendMoney(totalCost))
            {
                stats.AddSupplies(teaCount, sugarCount, cupCount);
                if (refillIce) stats.RefillIce();
                return true;
            }
            
            return false;
        }
    }
}
