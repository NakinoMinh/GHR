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

        // Progress
        public int currentDay;
        
        // Story flags có thể được thêm ở đây, vd: Dictionary<string, bool>
    }
}
