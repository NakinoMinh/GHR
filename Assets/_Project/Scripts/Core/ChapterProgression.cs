using GanhHangRong.Player;
using UnityEngine;

namespace GanhHangRong.Core
{
    public static class ChapterProgression
    {
        public static bool IsChapter1Complete(PlayerStats stats)
        {
            GameManager manager = GetGameManager();
            if (manager == null || stats == null) return false;

            return manager.CurrentDay >= Constants.CHAPTER1_REQUIRED_DAYS
                && stats.TotalCustomersServed >= Constants.CHAPTER1_TARGET_CUSTOMERS
                && stats.Money >= Constants.CHAPTER1_TARGET_MONEY;
        }

        public static string GetChapter1ProgressText(PlayerStats stats)
        {
            GameManager manager = GetGameManager();
            if (manager == null || stats == null)
                return "Tien do Chuong 1: chua co du lieu.";

            int day = ClampToTarget(manager.CurrentDay, Constants.CHAPTER1_REQUIRED_DAYS);
            int customers = ClampToTarget(stats.TotalCustomersServed, Constants.CHAPTER1_TARGET_CUSTOMERS);
            int money = ClampToTarget(stats.Money, Constants.CHAPTER1_TARGET_MONEY);

            if (IsChapter1Complete(stats))
                return "Da du von va khach quen. San sang sang Chuong 2: Cho Dem Ven Bien.";

            return $"Tien do Chuong 1: Ngay {day}/{Constants.CHAPTER1_REQUIRED_DAYS} | Khach quen {customers}/{Constants.CHAPTER1_TARGET_CUSTOMERS} | Von {money:N0}/{Constants.CHAPTER1_TARGET_MONEY:N0} VND";
        }

        private static int ClampToTarget(int value, int target)
        {
            if (value < 0) return 0;
            return value > target ? target : value;
        }

        private static GameManager GetGameManager()
        {
            return GameManager.HasInstance ? GameManager.Instance : Object.FindAnyObjectByType<GameManager>();
        }
    }
}
