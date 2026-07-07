using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GanhHangRong.UI
{
    public class RecipeMiniGameUI : MonoBehaviour
    {
        private const bool ShowRecipePanel = false;

        private static RecipeMiniGameUI instance;
        public static RecipeMiniGameUI Instance
        {
            get
            {
                if (!ShowRecipePanel)
                {
                    return null;
                }

                if (instance == null)
                {
                    GameObject go = new GameObject("RecipeMiniGameUI");
                    instance = go.AddComponent<RecipeMiniGameUI>();
                }
                return instance;
            }
        }

        private GameObject uiContainer;
        private Text titleText;
        private Text stepsText;
        
        private List<string> currentRecipeSteps = new List<string>();
        private int currentStepIndex = 0;
        private bool isActive = false;

        private void Awake()
        {
            if (!ShowRecipePanel)
            {
                Destroy(gameObject);
                return;
            }

            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();

            Core.EventManager.OnCustomerOrderPlaced += HandleOrderPlaced;
            Core.EventManager.OnCustomerOrderCleared += HandleOrderCleared;
        }

        private void OnDestroy()
        {
            Core.EventManager.OnCustomerOrderPlaced -= HandleOrderPlaced;
            Core.EventManager.OnCustomerOrderCleared -= HandleOrderCleared;
        }

        private void HandleOrderPlaced(int orderId, string drinkName)
        {
            // Nếu là Trà Đá (id 0)
            if (orderId == 0)
            {
                StartRecipe(drinkName, new List<string> { "Ly Trà", "Trà", "Nước Sôi", "Đá", "Đường" });
            }
            // Mở rộng cho Cà Phê Đá (id 1) nếu cần
            else if (orderId == 1)
            {
                StartRecipe(drinkName, new List<string> { "Ly Trà", "Cà Phê", "Nước Sôi", "Đá", "Đường" });
            }
        }

        private void HandleOrderCleared()
        {
            StopRecipe();
        }

        private void CreateUI()
        {
            // Canvas
            uiContainer = new GameObject("RecipeCanvas");
            uiContainer.transform.SetParent(transform);
            Canvas canvas = uiContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Below main HUD just in case, or above
            uiContainer.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Xóa GraphicRaycaster để không chặn chuột 3D của người chơi!
            
            // Panel Background (bên trái màn hình)
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(uiContainer.transform, false);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.4f);
            panelRect.anchorMax = new Vector2(0.25f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            titleText = title.AddComponent<Text>();
            titleText.text = "Công Thức Món";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.yellow;
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Steps Text
            GameObject steps = new GameObject("StepsText");
            steps.transform.SetParent(panel.transform, false);
            stepsText = steps.AddComponent<Text>();
            stepsText.text = "";
            stepsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stepsText.fontSize = 20;
            stepsText.alignment = TextAnchor.UpperLeft;
            stepsText.color = Color.white;
            stepsText.lineSpacing = 1.2f;
            RectTransform sRect = steps.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.05f);
            sRect.anchorMax = new Vector2(0.95f, 0.8f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            uiContainer.SetActive(false);
        }

        public void StartRecipe(string recipeName, List<string> steps)
        {
            titleText.text = "Pha: " + recipeName;
            currentRecipeSteps = steps;
            currentStepIndex = 0;
            isActive = true;
            UpdateStepsUI();
            uiContainer.SetActive(true);
        }

        public void StopRecipe()
        {
            isActive = false;
            uiContainer.SetActive(false);
        }

        public void OnIngredientAdded(string ingredient)
        {
            if (!isActive) return;

            if (currentStepIndex < currentRecipeSteps.Count)
            {
                // Kiểm tra xem món vừa bỏ vào có ĐÚNG là món tiếp theo không
                if (currentRecipeSteps[currentStepIndex] == ingredient)
                {
                    currentStepIndex++;
                    UpdateStepsUI();
                    
                    if (currentStepIndex >= currentRecipeSteps.Count)
                    {
                        // Xong công thức
                        Core.EventManager.TriggerDialogueLine("Hoàng Hôn", "Pha chế hoàn tất! Trông có vẻ hoàn hảo.");
                        // Không tắt UI ngay để người chơi thấy dấu tick cuối cùng
                    }
                }
                else
                {
                    // SAI thứ tự
                    Interaction.CartItem.HasRuinedDrink = true;
                    UpdateStepsUI(true); // show red cross
                    Core.EventManager.TriggerDialogueLine("Hoàng Hôn", $"Pha sai công thức rồi! (Vừa cho {ingredient} thay vì {currentRecipeSteps[currentStepIndex]}). Ly nước này đã hỏng, hãy đem ra bồn rửa.");
                }
            }
        }

        public void UndoStep()
        {
            if (!isActive) return;
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateStepsUI();
            }
        }

        private void UpdateStepsUI(bool isRuined = false)
        {
            string display = "";
            for (int i = 0; i < currentRecipeSteps.Count; i++)
            {
                if (i < currentStepIndex)
                {
                    display += $"<color=green>✔️ {currentRecipeSteps[i]}</color>\n";
                }
                else if (i == currentStepIndex)
                {
                    if (isRuined)
                        display += $"<color=red>❌ {currentRecipeSteps[i]}</color>\n";
                    else
                        display += $"<color=yellow>▶ {currentRecipeSteps[i]}</color>\n";
                }
                else
                {
                    display += $"<color=white>   {currentRecipeSteps[i]}</color>\n";
                }
            }
            stepsText.text = display;
        }
    }
}
