using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Vật phẩm trên mặt bàn xe đẩy — có thể tương tác khi đang ở chế độ góc nhìn xe đẩy.
    /// Gắn script này lên các FBX models: ấm đun nước, bình trà, gánh nước mía.
    /// </summary>
    public class CartItem : MonoBehaviour
    {
        public enum CartItemType
        {
            WaterKettle,    // Ấm đun nước (amdunnuoc.fbx)
            TeaTin,         // Bình trà (Meshy_AI_Red_Vintage_Tea_Tin)
            SugarcaneJuice, // Gánh nước mía (Meshy_AI_Vietnam_Sugarcane_Jui)
            SugarJar,       // Hũ đường (Meshy_AI_Cracked_Vintage_Jar_w)
            GasStove,       // Bếp ga (Meshy_AI_Namilux_High_Power_Po)
            Coffee,         // Cà phê (Meshy_AI_Cà_Phê_Phổ_Cổ_V)
            IceCooler,      // Bình đựng đá (Meshy_AI_Open_Red_Cooler_with)
            WaterBottle,    // Bình nước (Meshy_AI_Sài_Gòn_Aquwa_Bottl)
            WaterCup        // Ly nước (Meshy_AI_Steaming_Glass_Beer_M)
        }

        [Header("Cấu Hình Vật Phẩm")]
        [SerializeField] private CartItemType itemType;
        [SerializeField] private string itemName = "Vật phẩm";
        [SerializeField] private string itemDescription = "Mô tả vật phẩm";

        [Header("Hiệu Ứng")]
        [SerializeField] private float hoverScaleMultiplier = 1.15f;
        [SerializeField] private float hoverBobSpeed = 2f;
        [SerializeField] private float hoverBobAmount = 0.05f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

        [Header("Ly Trà Đá — Mô Hình Cầm Tay")]
        [Tooltip("Prefab mô hình ly trà đá (WaterCup FBX). Nếu để trống sẽ dùng primitive Cylinder thay thế.")]
        [SerializeField] private GameObject teaCupHeldPrefab;
        [Tooltip("Vị trí của ly khi cầm trên tay (Local Position)")]
        [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.015f, 0.045f, 0.015f);
        [Tooltip("Góc xoay của ly khi cầm trên tay (Local Rotation)")]
        [SerializeField] private Vector3 heldLocalRotation = new Vector3(0f, 0f, 90f);

        // State
        private bool isHighlighted = false;
        private bool isInteracting = false;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Renderer[] renderers;
        private Color[] originalColors;
        private Material[] originalMaterials;
        private float hoverTimer = 0f;
        private Coroutine resourceFeedbackRoutine;

        
        private static bool isBoilingWater = false;
        public static bool IsBoilingWater => isBoilingWater;
        private static bool isWaterBoiled = false;
        public static bool IsWaterBoiled => isWaterBoiled;

        public static bool HasRuinedDrink { get; set; } = false;

        private static float bottleWater = 30f;
        public static float BottleWater => bottleWater;

        private static float kettleWater = 1.2f;
        public static float KettleWater => kettleWater;

        private const float maxKettleWater = 1.2f;
        private const float minKettleWaterToRefill = 0.2f;
        private const float boilDurationSeconds = 10f;
        private const float boiledWaterCoolDownGameMinutes = 120f;

        private static bool isHoldingCup = false;
        public static bool IsHoldingCup => isHoldingCup;
        public static bool IsHoldingDirtyCup { get; private set; } = false;
        public static bool IsCupClean => isHoldingCup && !IsHoldingDirtyCup && teaInCup == 0 && coffeeInCup == 0 && waterInCup <= 0.001f && iceInCup <= 0.001f && !hasPreparedTea && !HasRuinedDrink;
        public static bool IsCupDirty => (isHoldingCup && !IsCupClean) || hasPreparedTea;
        public static bool HasCupToWash => IsCupDirty;

        private static int teaInCup = 0;
        public static int TeaInCup => teaInCup;

        private static int coffeeInCup = 0;
        public static int CoffeeInCup => coffeeInCup;

        private static float waterInCup = 0f;
        public static float WaterInCup => waterInCup;

        private static float iceInCup = 0f;
        public static float IceInCup => iceInCup;

        private static bool hasPreparedTea = false;
        private static int preparedDrinkId = -1;
        public static int PreparedDrinkId => preparedDrinkId;
        public static string PreparedDrinkName => GetDrinkName(preparedDrinkId);
        public static bool HasPreparedTea
        {
            get => hasPreparedTea;
            set
            {
                hasPreparedTea = value;
                if (!value) preparedDrinkId = -1;
            }
        }

        private static Coroutine activeCoolDownCoroutine = null;
        private static CartItem activeInstance = null;

        // Mô hình ly trà đá đang cầm trên tay nhân vật
        private static GameObject heldTeaCupObj = null;
        private static int returnedCleanCupVisualIndex = 0;

        public static void ResetBrewingState()
        {
            isBoilingWater = false;
            isWaterBoiled = false;
            bottleWater = 30f;
            kettleWater = maxKettleWater;
            ResetCupState();
            activeCoolDownCoroutine = null;
        }

        public static void ResetCupState()
        {
            isHoldingCup = false;
            IsHoldingDirtyCup = false;
            teaInCup = 0;
            coffeeInCup = 0;
            waterInCup = 0f;
            iceInCup = 0f;
            hasPreparedTea = false;
            HasRuinedDrink = false;
            preparedDrinkId = -1;
            DetachTeaCup();
        }

        public static void PrepareReadyOrder(int orderId)
        {
            ResetCupState();
            hasPreparedTea = true;
            preparedDrinkId = orderId;
        }

        public static void PickUpDirtyCupFromTable(Player.PlayerController player)
        {
            if (isHoldingCup || hasPreparedTea)
            {
                return;
            }

            isHoldingCup = true;
            IsHoldingDirtyCup = true;
            teaInCup = 0;
            coffeeInCup = 0;
            waterInCup = 0f;
            iceInCup = 0f;

            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã dọn ly dơ trên bàn. Hãy mang đến bồn rửa ly để rửa sạch tái sử dụng!");
            Debug.Log("[CartItem] Dọn ly trên bàn -> Cầm ly dơ đi rửa");

            AttachEmptyCupToPlayer(player);
        }

        public static void ConsumeWater(float amount)
        {
            kettleWater = Mathf.Max(0f, kettleWater - amount);
            if (kettleWater <= 0.01f)
            {
                isWaterBoiled = false;
                if (activeInstance != null && activeCoolDownCoroutine != null)
                {
                    activeInstance.StopCoroutine(activeCoolDownCoroutine);
                    activeCoolDownCoroutine = null;
                }
            }
        }

        public CartItemType ItemType => itemType;
        public string ItemName => itemType == CartItemType.WaterCup ? "Ly sạch" : itemName;
        public string ItemDescription => itemDescription;
        public bool IsHighlighted => isHighlighted;

        private void ShowResourceDelta(string text)
        {
            if (resourceFeedbackRoutine != null)
            {
                StopCoroutine(resourceFeedbackRoutine);
            }

            resourceFeedbackRoutine = StartCoroutine(ResourceFeedbackRoutine(text));
        }

        private System.Collections.IEnumerator ResourceFeedbackRoutine(string text)
        {
            EventManager.TriggerInteractionPromptShow(text);
            yield return new WaitForSeconds(1.0f);

            if (isHighlighted)
            {
                EventManager.TriggerInteractionPromptShow(itemName);
            }

            resourceFeedbackRoutine = null;
        }

private void Awake()
        {
            if (activeInstance == null)
            {
                ResetBrewingState();
            }

            if (itemType == CartItemType.WaterKettle)
            {
                activeInstance = this;
            }
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
            renderers = GetComponentsInChildren<Renderer>();

            originalColors = new Color[renderers.Length];
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                {
                    originalMaterials[i] = renderers[i].material;
                    originalColors[i] = renderers[i].material.color;
                }
            }

            EnsureInteractionCollider();
        }

