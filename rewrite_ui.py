import os

file_path = r'd:\G\GHR\Assets\Editor\SceneBuilder\Chapter1SceneBuilder.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

start_idx = -1
end_idx = -1
for i, line in enumerate(lines):
    if '// 8. HỆ THỐNG UI' in line:
        start_idx = i - 1
    if 'promptUI.GetType().GetField("canvasGroup"' in line:
        end_idx = i + 1
        break

new_code = """            // ==========================================
            // 8. HỆ THỐNG UI (REDESIGNED)
            // ==========================================
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            GameObject hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform, false);
            var hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero; hudRect.offsetMax = Vector2.zero;
            hudObj.AddComponent<CanvasGroup>();
            var gameplayHUD = hudObj.AddComponent<GameplayHUD>(); 
            
            // Tải Sprites
            Sprite sprAvatarBoard = LoadSprite("Assets/_Project/Art/UI/avatar_board_1779824082283.png");
            Sprite sprPriceTag = LoadSprite("Assets/_Project/Art/UI/price_tag_1779824336421.png");
            Sprite sprCompass = LoadSprite("Assets/_Project/Art/UI/compass_clock_1779824348382.png");
            Sprite sprCounter = LoadSprite("Assets/_Project/Art/UI/counter_table_1779824397782.png");
            Sprite sprTea = LoadSprite("Assets/_Project/Art/UI/tea_jar_1779824460314.png");
            Sprite sprSugar = LoadSprite("Assets/_Project/Art/UI/sugar_jar_1779824538254.png");

            // 1. Top Left - Avatar & Stats
            GameObject topLeftPanel = new GameObject("TopLeftPanel");
            topLeftPanel.transform.SetParent(hudObj.transform, false);
            var tlRect = topLeftPanel.AddComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(0, 1); tlRect.anchorMax = new Vector2(0, 1);
            tlRect.pivot = new Vector2(0, 1);
            tlRect.anchoredPosition = new Vector2(50, -50);
            tlRect.sizeDelta = new Vector2(600, 200);
            
            if (sprAvatarBoard != null) {
                var tlImg = topLeftPanel.AddComponent<UnityEngine.UI.Image>();
                tlImg.sprite = sprAvatarBoard;
                tlImg.preserveAspect = true;
            }

            var nameText = new GameObject("NameText").AddComponent<TMPro.TextMeshProUGUI>();
            nameText.transform.SetParent(topLeftPanel.transform, false);
            nameText.rectTransform.anchoredPosition = new Vector2(40, 20);
            nameText.rectTransform.sizeDelta = new Vector2(300, 50);
            nameText.text = "NGUYỄN HOÀNG HÔN";
            nameText.fontSize = 28; nameText.alignment = TMPro.TextAlignmentOptions.Center;
            nameText.fontStyle = TMPro.FontStyles.Bold;
            nameText.color = new Color(0.9f, 0.8f, 0.7f); // Màu gỗ/kem

            // Năng lượng
            GameObject energyBg = new GameObject("EnergyBg");
            energyBg.transform.SetParent(topLeftPanel.transform, false);
            var ebgRect = energyBg.AddComponent<RectTransform>();
            ebgRect.anchoredPosition = new Vector2(40, -30); ebgRect.sizeDelta = new Vector2(250, 30);
            energyBg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject energyFill = new GameObject("EnergyFill");
            energyFill.transform.SetParent(energyBg.transform, false);
            var efillRect = energyFill.AddComponent<RectTransform>();
            efillRect.anchorMin = new Vector2(0, 0); efillRect.anchorMax = new Vector2(1, 1);
            efillRect.offsetMin = Vector2.zero; efillRect.offsetMax = Vector2.zero;
            var energySliderImg = energyFill.AddComponent<UnityEngine.UI.Image>();
            energySliderImg.color = new Color(1f, 0.6f, 0f); // Cam vàng

            var energySlider = energyBg.AddComponent<UnityEngine.UI.Slider>();
            energySlider.fillRect = efillRect; energySlider.minValue = 0; energySlider.maxValue = 1; energySlider.value = 1;
            energySlider.interactable = false;

            // Stress
            GameObject stressBg = new GameObject("StressBg");
            stressBg.transform.SetParent(topLeftPanel.transform, false);
            var sbgRect = stressBg.AddComponent<RectTransform>();
            sbgRect.anchoredPosition = new Vector2(40, -65); sbgRect.sizeDelta = new Vector2(250, 10);
            stressBg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject stressFill = new GameObject("StressFill");
            stressFill.transform.SetParent(stressBg.transform, false);
            var sfillRect = stressFill.AddComponent<RectTransform>();
            sfillRect.anchorMin = new Vector2(0, 0); sfillRect.anchorMax = new Vector2(1, 1);
            sfillRect.offsetMin = Vector2.zero; sfillRect.offsetMax = Vector2.zero;
            var stressSliderImg = stressFill.AddComponent<UnityEngine.UI.Image>();
            stressSliderImg.color = new Color(0.8f, 0.1f, 0.1f); // Đỏ

            var stressSlider = stressBg.AddComponent<UnityEngine.UI.Slider>();
            stressSlider.fillRect = sfillRect; stressSlider.minValue = 0; stressSlider.maxValue = 1; stressSlider.value = 0;
            stressSlider.interactable = false;

            // Avatar viền tròn
            GameObject avatarBox = new GameObject("AvatarBox");
            avatarBox.transform.SetParent(topLeftPanel.transform, false);
            var avBoxRect = avatarBox.AddComponent<RectTransform>();
            avBoxRect.anchoredPosition = new Vector2(-150, 0); avBoxRect.sizeDelta = new Vector2(120, 120);
            avatarBox.AddComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0);

            // 2. Top Right - Tiền
            GameObject topRightPanel = new GameObject("MoneyPanel");
            topRightPanel.transform.SetParent(hudObj.transform, false);
            var trRect = topRightPanel.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(1, 1); trRect.anchorMax = new Vector2(1, 1);
            trRect.pivot = new Vector2(1, 1);
            trRect.anchoredPosition = new Vector2(-250, -50);
            trRect.sizeDelta = new Vector2(400, 120);
            
            if (sprPriceTag != null) {
                var trImg = topRightPanel.AddComponent<UnityEngine.UI.Image>();
                trImg.sprite = sprPriceTag;
                trImg.preserveAspect = true;
            }

            var moneyText = new GameObject("MoneyText").AddComponent<TMPro.TextMeshProUGUI>();
            moneyText.transform.SetParent(topRightPanel.transform, false);
            moneyText.rectTransform.anchoredPosition = new Vector2(30, 0);
            moneyText.rectTransform.sizeDelta = new Vector2(280, 80);
            moneyText.text = "50,000 VNĐ";
            moneyText.fontSize = 42; moneyText.alignment = TMPro.TextAlignmentOptions.Center;
            moneyText.color = new Color(0.3f, 0.2f, 0.1f);
            moneyText.fontStyle = TMPro.FontStyles.Bold;

            // 3. Top Right - Đồng hồ La Bàn
            GameObject clockPanel = new GameObject("ClockPanel");
            clockPanel.transform.SetParent(hudObj.transform, false);
            var clRect = clockPanel.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(1, 1); clRect.anchorMax = new Vector2(1, 1);
            clRect.pivot = new Vector2(1, 1);
            clRect.anchoredPosition = new Vector2(-50, -50);
            clRect.sizeDelta = new Vector2(150, 150);
            
            if (sprCompass != null) {
                var clImg = clockPanel.AddComponent<UnityEngine.UI.Image>();
                clImg.sprite = sprCompass;
                clImg.preserveAspect = true;
            }

            var clockText = new GameObject("ClockText").AddComponent<TMPro.TextMeshProUGUI>();
            clockText.transform.SetParent(clockPanel.transform, false);
            clockText.rectTransform.anchoredPosition = new Vector2(0, 0);
            clockText.rectTransform.sizeDelta = new Vector2(150, 40);
            clockText.text = "17:00";
            clockText.fontSize = 28; clockText.alignment = TMPro.TextAlignmentOptions.Center;
            clockText.color = Color.white;
            clockText.fontStyle = TMPro.FontStyles.Bold;
            var clockOutline = clockText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            clockOutline.effectColor = Color.black; clockOutline.effectDistance = new Vector2(2, -2);

            // 4. Bottom Center - Mặt Bàn Pha Chế
            GameObject invPanel = new GameObject("InventoryPanel");
            invPanel.transform.SetParent(hudObj.transform, false);
            var invRect = invPanel.AddComponent<RectTransform>();
            invRect.anchorMin = new Vector2(0.5f, 0); invRect.anchorMax = new Vector2(0.5f, 0);
            invRect.pivot = new Vector2(0.5f, 0);
            invRect.anchoredPosition = new Vector2(0, 0);
            invRect.sizeDelta = new Vector2(1200, 200);
            
            if (sprCounter != null) {
                var invImg = invPanel.AddComponent<UnityEngine.UI.Image>();
                invImg.sprite = sprCounter;
                invImg.type = UnityEngine.UI.Image.Type.Sliced; // Nếu nó bị kéo giãn, có thể chỉnh lại type
            } else {
                invPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            
            var invTitle = new GameObject("Title").AddComponent<TMPro.TextMeshProUGUI>();
            invTitle.transform.SetParent(invPanel.transform, false);
            invTitle.rectTransform.anchoredPosition = new Vector2(-400, 80);
            invTitle.rectTransform.sizeDelta = new Vector2(300, 40);
            invTitle.text = "Mặt Bàn Pha Chế";
            invTitle.fontSize = 24; invTitle.alignment = TMPro.TextAlignmentOptions.Center;
            invTitle.color = new Color(1f, 0.9f, 0.8f);
            invTitle.fontStyle = TMPro.FontStyles.Bold;

            var hlgObj = new GameObject("SlotContainer");
            hlgObj.transform.SetParent(invPanel.transform, false);
            var hlgRect = hlgObj.AddComponent<RectTransform>();
            hlgRect.anchorMin = Vector2.zero; hlgRect.anchorMax = Vector2.one;
            hlgRect.offsetMin = new Vector2(50, 10); hlgRect.offsetMax = new Vector2(-50, -30);

            var hlg = hlgObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 30;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            TMPro.TextMeshProUGUI[] invTexts = new TMPro.TextMeshProUGUI[4];
            string[] invNames = { "TRÀ:\n10", "ĐƯỜNG:\n10", "LY:\n20", "ĐÁ:\n100%" };
            Sprite[] invSprites = { sprTea, sprSugar, null, null };

            for(int i=0; i<4; i++) {
                GameObject slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(hlgObj.transform, false);
                var le = slot.AddComponent<UnityEngine.UI.LayoutElement>();
                le.minWidth = 180; le.minHeight = 120;
                
                // Icon
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                var iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0); iconRect.anchorMax = new Vector2(0, 1);
                iconRect.pivot = new Vector2(0, 0.5f);
                iconRect.anchoredPosition = new Vector2(0, 0);
                iconRect.sizeDelta = new Vector2(100, 100);
                
                var iconImg = iconObj.AddComponent<UnityEngine.UI.Image>();
                if (invSprites[i] != null) {
                    iconImg.sprite = invSprites[i];
                    iconImg.preserveAspect = true;
                } else {
                    if (i == 2) iconImg.color = new Color(1f, 0.5f, 0.5f); // Cup
                    if (i == 3) iconImg.color = new Color(0.5f, 0.8f, 1f, 0.8f); // Ice
                }
                
                // Text
                var text = new GameObject("Text").AddComponent<TMPro.TextMeshProUGUI>();
                text.transform.SetParent(slot.transform, false);
                text.rectTransform.anchorMin = new Vector2(1, 0); text.rectTransform.anchorMax = new Vector2(1, 1);
                text.rectTransform.pivot = new Vector2(1, 0.5f);
                text.rectTransform.anchoredPosition = new Vector2(0, 0);
                text.rectTransform.sizeDelta = new Vector2(80, 100);
                
                text.text = invNames[i]; 
                text.fontSize = 24; 
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = new Color(0.2f, 0.2f, 0.2f);
                text.fontStyle = TMPro.FontStyles.Bold;
                invTexts[i] = text;
            }

            // Gán reference cho GameplayHUD qua Reflection
            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            gameplayHUD.GetType().GetField("playerNameText", bf).SetValue(gameplayHUD, nameText);
            gameplayHUD.GetType().GetField("energySlider", bf).SetValue(gameplayHUD, energySlider);
            gameplayHUD.GetType().GetField("stressSlider", bf).SetValue(gameplayHUD, stressSlider);
            gameplayHUD.GetType().GetField("moneyText", bf).SetValue(gameplayHUD, moneyText);
            gameplayHUD.GetType().GetField("clockText", bf).SetValue(gameplayHUD, clockText);
            gameplayHUD.GetType().GetField("teaCountText", bf).SetValue(gameplayHUD, invTexts[0]);
            gameplayHUD.GetType().GetField("sugarCountText", bf).SetValue(gameplayHUD, invTexts[1]);
            gameplayHUD.GetType().GetField("cupCountText", bf).SetValue(gameplayHUD, invTexts[2]);
            gameplayHUD.GetType().GetField("iceLevelText", bf).SetValue(gameplayHUD, invTexts[3]);

            GameObject promptObj = new GameObject("InteractionPrompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, 150); // Nâng lên khỏi xe đẩy
            pRect.sizeDelta = new Vector2(160, 60);
            
            // Viền đỏ cho nút E
            promptObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);
            var pOutline = promptObj.AddComponent<UnityEngine.UI.Outline>();
            pOutline.effectColor = Color.red; pOutline.effectDistance = new Vector2(3, 3);

            promptObj.AddComponent<CanvasGroup>();
            var promptUI = promptObj.AddComponent<InteractionPromptUI>();
            var promptText = promptObj.AddComponent<TMPro.TextMeshProUGUI>();
            promptText.rectTransform.SetParent(promptObj.transform, false);
            promptText.rectTransform.anchorMin = Vector2.zero; promptText.rectTransform.anchorMax = Vector2.one;
            promptText.rectTransform.sizeDelta = Vector2.zero;
            promptText.text = "Nhấn E";
            promptText.fontSize = 28; promptText.color = Color.white;
            promptText.alignment = TMPro.TextAlignmentOptions.Center;
            promptUI.GetType().GetField("promptText", bf).SetValue(promptUI, promptText);
            promptUI.GetType().GetField("canvasGroup", bf).SetValue(promptUI, promptObj.GetComponent<CanvasGroup>());\n"""

if start_idx != -1 and end_idx != -1:
    new_lines = lines[:start_idx] + [new_code] + lines[end_idx:]
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Replaced UI successfully.")
else:
    print("Could not find start or end index.")
