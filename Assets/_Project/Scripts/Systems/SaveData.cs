using System;

namespace GanhHangRong.Systems
{
    [Serializable]
    public class SaveData
    {
        public int version = Core.Constants.SAVE_VERSION;
        
        // Player Stats
        public int money;
        public float fatigue;
        public int teaSupply;
        public int sugarSupply;
        public int coffeeSupply;
        public int cupSupply;
        public int totalCustomersServed;
        public int totalMoneyEarned;

        // Progress
        public int currentDay;
        public int currentChapter = 1;
        public bool chapter1Completed;

        // Vòng ngày kinh doanh
        public float currentHour = 6f;
        public int businessDayPhase;
        public bool lateReturnPenalty;
        public bool servingMenuSaved;
        public int[] activeServingOrderIds;
        
        // Story flags có thể được thêm ở đây, vd: Dictionary<string, bool>
    }
}
