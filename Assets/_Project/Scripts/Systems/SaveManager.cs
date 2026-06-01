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
            
            // Lấy data từ GameManager
            data.currentDay = GameManager.Instance.CurrentDay;

            // Lấy data từ PlayerStats
            var playerStats = FindAnyObjectByType<Player.PlayerStats>();
            if (playerStats != null)
            {
                data.money = playerStats.Money;
                data.fatigue = playerStats.Fatigue;
                data.teaSupply = playerStats.TeaSupply;
                data.sugarSupply = playerStats.SugarSupply;
                data.cupSupply = playerStats.CupSupply;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveManager] Đã lưu game tại: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Lỗi khi lưu: {e.Message}");
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[SaveManager] Không tìm thấy file save.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data.version != Constants.SAVE_VERSION)
                {
                    Debug.LogWarning("[SaveManager] Phiên bản save không khớp!");
                    // Trong thực tế, cần migration logic ở đây
                }

                // Cập nhật GameManager (sẽ bị ghi đè nếu load scene sau, nên thực tế có thể cần truyền qua scene bằng DTO)
                // Hoặc load xong apply sau khi scene init
                ApplySaveData(data);
                
                Debug.Log("[SaveManager] Đã tải game thành công.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Lỗi khi tải: {e.Message}");
                return false;
            }
        }

        private void ApplySaveData(SaveData data)
        {
            // Note: Trong thiết kế thực tế, ApplySaveData nên được gọi khi Scene Chapter1 đã load xong.
            var playerStats = FindAnyObjectByType<Player.PlayerStats>();
            if (playerStats != null)
            {
                // Reflection hoặc methods công khai (cần bổ sung method Set ở PlayerStats nếu ko muốn reflection)
                // Để đơn giản prototype, ta có thể bỏ qua nếu player chưa init
            }
        }
    }
}