private void EnsureInteractionCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 paddedSize = bounds.size + new Vector3(0.08f, 0.08f, 0.08f);
            box.center = transform.InverseTransformPoint(bounds.center + Vector3.up * 0.02f);
            Vector3 localSize = transform.InverseTransformVector(paddedSize);
            box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }


        private void Update()
        {
            // Đã tắt hiệu ứng nhấp nhô (bobbing) và phóng to (scaling) khi hover theo yêu cầu để vật phẩm đứng yên cố định
        }

        /// <summary>
        /// Bật highlight khi chuột trỏ vào.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (highlighted)
                    {
                        // Thêm emission để vật phẩm sáng lên
                        renderers[i].material.EnableKeyword("_EMISSION");
                        renderers[i].material.SetColor("_EmissionColor", highlightColor * 0.3f);
                    }
                    else
                    {
                        renderers[i].material.DisableKeyword("_EMISSION");
                        renderers[i].material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý khi người chơi click vào vật phẩm.
        /// </summary>
        public void OnItemClicked(Player.PlayerController player)
        {
            if (isInteracting) return;

            switch (itemType)
            {
                case CartItemType.WaterKettle:
                    OnWaterKettleInteract(player);
                    break;
                case CartItemType.TeaTin:
                    OnTeaTinInteract(player);
                    break;
                case CartItemType.SugarcaneJuice:
                    OnSugarcaneJuiceInteract(player);
                    break;
                case CartItemType.SugarJar:
                    OnSugarJarInteract(player);
                    break;
                case CartItemType.GasStove:
                    OnGasStoveInteract(player);
                    break;
                case CartItemType.Coffee:
                    OnCoffeeInteract(player);
                    break;
                case CartItemType.IceCooler:
                    OnIceCoolerInteract(player);
                    break;
                case CartItemType.WaterBottle:
                    OnWaterBottleInteract(player);
                    break;
                case CartItemType.WaterCup:
                    OnWaterCupInteract(player);
                    break;
            }

            // Hiệu ứng click — rung nhẹ
            StartCoroutine(ClickFeedback());
        }

        private void OnWaterKettleInteract(Player.PlayerController player)
        {
            if (isHoldingCup)
            {
                if (IsHoldingDirtyCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này khách vừa uống xong còn dơ, hãy mang đến bồn rửa trước khi rót nước!");
                    return;
                }

                if (waterInCup >= 0.2f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly nước đã có đủ nước sôi rồi!");
                    return;
                }

                // Nếu nước chưa sôi hoặc đã nguội → tự động đun ngay
                if (!isWaterBoiled)
                {
                    if (isBoilingWater)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đang được đun, hãy đợi một chút rồi nhấp lại vào ấm để rót nước sôi!");
                        return;
                    }

                    // Tìm các đối tượng cần thiết trong cảnh để đun
                    GameObject kettleObj = GameObject.Find("KettleProp");
                    GameObject stoveObj = GameObject.Find("GasStoveProp");
                    GameObject waterObj = GameObject.Find("WaterBottleProp");

                    if (kettleObj == null || stoveObj == null || waterObj == null)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Thiếu dụng cụ đun nước! Cần có Ấm Nước, Bếp Ga và Bình Nước.");
                        return;
                    }

                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đã nguội! Đun lại ngay để pha trà...");
                    StartCoroutine(BoilWaterRoutine(kettleObj, stoveObj, waterObj));
                    return;
                }

                if (kettleWater < 0.2f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ấm hết nước rồi! Đang lấy thêm nước từ bình và đun sôi...");
                    GameObject kettleObj2 = GameObject.Find("KettleProp");
                    GameObject stoveObj2 = GameObject.Find("GasStoveProp");
                    GameObject waterObj2 = GameObject.Find("WaterBottleProp");
                    if (kettleObj2 != null && stoveObj2 != null && waterObj2 != null)
                        StartCoroutine(BoilWaterRoutine(kettleObj2, stoveObj2, waterObj2));
                    return;
                }

                // Rót 0.2L nước sôi vào ly
                ConsumeWater(0.2f);
                waterInCup += 0.2f;
                ShowResourceDelta($"-200ml nước ấm (ấm còn {kettleWater:F1}L)");
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã rót 200ml nước sôi vào ly. (-200ml nước ấm, còn {kettleWater:F1}L trong ấm)");

                if (UI.RecipeMiniGameUI.Instance != null) UI.RecipeMiniGameUI.Instance.OnIngredientAdded("Nước Sôi");
                CheckBrewingCompletion(player);
                return;
            }

            if (isWaterBoiled)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Ấm nước nóng đã sôi 100 độ C, sẵn sàng pha trà ngon rồi! (Nước trong ấm: {kettleWater:F1}L)");
                return;
            }

            if (isBoilingWater)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đang được đun trên bếp ga, hãy đợi một chút!");
                return;
            }

            // Tìm các đối tượng cần thiết trong cảnh (không cầm ly)
            GameObject kettleProp = GameObject.Find("KettleProp");
            GameObject stoveProp = GameObject.Find("GasStoveProp");
            GameObject waterProp = GameObject.Find("WaterBottleProp");

            if (kettleProp == null || stoveProp == null || waterProp == null)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Thiếu dụng cụ đun nước! Cần có Ấm Nước, Bếp Ga và Bình Nước.");
                return;
            }

            StartCoroutine(BoilWaterRoutine(kettleProp, stoveProp, waterProp));
            Debug.Log("[CartItem] Tương tác ấm đun nước (bình thủy) -> Bắt đầu đun nước");
        }

        private void OnTeaTinInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (!isHoldingCup)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hãy lấy 1 chiếc ly sạch trước khi lấy trà!");
                return;
            }

            if (IsHoldingDirtyCup)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này khách vừa uống xong còn dơ, hãy mang đến bồn rửa trước khi cho trà vào!");
                return;
            }

            if (coffeeInCup > 0)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này đang pha cà phê rồi, không trộn thêm trà vào được.");
                return;
            }

            if (stats.TeaSupply < 50)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ trà khô trong hộp trà (cần ít nhất 50g)!");
                return;
            }

            // Lấy 50g trà bỏ vào ly
            stats.ConsumeTea(50);
            teaInCup += 50;
            ShowResourceDelta($"-50g trà (còn {stats.TeaSupply}g)");
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã cho 50g trà vào ly. (-50g trà, còn {stats.TeaSupply}g)");

            if (UI.RecipeMiniGameUI.Instance != null) UI.RecipeMiniGameUI.Instance.OnIngredientAdded("Trà");
            CheckBrewingCompletion(player);
        }

        private void OnSugarcaneJuiceInteract(Player.PlayerController player)
        {
            // Gánh nước mía — lấy đường
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats != null)
            {
                stats.AddSupplies(0, 200, 0); // Thêm 200g đường
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy đường từ gánh nước mía. (+200g đường, hiện có {stats.SugarSupply}g)");
            }
            Debug.Log("[CartItem] Tương tác gánh nước mía");
        }

        private void OnSugarJarInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (isHoldingCup)
            {
                if (IsHoldingDirtyCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này khách vừa uống xong còn dơ, hãy mang đến bồn rửa trước khi cho đường vào!");
                    return;
                }
                if (stats.SugarSupply < 10)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ đường trong hũ (cần ít nhất 10g)!");
                    return;
                }
                stats.AddSupplies(0, -10, 0); // Consume 10g sugar
                ShowResourceDelta($"-10g đường (còn {stats.SugarSupply}g)");
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã cho 10g đường vào ly. (-10g đường, còn {stats.SugarSupply}g)");
                
                if (UI.RecipeMiniGameUI.Instance != null) UI.RecipeMiniGameUI.Instance.OnIngredientAdded("Đường");
                CheckBrewingCompletion(player);
                return;
            }

            // Hũ đường — lấy đường
            stats.AddSupplies(0, 200, 0); // Thêm 200g đường
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy đường từ hũ đường. (+200g đường, hiện có {stats.SugarSupply}g)");
            Debug.Log("[CartItem] Tương tác hũ đường");
        }

        private void OnGasStoveInteract(Player.PlayerController player)
        {
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Bếp ga Namilux đang hoạt động tốt để đun nước nóng.");
            Debug.Log("[CartItem] Tương tác bếp ga");
        }

        private void OnCoffeeInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (isHoldingCup)
            {
                if (IsHoldingDirtyCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này khách vừa uống xong còn dơ, hãy mang đến bồn rửa trước khi cho cà phê vào!");
                    return;
                }

                if (teaInCup > 0)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này đang pha trà rồi, không trộn thêm cà phê vào được.");
                    return;
                }

                if (coffeeInCup >= 30)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly đã có đủ 30g cà phê rồi.");
                    return;
                }

                if (stats.CoffeeSupply < 30)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ bột cà phê trong hũ (cần ít nhất 30g)!");
                    return;
                }

                stats.ConsumeCoffee(30);
                coffeeInCup += 30;
                ShowResourceDelta($"-30g cà phê (còn {stats.CoffeeSupply}g)");
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã cho 30g cà phê vào ly. (-30g cà phê, còn {stats.CoffeeSupply}g)");
                CheckBrewingCompletion(player);
                return;
            }
            if (stats != null)
            {
                stats.AddCoffee(150); // Thêm 150g cà phê
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Lấy thêm bột cà phê. (+150g cà phê, hiện có {stats.CoffeeSupply}g)");
            }
            Debug.Log("[CartItem] Tương tác hũ cà phê");
        }

        private void OnIceCoolerInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (isHoldingCup)
            {
                if (IsHoldingDirtyCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly này khách vừa uống xong còn dơ, hãy mang đến bồn rửa trước khi cho đá vào!");
                    return;
                }

                if (stats.IceLevel < 5f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Thùng đựng đá đã hết đá sạch! Hãy tiếp thêm đá.");
                    return;
                }

                if (iceInCup >= 5f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly nước đã có đủ đá lạnh rồi!");
                    return;
                }

                // Lấy 5% đá cho vào ly
                stats.ModifyIceLevel(-5f);
                iceInCup += 5f;
                ShowResourceDelta($"-5% đá (còn {Mathf.RoundToInt(stats.IceLevel)}%)");
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã thêm 5% đá vào ly. (-5% đá, còn {Mathf.RoundToInt(stats.IceLevel)}%)");

                if (UI.RecipeMiniGameUI.Instance != null) UI.RecipeMiniGameUI.Instance.OnIngredientAdded("Đá");
                CheckBrewingCompletion(player);
                return;
            }

            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Thùng đựng đá sạch. (Hiện còn: {Mathf.RoundToInt(stats.IceLevel)}% đá). Hãy lấy 1 chiếc ly sạch trước khi lấy đá!");
            Debug.Log("[CartItem] Tương tác bình đựng đá");
        }

        private void OnWaterBottleInteract(Player.PlayerController player)
        {
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đây là bình nước lọc Sài Gòn Aquwa. (Hiện còn: {bottleWater:F1}L). Hãy tương tác với Ấm Nước để lấy nước và đun sôi!");
            Debug.Log("[CartItem] Tương tác bình nước");
        }

        private void CheckBrewingCompletion(Player.PlayerController player)
        {
            bool teaReady = teaInCup >= 50 && coffeeInCup == 0;
            bool coffeeReady = coffeeInCup >= 30 && teaInCup == 0;
            if (isHoldingCup && (teaReady || coffeeReady) && waterInCup >= 0.2f && iceInCup >= 5f)
            {
                isHoldingCup = false;
                IsHoldingDirtyCup = false;
                hasPreparedTea = true;
                preparedDrinkId = coffeeReady ? 1 : 0;
                string drinkName = GetDrinkName(preparedDrinkId);
                teaInCup = 0;
                coffeeInCup = 0;
                waterInCup = 0f;
                iceInCup = 0f;

                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Hoàn thành 1 ly {drinkName}! Nhấn Space để phục vụ hoặc đi đến bàn khách để đặt ly xuống.");

                // Gắn mô hình ly trà đá lên tay phải nhân vật
                AttachTeaCupToPlayer(player);
            }
        }

        private static void AttachEmptyCupToPlayer(Player.PlayerController player)
        {
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
            }

            Transform attachPoint = FindRightHandBone(player.transform);
            
            CartItem[] items = Object.FindObjectsByType<CartItem>(FindObjectsSortMode.None);
            CartItem cupTemplate = null;
            foreach (var item in items)
            {
                if (item.itemType == CartItemType.WaterCup)
                {
                    cupTemplate = item;
                    break;
                }
            }

            GameObject cupGO;
            Vector3 targetWorldScale = Vector3.one * 0.12f;
            if (cupTemplate != null)
            {
                cupGO = Instantiate(cupTemplate.gameObject);
                Destroy(cupGO.GetComponent<CartItem>());
                foreach (var col in cupGO.GetComponentsInChildren<Collider>()) Destroy(col);
                targetWorldScale = cupTemplate.transform.lossyScale;
            }
            else
            {
                cupGO = CreateFallbackEmptyCupModel();
            }

            if (attachPoint != null)
            {
                Vector3 targetPos = cupTemplate != null ? cupTemplate.heldLocalPosition : new Vector3(0.015f, 0.045f, 0.015f);
                Vector3 targetRot = cupTemplate != null ? cupTemplate.heldLocalRotation : new Vector3(0f, 0f, 90f);
                AttachCupToHand(cupGO, attachPoint, targetWorldScale, targetPos, targetRot);
            }
            else
            {
                cupGO.transform.SetParent(player.transform, false);
                cupGO.transform.localPosition = new Vector3(0.35f, 0.85f, 0.15f);
                cupGO.transform.localRotation = Quaternion.identity;
                cupGO.transform.localScale = targetWorldScale;
            }

            cupGO.name = "HeldEmptyCup";
            if (cupTemplate == null) ApplyHeldCupMaterials(cupGO, false);
            heldTeaCupObj = cupGO;
        }

        private void AttachTeaCupToPlayer(Player.PlayerController player)
        {
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
            }

            Transform attachPoint = FindRightHandBone(player.transform);

            CartItem[] items = Object.FindObjectsByType<CartItem>(FindObjectsSortMode.None);
            CartItem cupTemplate = null;
            foreach (var item in items)
            {
                if (item.itemType == CartItemType.WaterCup)
                {
                    cupTemplate = item;
                    break;
                }
            }

            GameObject cupGO;
            Vector3 targetWorldScale = Vector3.one * 0.12f;
            if (cupTemplate != null)
            {
                cupGO = Instantiate(cupTemplate.gameObject);
                Destroy(cupGO.GetComponent<CartItem>());
                foreach (var col in cupGO.GetComponentsInChildren<Collider>()) Destroy(col);
                targetWorldScale = cupTemplate.transform.lossyScale;
                
                // Add a simple tea liquid inside the cloned mesh
                GameObject teaLiquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                teaLiquid.transform.SetParent(cupGO.transform, false);
                // Adjust position/scale relative to the cup template's bounds
                var mf = cupTemplate.GetComponent<MeshFilter>();
                if(mf != null && mf.sharedMesh != null) {
                    float h = mf.sharedMesh.bounds.size.y;
                    teaLiquid.transform.localPosition = mf.sharedMesh.bounds.center + new Vector3(0, h * 0.1f, 0);
                    teaLiquid.transform.localScale = new Vector3(mf.sharedMesh.bounds.size.x * 0.85f, h * 0.4f, mf.sharedMesh.bounds.size.z * 0.85f);
                } else {
                    teaLiquid.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    teaLiquid.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
                }
                Object.Destroy(teaLiquid.GetComponent<Collider>());
                var rend = teaLiquid.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = CreateCupMaterial("FallbackTeaCup_Liquid", new Color(0.75f, 0.42f, 0.12f, 0.85f), true, 3001);
                    if (mat != null) rend.material = mat;
                }
            }
            else if (teaCupHeldPrefab != null)
            {
                cupGO = Instantiate(teaCupHeldPrefab);
            }
            else
            {
                cupGO = CreateFallbackTeaCupModel();
            }

            if (attachPoint != null)
            {
                Vector3 targetPos = cupTemplate != null ? cupTemplate.heldLocalPosition : new Vector3(0.015f, 0.045f, 0.015f);
                Vector3 targetRot = cupTemplate != null ? cupTemplate.heldLocalRotation : new Vector3(0f, 0f, 90f);
                AttachCupToHand(cupGO, attachPoint, targetWorldScale, targetPos, targetRot);
            }
            else
            {
                cupGO.transform.SetParent(player.transform, false);
                cupGO.transform.localPosition = new Vector3(0.35f, 0.85f, 0.15f);
                cupGO.transform.localRotation = Quaternion.identity;
                cupGO.transform.localScale = targetWorldScale;
            }

            cupGO.name = "HeldTeaCup";
            if (cupTemplate == null && teaCupHeldPrefab == null) ApplyHeldCupMaterials(cupGO, true);
            heldTeaCupObj = cupGO;
        }

        private static void AttachCupToHand(GameObject cupGO, Transform hand, Vector3 targetWorldScale, Vector3 localPosition, Vector3 localRotation)
        {
            if (Mathf.Abs(localRotation.x - 84f) < 10f || Mathf.Abs(localRotation.z - 12f) < 10f)
            {
                localRotation = new Vector3(0f, 0f, 90f);
            }
            cupGO.transform.SetParent(hand, false);

            Vector3 parentScale = hand.lossyScale;
            float scaleX = targetWorldScale.x / (parentScale.x != 0 ? Mathf.Abs(parentScale.x) : 1f);
            float scaleY = targetWorldScale.y / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
            float scaleZ = targetWorldScale.z / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
            cupGO.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            float posX = localPosition.x / (parentScale.x != 0 ? Mathf.Abs(parentScale.x) : 1f);
            float posY = localPosition.y / (parentScale.y != 0 ? Mathf.Abs(parentScale.y) : 1f);
            float posZ = localPosition.z / (parentScale.z != 0 ? Mathf.Abs(parentScale.z) : 1f);
            cupGO.transform.localPosition = new Vector3(posX, posY, posZ);
            cupGO.transform.localRotation = Quaternion.Euler(localRotation);
        }

        /// <summary>Xóa mô hình ly trà khỏi tay khi phục vụ xong.</summary>
        public static void DetachTeaCup()
        {
            if (heldTeaCupObj != null)
            {
                Destroy(heldTeaCupObj);
                heldTeaCupObj = null;
                Debug.Log("[CartItem] Đã tháo mô hình ly trà khỏi tay sau khi phục vụ.");
            }
        }

        private static string GetDrinkName(int drinkId)
        {
            return ChapterOrderCatalog.GetOrderName(drinkId);
        }

        private static GameObject cachedTeaCupPrefab;

        public static GameObject CreateStaticTeaCupModel(Vector3 worldPosition)
        {
            CartItem[] items = Object.FindObjectsByType<CartItem>(FindObjectsSortMode.None);
            CartItem cupTemplate = null;
            foreach (var item in items)
            {
                if (item != null && item.itemType == CartItemType.WaterCup)
                {
                    cupTemplate = item;
                    break;
                }
            }

            GameObject cupGO;
            if (cupTemplate != null)
            {
                cupGO = Instantiate(cupTemplate.gameObject);
                CartItem ci = cupGO.GetComponent<CartItem>();
                if (ci != null) Destroy(ci);
                foreach (var col in cupGO.GetComponentsInChildren<Collider>()) Destroy(col);
                cupGO.transform.position = worldPosition;
                cupGO.transform.rotation = Quaternion.identity;
                cupGO.transform.localScale = cupTemplate.transform.lossyScale;
            }
            else
            {
                GameObject prefab = null;
                if (activeInstance != null) prefab = activeInstance.teaCupHeldPrefab;
                if (prefab == null) prefab = cachedTeaCupPrefab;
                if (prefab == null)
                {
                    prefab = Resources.Load<GameObject>("lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture");
#if UNITY_EDITOR
                    if (prefab == null)
                    {
                        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture.fbx");
                    }
#endif
                    cachedTeaCupPrefab = prefab;
                    if (activeInstance != null && activeInstance.teaCupHeldPrefab == null)
                    {
                        activeInstance.teaCupHeldPrefab = prefab;
                    }
                }

                if (prefab != null)
                {
                    cupGO = Instantiate(prefab);
                    cupGO.transform.position = worldPosition;
                    cupGO.transform.rotation = Quaternion.identity;
                    cupGO.transform.localScale = Vector3.one * 7.34f;
                }
                else
                {
                    cupGO = CreateFallbackTeaCupModel();
                    cupGO.transform.position = worldPosition;
                    cupGO.transform.rotation = Quaternion.identity;
                    cupGO.transform.localScale = Vector3.one * 0.12f;
                    ApplyHeldCupMaterials(cupGO, true);
                }
            }

            cupGO.name = "PlacedTeaCup";
            AlignMeshBottomToPosition(cupGO, worldPosition);
            return cupGO;
        }

        public static GameObject CreateStaticPreparedOrderModel(int orderId, Vector3 worldPosition)
        {
            if (!ChapterOrderCatalog.IsChapter2Order(orderId))
            {
                return CreateStaticTeaCupModel(worldPosition);
            }

            GameObject food = CreateFallbackFoodModel(orderId);
            food.transform.position = worldPosition;
            food.transform.rotation = Quaternion.identity;
            food.transform.localScale = Vector3.one * 0.42f;
            food.name = "Placed_" + ChapterOrderCatalog.GetOrderName(orderId);
            AlignMeshBottomToPosition(food, worldPosition);
            return food;
        }

        public static void AlignMeshBottomToPosition(GameObject go, Vector3 worldPosition)
        {
            if (go == null) return;
            go.transform.position = worldPosition;
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                float minBoundsY = float.MaxValue;
                foreach (var r in rends)
                {
                    if (r != null && r.bounds.min.y < minBoundsY) minBoundsY = r.bounds.min.y;
                }
                if (minBoundsY < float.MaxValue)
                {
                    float diff = worldPosition.y - minBoundsY;
                    go.transform.position += new Vector3(0f, diff, 0f);
                }
            }
        }

        public static GameObject ReturnCleanCupToCart()
        {
            TeaCart cart = Object.FindAnyObjectByType<TeaCart>();
            Transform cartTransform = cart != null ? cart.transform : null;
            Transform anchor = FindCleanCupReturnAnchor(cartTransform);

            int slot = returnedCleanCupVisualIndex++ % 5;
            Vector3 right = FlattenHorizontal(cartTransform != null ? cartTransform.right : Vector3.right, Vector3.right);
            Vector3 forward = FlattenHorizontal(cartTransform != null ? cartTransform.forward : Vector3.forward, Vector3.forward);
            Vector3 basePos = anchor != null
                ? anchor.position
                : (cartTransform != null ? cartTransform.position + Vector3.up * 0.85f : Vector3.up);

            Vector3 worldPosition = basePos
                + right * (0.12f * ((slot + 2) % 3))
                + forward * (0.11f * ((slot + 2) / 3));

            string cupName = $"ReturnedCleanCup_{slot + 1}";
            GameObject previousCup = GameObject.Find(cupName);
            if (previousCup != null)
            {
                Destroy(previousCup);
            }

            GameObject cupGO;
            if (anchor != null)
            {
                cupGO = Instantiate(anchor.gameObject);
                cupGO.name = cupName;
                CartItem ci = cupGO.GetComponent<CartItem>();
                if (ci != null) Destroy(ci);
                foreach (var col in cupGO.GetComponentsInChildren<Collider>()) Destroy(col);
                cupGO.transform.position = new Vector3(worldPosition.x, anchor.position.y, worldPosition.z);
                cupGO.transform.rotation = anchor.rotation;
                cupGO.transform.localScale = anchor.lossyScale;
            }
            else
            {
                GameObject prefab = null;
                if (activeInstance != null) prefab = activeInstance.teaCupHeldPrefab;
                if (prefab == null) prefab = cachedTeaCupPrefab;
                if (prefab == null)
                {
                    prefab = Resources.Load<GameObject>("lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture");
#if UNITY_EDITOR
                    if (prefab == null)
                    {
                        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture.fbx");
                    }
#endif
                    cachedTeaCupPrefab = prefab;
                }

                if (prefab != null)
                {
                    cupGO = Instantiate(prefab);
                    cupGO.name = cupName;
                    cupGO.transform.position = worldPosition;
                    cupGO.transform.rotation = Quaternion.identity;
                    cupGO.transform.localScale = Vector3.one * 7.34f;
                }
                else
                {
                    cupGO = CreateFallbackEmptyCupModel();
                    cupGO.name = cupName;
                    ApplyHeldCupMaterials(cupGO, false);
                    cupGO.transform.position = worldPosition;
                    cupGO.transform.rotation = Quaternion.identity;
                    cupGO.transform.localScale = Vector3.one * 0.12f;
                }
            }

            if (cartTransform != null)
            {
                cupGO.transform.SetParent(cartTransform, true);
            }

            return cupGO;
        }

        private static Transform FindCleanCupReturnAnchor(Transform cartTransform)
        {
            if (cartTransform != null)
            {
                CartItem[] cartItems = cartTransform.GetComponentsInChildren<CartItem>(true);
                foreach (CartItem item in cartItems)
                {
                    if (item != null && item.itemType == CartItemType.WaterCup)
                    {
                        return item.transform;
                    }
                }
            }

            GameObject cupObj = GameObject.Find("WaterCupProp_1");
            return cupObj != null ? cupObj.transform : null;
        }

        private static Vector3 FlattenHorizontal(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.normalized;
        }

        private static Transform FindRightHandBone(Transform root)
        {
            // Tìm bone tay phải theo các tên phổ biến của Unity Humanoid / Mixamo / Meshy
            string[] candidateNames = {
                "RightHand", "Hand_R", "R_Hand", "hand_r", "mixamorig:RightHand",
                "Bip001 R Hand", "RHand", "HandRight"
            };

            // Tìm trong SkinnedMeshRenderer trước để tránh lỗi Optimize Game Objects
            var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone != null && System.Array.IndexOf(candidateNames, bone.name) >= 0)
                        return bone;
                }
            }

            foreach (string name in candidateNames)
            {
                Transform found = root.Find(name);
                if (found != null) return found;

                // Tìm đệ quy trong toàn bộ cây
                found = FindChildRecursive(root, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void ApplyHeldCupMaterials(GameObject cup, bool filled)
        {
            if (cup == null) return;

            Color cupColor = filled
                ? new Color(0.82f, 0.42f, 0.12f, 0.88f)
                : new Color(0.86f, 0.95f, 1f, 0.42f);
            Material cupMaterial = CreateCupMaterial(filled ? "HeldTeaCup_Amber" : "HeldEmptyCup_Glass", cupColor, true, filled ? 3001 : 3000);

            var renderers = cup.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = cupMaterial;
            }
        }

        private static Material CreateCupMaterial(string name, Color color, bool transparent, int renderQueue)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.name = name;
            material.color = color;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = renderQueue;
            }

            return material;
        }

        private static GameObject CreateFallbackEmptyCupModel()
        {
            GameObject cup = new GameObject("FallbackEmptyCup");

            // Thân ly (Cylinder trong suốt trắng)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(cup.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 0.6f, 1f);
            Object.Destroy(body.GetComponent<Collider>());

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = CreateCupMaterial("FallbackEmptyCup_Glass", new Color(0.9f, 0.95f, 1f, 0.25f), true, 3000);
                if (mat != null)
                {
                    mat.color = new Color(0.9f, 0.95f, 1f, 0.25f); // Thủy tinh trắng trong suốt
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }

            return cup;
        }

        private static GameObject CreateFallbackTeaCupModel()
        {
            GameObject cup = new GameObject("FallbackTeaCup");

            // 1. Thân ly (Cylinder trong suốt trắng)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(cup.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 0.6f, 1f);
            Object.Destroy(body.GetComponent<Collider>());

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = CreateCupMaterial("FallbackTeaCup_Glass", new Color(0.9f, 0.95f, 1f, 0.25f), true, 3000);
                if (mat != null)
                {
                    mat.color = new Color(0.9f, 0.95f, 1f, 0.25f); // Thủy tinh trắng trong suốt
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }

            // 2. Nước trà bên trong (Cylinder màu hổ phách/cam trà)
            GameObject teaLiquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teaLiquid.transform.SetParent(cup.transform, false);
            teaLiquid.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            teaLiquid.transform.localScale = new Vector3(0.92f, 0.5f, 0.92f);
            Object.Destroy(teaLiquid.GetComponent<Collider>());

            var liquidRend = teaLiquid.GetComponent<Renderer>();
            if (liquidRend != null)
            {
                Material mat = CreateCupMaterial("FallbackTeaCup_Liquid", new Color(0.75f, 0.42f, 0.12f, 0.85f), true, 3001);
                if (mat != null)
                {
                    mat.color = new Color(0.75f, 0.42f, 0.12f, 0.85f); // Màu trà đá hổ phách
                    mat.SetFloat("_Mode", 3f); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3001; // Render sau ly thủy tinh
                    liquidRend.material = mat;
                }
            }

            // 3. Đá viên (Tạo 3 Cube nhỏ màu trắng đục bay ở phần trên ly nước)
            Vector3[] icePositions = {
                new Vector3(0.18f, 0.15f, 0.12f),
                new Vector3(-0.15f, 0.18f, -0.1f),
                new Vector3(-0.05f, 0.2f, 0.2f)
            };
            Vector3[] iceRotations = {
                new Vector3(25f, 40f, 15f),
                new Vector3(-35f, 12f, 45f),
                new Vector3(10f, -60f, -20f)
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject iceCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                iceCube.transform.SetParent(cup.transform, false);
                iceCube.transform.localPosition = icePositions[i];
                iceCube.transform.localRotation = Quaternion.Euler(iceRotations[i]);
                iceCube.transform.localScale = Vector3.one * 0.3f;
                Object.Destroy(iceCube.GetComponent<Collider>());

                var iceRend = iceCube.GetComponent<Renderer>();
                if (iceRend != null)
                {
                    Material mat = CreateCupMaterial("FallbackTeaCup_Ice", new Color(0.92f, 0.96f, 1f, 0.65f), true, 3002);
                    if (mat != null)
                    {
                        mat.color = new Color(0.92f, 0.96f, 1f, 0.65f); // Đá viên mờ đục trong suốt
                        mat.SetFloat("_Mode", 3f);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = 3002; // Render sau cùng
                        iceRend.material = mat;
                    }
                }
            }

            return cup;
        }

        private static GameObject CreateFallbackFoodModel(int orderId)
        {
            GameObject root = new GameObject("FallbackFood");

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(1.2f, 0.06f, 1.2f);
            Object.Destroy(plate.GetComponent<Collider>());
            SetRendererColor(plate, new Color(0.92f, 0.88f, 0.76f, 1f));

            if (orderId == ChapterOrderCatalog.BanhMiMuoiOt)
            {
                GameObject bread = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bread.transform.SetParent(root.transform, false);
                bread.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                bread.transform.localScale = new Vector3(1.25f, 0.18f, 0.38f);
                bread.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                Object.Destroy(bread.GetComponent<Collider>());
                SetRendererColor(bread, new Color(0.96f, 0.61f, 0.22f, 1f));

                AddTopping(root.transform, new Vector3(-0.2f, 0.25f, 0.02f), new Color(0.8f, 0.1f, 0.05f, 1f));
                AddTopping(root.transform, new Vector3(0.22f, 0.25f, -0.04f), new Color(0.2f, 0.75f, 0.22f, 1f));
            }
            else if (orderId == ChapterOrderCatalog.BanhTrangNuong)
            {
                GameObject ricePaper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ricePaper.transform.SetParent(root.transform, false);
                ricePaper.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                ricePaper.transform.localScale = new Vector3(1.0f, 0.035f, 1.0f);
                Object.Destroy(ricePaper.GetComponent<Collider>());
                SetRendererColor(ricePaper, new Color(0.98f, 0.74f, 0.28f, 1f));

                AddTopping(root.transform, new Vector3(-0.16f, 0.18f, 0.12f), new Color(0.95f, 0.94f, 0.65f, 1f));
                AddTopping(root.transform, new Vector3(0.15f, 0.18f, -0.08f), new Color(0.78f, 0.08f, 0.04f, 1f));
                AddTopping(root.transform, new Vector3(0.02f, 0.19f, 0.02f), new Color(0.15f, 0.65f, 0.18f, 1f));
            }
            else
            {
                GameObject skewer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                skewer.transform.SetParent(root.transform, false);
                skewer.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                skewer.transform.localScale = new Vector3(1.25f, 0.035f, 0.035f);
                skewer.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
                Object.Destroy(skewer.GetComponent<Collider>());
                SetRendererColor(skewer, new Color(0.38f, 0.18f, 0.08f, 1f));

                AddTopping(root.transform, new Vector3(-0.32f, 0.23f, -0.05f), new Color(1f, 0.46f, 0.24f, 1f));
                AddTopping(root.transform, new Vector3(0f, 0.23f, 0f), new Color(0.96f, 0.82f, 0.48f, 1f));
                AddTopping(root.transform, new Vector3(0.32f, 0.23f, 0.05f), new Color(1f, 0.34f, 0.2f, 1f));
            }

            return root;
        }

        private static void AddTopping(Transform parent, Vector3 localPosition, Color color)
        {
            GameObject topping = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topping.transform.SetParent(parent, false);
            topping.transform.localPosition = localPosition;
            topping.transform.localScale = Vector3.one * 0.14f;
            Object.Destroy(topping.GetComponent<Collider>());
            SetRendererColor(topping, color);
        }

        private static void SetRendererColor(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;
        }

        private System.Collections.IEnumerator CoolDownRoutine()
        {
            float coolDownSeconds = boiledWaterCoolDownGameMinutes / Constants.GAME_MINUTES_PER_REAL_SECOND;
            yield return new WaitForSeconds(coolDownSeconds);
            isWaterBoiled = false;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước trong ấm đã nguội rồi, cần đun sôi lại để pha trà.");
            activeCoolDownCoroutine = null;
        }

        private System.Collections.IEnumerator BoilWaterRoutine(GameObject kettle, GameObject stove, GameObject water)
        {
            isBoilingWater = true;
            isWaterBoiled = false;

            if (activeCoolDownCoroutine != null)
            {
                StopCoroutine(activeCoolDownCoroutine);
                activeCoolDownCoroutine = null;
            }

            Transform kettleT = kettle.transform;
            Vector3 kettleOrigPos = kettleT.position;
            Quaternion kettleOrigRot = kettleT.rotation;

            bool needsRefill = kettleWater <= minKettleWaterToRefill;

            if (needsRefill)
            {
                if (bottleWater <= 0.01f)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Bình nước Sài Gòn Aquwa đã hết sạch nước rồi! Không thể đun.");
                    isBoilingWater = false;
                    yield break;
                }

                // 1. Cầm ấm qua bình nước lấy nước
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Cầm ấm nước qua bình nước Sài Gòn Aquwa để hứng nước sạch...");
                Vector3 pourPos = water.transform.position + Vector3.up * 0.38f;
                Quaternion pourRot = Quaternion.Euler(-45f, 180f, 0f); // Nghiêng ấm để hứng/rót nước
                yield return StartCoroutine(SmoothMove(kettleT, pourPos, pourRot, 1.5f));

                float refillAmount = maxKettleWater - kettleWater;
                if (bottleWater < refillAmount)
                {
                    refillAmount = bottleWater;
                }
                bottleWater -= refillAmount;
                kettleWater += refillAmount;
                ShowResourceDelta($"-{refillAmount:F1}L nước bình (còn {bottleWater:F1}L)");

                // Hứng nước trong 2 giây
                yield return new WaitForSeconds(2f);
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước trong ấm vẫn còn nhiều, bắt đầu đun sôi trực tiếp...");
            }

            // 2. Đặt ấm nước lên bếp ga đun sôi
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đặt ấm lên bếp ga Namilux và bật lửa đun sôi...");
            Vector3 stovePos = GetKettleBoilPosition(kettle, stove); // Sit the kettle on top of the burner instead of inside it.
            Quaternion stoveRot = Quaternion.identity; // Phẳng ngang
            yield return StartCoroutine(SmoothMove(kettleT, stovePos, stoveRot, 1.5f));

            // Tạo hiệu ứng hơi nước
            GameObject steamFx = CreateSteamParticles(kettleT);

            // Đun nước trong 10 giây
            yield return new WaitForSeconds(10f);

            // Hủy hiệu ứng hơi nước
            if (steamFx != null)
            {
                Destroy(steamFx);
            }

            isWaterBoiled = true;
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Nước đã sôi sùng sục 100 độ C! Nhấc ấm nước nóng đặt lại chỗ cũ.");

            // 3. Di chuyển ấm về vị trí ban đầu
            yield return StartCoroutine(SmoothMove(kettleT, kettleOrigPos, kettleOrigRot, 1.5f));

            isBoilingWater = false;
            EventManager.TriggerInteractionPromptShow("Nhấn F để tương tác");

            // Bắt đầu đếm ngược 5 giây nguội
            activeCoolDownCoroutine = StartCoroutine(CoolDownRoutine());
        }

        private static Vector3 GetKettleBoilPosition(GameObject kettle, GameObject stove)
        {
            if (!TryGetRendererBounds(stove, out Bounds stoveBounds) ||
                !TryGetRendererBounds(kettle, out Bounds kettleBounds))
            {
                return stove.transform.position + Vector3.up * 0.24f;
            }

            float kettlePivotToBottom = kettle.transform.position.y - kettleBounds.min.y;
            return new Vector3(
                stoveBounds.center.x,
                stoveBounds.max.y + kettlePivotToBottom + 0.025f,
                stoveBounds.center.z);
        }

        private static bool TryGetRendererBounds(GameObject obj, out Bounds bounds)
        {
            bounds = default;
            if (obj == null) return false;

            Renderer[] objectRenderers = obj.GetComponentsInChildren<Renderer>(true);
            if (objectRenderers.Length == 0) return false;

            bounds = objectRenderers[0].bounds;
            for (int i = 1; i < objectRenderers.Length; i++)
            {
                bounds.Encapsulate(objectRenderers[i].bounds);
            }

            return true;
        }

        private System.Collections.IEnumerator SmoothMove(Transform target, Vector3 destPos, Quaternion destRot, float duration)
        {
            Vector3 startPos = target.position;
            Quaternion startRot = target.rotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                target.position = Vector3.Lerp(startPos, destPos, t);
                target.rotation = Quaternion.Lerp(startRot, destRot, t);
                yield return null;
            }
            target.position = destPos;
            target.rotation = destRot;
        }

        private GameObject CreateSteamParticles(Transform parent)
        {
            GameObject steam = new GameObject("SteamParticles");
            steam.transform.SetParent(parent);
            steam.transform.localPosition = new Vector3(0f, 0.15f, 0.05f); // Đầu vòi ấm
            steam.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem ps = steam.AddComponent<ParticleSystem>();
            
            // Cấu hình ParticleSystem.MainModule
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 0.4f;
            main.startSize = 0.02f;
            main.maxParticles = 60;
            main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.25f); // Hơi nước mờ nhẹ màu xám trắng
            main.gravityModifier = -0.08f; // Hơi bay lên trên

            // Cấu hình Emission
            var emission = ps.emission;
            emission.rateOverTime = 20f;

            // Cấu hình Shape (Cone)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.015f;

            // Cấu hình ColorOverLifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.35f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Thiết lập Material tương thích runtime
            var renderer = steam.GetComponent<ParticleSystemRenderer>();
            Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (defaultShader != null)
            {
                renderer.sharedMaterial = new Material(defaultShader);
            }

            return steam;
        }

        private void OnWaterCupInteract(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (hasPreparedTea)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Bạn đang có 1 ly trà đá pha sẵn rồi! Hãy phục vụ khách hàng trước (nhấn Space hoặc đi đến bàn khách đặt ly xuống).");
                return;
            }

            if (isHoldingCup)
            {
                if (IsHoldingDirtyCup)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Bạn đang cầm ly dơ! Hãy mang đến bồn rửa ly trước khi lấy ly mới.");
                    return;
                }
                if (IsCupClean)
                {
                    ResetCupState();
                    stats.AddSupplies(0, 0, 1);
                    ShowResourceDelta($"+1 cốc (còn {stats.CupSupply})");
                    EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã đặt ly trống trở lại xe đẩy. (+1 cốc, còn {stats.CupSupply})");
                    Debug.Log("[CartItem] Trả ly trống chưa thêm nguyên liệu về chỗ cũ trên xe đẩy");
                    if (UI.RecipeMiniGameUI.Instance != null)
                    {
                        UI.RecipeMiniGameUI.Instance.UndoStep();
                    }
                    return;
                }
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Ly đã thêm nguyên liệu không thể đặt lại vào chỗ cũ! Hãy mang tới bồn rửa ly để rửa mới có thể bỏ vào chỗ cũ.");
                return;
            }

            if (stats.CupSupply < 1)
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Hết ly sạch rồi! Cần mua thêm ly.");
                return;
            }

            // Bắt đầu cầm ly pha chế
            stats.TakeOneCup();
            isHoldingCup = true;
            teaInCup = 0;
            coffeeInCup = 0;
            waterInCup = 0f;
            iceInCup = 0f;

            ShowResourceDelta($"-1 cốc (còn {stats.CupSupply})");
            EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã lấy 1 ly sạch đặt lên tay. (-1 cốc, còn {stats.CupSupply})");
            Debug.Log("[CartItem] Tương tác ly nước -> Cầm ly pha chế");

            // Gắn mô hình ly trống lên tay Hoàng Hôn
            AttachEmptyCupToPlayer(player);
            
            // Báo cho UI (nếu có)
            if (UI.RecipeMiniGameUI.Instance != null)
            {
                UI.RecipeMiniGameUI.Instance.OnIngredientAdded("Ly Trà");
            }
        }

        private System.Collections.IEnumerator ClickFeedback()
        {
            isInteracting = true;

            // Rung nhẹ — scale up rồi down
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 targetScale = originalScale * 1.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (t < 0.5f)
                {
                    // Scale up
                    transform.localScale = Vector3.Lerp(originalScale, targetScale, t * 2f);
                }
                else
                {
                    // Scale down
                    transform.localScale = Vector3.Lerp(targetScale, originalScale, (t - 0.5f) * 2f);
                }

                yield return null;
            }

            transform.localScale = originalScale;
            isInteracting = false;
        }

        private void OnDestroy()
        {
            // Cleanup materials
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
