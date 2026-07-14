using UnityEngine;
using System.IO;
using GanhHangRong.Core;

namespace GanhHangRong.Systems
{
    public class SaveManager : Singleton<SaveManager>
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, Constants.SAVE_FILE_NAME);

        public void SaveGame()
        {
            var data = new SaveData();

            if (GameManager.HasInstance)
            {
                data.currentDay = GameManager.Instance.CurrentDay;
                data.currentChapter = GameManager.Instance.CurrentChapter;
                data.chapter1Completed = GameManager.Instance.Chapter1Completed;
            }

            var businessDay = FindAnyObjectByType<BusinessDayController>();
            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            if (businessDay != null)
            {
                data.businessDayPhase = (int)businessDay.CurrentPhase;
                data.lateReturnPenalty = businessDay.HasLateReturnPenalty;
            }
            if (dayNight != null)
            {
                data.currentHour = dayNight.CurrentHour;
            }
            data.servingMenuSaved = UI.TabMenuUI.HasSavedServingMenu;
            data.activeServingOrderIds = UI.TabMenuUI.ExportActiveServingOrderIds();

            var playerStats = FindAnyObjectByType<Player.PlayerStats>();
            if (playerStats != null)
            {
                data.money = playerStats.Money;
                data.fatigue = playerStats.Fatigue;
                data.teaSupply = playerStats.TeaSupply;
                data.sugarSupply = playerStats.SugarSupply;
                data.coffeeSupply = playerStats.CoffeeSupply;
                data.cupSupply = playerStats.CupSupply;
                data.totalCustomersServed = playerStats.TotalCustomersServed;
                data.totalMoneyEarned = playerStats.TotalMoneyEarned;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveManager] Saved game at: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[SaveManager] Save file not found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data.version < 2)
                {
                    data.currentHour = 6f;
                    data.businessDayPhase = (int)BusinessDayPhase.PreOpen;
                    data.servingMenuSaved = false;
                    data.activeServingOrderIds = new[] { 0, 1 };
                }

                if (data.version != Constants.SAVE_VERSION)
                    Debug.LogWarning("[SaveManager] Save version mismatch.");

                ApplySaveData(data);
                Debug.Log("[SaveManager] Loaded game successfully.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return false;
            }
        }

        private void ApplySaveData(SaveData data)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.RestoreProgress(data.currentDay, data.currentChapter, data.chapter1Completed);

            var playerStats = FindAnyObjectByType<Player.PlayerStats>();
            if (playerStats != null)
            {
                playerStats.RestoreFromSave(
                    data.money,
                    data.fatigue,
                    data.teaSupply,
                    data.sugarSupply,
                    data.coffeeSupply,
                    data.cupSupply,
                    data.totalCustomersServed,
                    data.totalMoneyEarned);
            }

            UI.TabMenuUI.RestoreActiveServingOrderIds(data.activeServingOrderIds, data.servingMenuSaved);

            var businessDay = FindAnyObjectByType<BusinessDayController>();
            if (businessDay != null && businessDay.IsManagingGameLoop)
            {
                BusinessDayPhase phase = System.Enum.IsDefined(typeof(BusinessDayPhase), data.businessDayPhase)
                    ? (BusinessDayPhase)data.businessDayPhase
                    : BusinessDayPhase.PreOpen;
                businessDay.RestoreState(data.currentHour, phase, data.lateReturnPenalty);
            }
        }
    }
}
