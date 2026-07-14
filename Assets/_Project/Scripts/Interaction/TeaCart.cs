using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Interaction
{
    /// <summary>
    /// Xe trà đá chính của người chơi.
    /// Phục vụ khách hàng, sửa chữa.
    /// Nhấn F gần xe → góc nhìn thứ 1 từ mặt bàn xe đẩy (First Person).
    /// </summary>
    public class TeaCart : Interactable
    {
        [SerializeField] private bool needsRepair = false;
        [SerializeField] private Transform cameraViewPoint;

        [Header("Cart First Person — Điểm căn cứ mặt bàn")]
        [Tooltip("Transform chính giữa mặt bàn xe đẩy, camera sẽ đứng nhìn từ đây")]
        [SerializeField] private Transform cartOrbitCenter;

        private const float TargetCartVisualHeight = 2.35f;
        private const float CartCameraTableHeightRatio = 0.42f;
        private const float CartCameraForwardPadding = 0.52f;
        private const float CartCameraHeightAboveTable = 0.82f;
        private const float CartCameraLookHeightAboveTable = -0.02f;
        private bool isPlayerInteracting = false;
        private Player.PlayerController interactingPlayer;
        private Renderer[] hiddenPlayerRenderers;
        private Transform dynamicCameraViewPoint;
        private bool hasStableCameraViewPose;
        private Vector3 stableCameraViewLocalPosition;
        private Quaternion stableCameraViewLocalRotation;
        private Vector3 playerPositionBeforeInteraction;
        private Quaternion playerRotationBeforeInteraction;
        private bool hasSavedPlayerTransform;

        /// <summary>
        /// Có đang ở chế độ tương tác xe đẩy không.
        /// </summary>
        public bool IsPlayerInteracting => isPlayerInteracting;

        private void Start()
        {
            // Nếu chưa gán cartOrbitCenter, dùng chính transform của xe đẩy
            if (cartOrbitCenter == null) cartOrbitCenter = transform;
            // Khởi tạo Recipe UI để nó nghe sự kiện
            var initUI = UI.RecipeMiniGameUI.Instance;

            ScaleCartForStreetPresence();
            promptText = "Nhấn F để tương tác xe đẩy";
        }

        private void Update()
        {
            if (!isPlayerInteracting && Systems.BusinessDayController.HasInstance)
            {
                promptText = Systems.BusinessDayController.Instance.GetCartPrompt("Nhấn F để tương tác xe đẩy");
            }
        }

        private void OnDisable()
        {
            if (isPlayerInteracting)
            {
                RestorePlayerTransform(interactingPlayer);
                SetPlayerVisualsVisible(interactingPlayer, true);
                EventManager.TriggerCartInteractionChanged(false);
            }
        }

        private void ScaleCartForStreetPresence()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (bounds.size.y <= 0.001f) return;
            float scaleFactor = TargetCartVisualHeight / bounds.size.y;
            if (scaleFactor <= 1.03f) return;

            float bottomBefore = bounds.min.y;
            transform.localScale *= scaleFactor;

            renderers = GetComponentsInChildren<Renderer>(true);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            transform.position += Vector3.up * (bottomBefore - bounds.min.y);
        }

        protected override void OnInteract(Player.PlayerController player)
        {
            if (isPlayerInteracting)
            {
                ExitCartInteraction(player);
                return;
            }

            if (Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.TryHandleCartInteractionOverride())
            {
                return;
            }

            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop
                    ? Systems.BusinessDayController.Instance.CanServeCustomers
                    : dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;

            if (needsRepair)
            {
                if (!isNight)
                {
                    RepairCart(player.GetComponent<Player.PlayerStats>());
                }
                else
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Trời tối rồi, giờ không thể sửa xe được.");
                }
                return;
            }

            EnterCartInteraction(player);
        }

        private void EnterCartInteraction(Player.PlayerController player)
        {
            isPlayerInteracting = true;
            interactingPlayer = player;
            playerPositionBeforeInteraction = player.transform.position;
            playerRotationBeforeInteraction = player.transform.rotation;
            hasSavedPlayerTransform = true;
            EventManager.TriggerCartInteractionChanged(true);
            player.SetState(PlayerState.Interacting);
            player.DisableMovement();

            // Tính toán hướng forward nằm ngang thực tế của xe đẩy (tránh bị nghiêng do mô hình import -90 độ X)
            Vector3 cartForward;
            if (cameraViewPoint != null)
            {
                cartForward = transform.position - cameraViewPoint.position;
            }
            else
            {
                cartForward = transform.up; // Trục Y cục bộ của xe đẩy tương đương với hướng dọc của xe trong thế giới thực
            }
            cartForward.y = 0f;
            if (cartForward.sqrMagnitude < 0.001f)
                cartForward = transform.forward;
            cartForward.Normalize();

            StopPlayerRigidbody(player);
            SetPlayerVisualsVisible(player, false);

            // ═══ BẬT CAMERA GÓC NHÌN THỨ 1 TỪ MẶT BÀN XE ĐẨY ═══
            var cam = Camera.main != null ? Camera.main.GetComponent<Player.CinematicCamera>() : null;
            if (cam != null)
            {
                Transform viewPoint = BuildClearTableCameraPoint(cartForward);
                cam.EnableCartFirstPerson(viewPoint);
            }

            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop
                    ? Systems.BusinessDayController.Instance.CanServeCustomers
                    : dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;
            if (isNight)
            {
                promptText = "Nhìn từ mặt bàn | Click vật phẩm | Space phục vụ | F thoát";
            }
            else
            {
                promptText = "Nhìn từ mặt bàn | Click vật phẩm trên bàn | F thoát";
            }
            EventManager.TriggerInteractionPromptShow(promptText);
        }

        private Transform BuildClearTableCameraPoint(Vector3 cartForward)
        {
            if (dynamicCameraViewPoint == null)
            {
                GameObject point = new GameObject("RuntimeCartCameraPoint");
                point.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                point.transform.SetParent(transform, true);
                dynamicCameraViewPoint = point.transform;
            }

            if (cartForward.sqrMagnitude < 0.001f)
            {
                cartForward = transform.forward;
                cartForward.y = 0f;
            }
            cartForward.Normalize();

            if (hasStableCameraViewPose)
            {
                dynamicCameraViewPoint.localPosition = stableCameraViewLocalPosition;
                dynamicCameraViewPoint.localRotation = stableCameraViewLocalRotation;
                return dynamicCameraViewPoint;
            }

            if (!TryGetVisualBounds(out Bounds bounds))
            {
                Transform fallback = (cameraViewPoint != null) ? cameraViewPoint : cartOrbitCenter;
                if (fallback != null)
                {
                    dynamicCameraViewPoint.position = fallback.position;
                    dynamicCameraViewPoint.rotation = fallback.rotation;
                    CacheStableCameraViewPose();
                    return dynamicCameraViewPoint;
                }

                dynamicCameraViewPoint.position = transform.position - cartForward * 1.6f + Vector3.up * 1.25f;
                dynamicCameraViewPoint.rotation = Quaternion.LookRotation(cartForward, Vector3.up);
                CacheStableCameraViewPose();
                return dynamicCameraViewPoint;
            }

            float tableY = Mathf.Lerp(bounds.min.y, bounds.max.y, CartCameraTableHeightRatio);
            float horizontalDepth = Mathf.Max(bounds.extents.x, bounds.extents.z);
            Vector3 tableCenter = new Vector3(transform.position.x, tableY, transform.position.z);
            Vector3 cameraPosition = tableCenter - cartForward * (horizontalDepth + CartCameraForwardPadding);
            cameraPosition.y = tableY + CartCameraHeightAboveTable;

            Vector3 lookTarget = tableCenter + Vector3.up * CartCameraLookHeightAboveTable + cartForward * 0.35f;
            dynamicCameraViewPoint.position = cameraPosition;
            dynamicCameraViewPoint.rotation = Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up);
            CacheStableCameraViewPose();
            return dynamicCameraViewPoint;
        }

        private void CacheStableCameraViewPose()
        {
            if (dynamicCameraViewPoint == null) return;

            stableCameraViewLocalPosition = dynamicCameraViewPoint.localPosition;
            stableCameraViewLocalRotation = dynamicCameraViewPoint.localRotation;
            hasStableCameraViewPose = true;
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void ExitCartInteraction(Player.PlayerController player)
        {
            isPlayerInteracting = false;
            interactingPlayer = null;
            EventManager.TriggerCartInteractionChanged(false);
            RestorePlayerTransform(player);
            SetPlayerVisualsVisible(player, true);
            player.SetState(PlayerState.Idle);
            player.EnableMovement();

            // ═══ TẮT CAMERA GÓC NHÌN THỨ 1 ═══
            var cam = Camera.main != null ? Camera.main.GetComponent<Player.CinematicCamera>() : null;
            if (cam != null)
            {
                cam.DisableCartFirstPerson();
            }

            promptText = "Nhấn F để tương tác xe đẩy";
            EventManager.TriggerInteractionPromptShow(promptText);
        }

        private void RestorePlayerTransform(Player.PlayerController player)
        {
            if (player == null || !hasSavedPlayerTransform) return;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = playerPositionBeforeInteraction;
                rb.rotation = playerRotationBeforeInteraction;
            }
            else
            {
                player.transform.SetPositionAndRotation(playerPositionBeforeInteraction, playerRotationBeforeInteraction);
            }

            hasSavedPlayerTransform = false;
        }

        private void StopPlayerRigidbody(Player.PlayerController player)
        {
            if (player == null) return;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private void SetPlayerVisualsVisible(Player.PlayerController player, bool visible)
        {
            if (player == null) return;

            if (!visible)
            {
                hiddenPlayerRenderers = player.GetComponentsInChildren<Renderer>(true);
            }

            if (hiddenPlayerRenderers == null) return;

            foreach (Renderer renderer in hiddenPlayerRenderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            if (visible)
            {
                hiddenPlayerRenderers = null;
            }
        }

        public void ServeFromFirstPerson(Player.PlayerController player)
        {
            var dayNight = FindAnyObjectByType<Economy.DayNightCycle>();
            bool isNight = Systems.BusinessDayController.HasInstance &&
                Systems.BusinessDayController.Instance.IsManagingGameLoop
                    ? Systems.BusinessDayController.Instance.CanServeCustomers
                    : dayNight != null && dayNight.CurrentTimeOfDay == TimeOfDay.Night;

            if (isNight)
            {
                ServeNearestCustomer(player);
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Quán chưa mở cửa, chưa thể phục vụ khách.");
            }
        }

        private void ServeNearestCustomer(Player.PlayerController player)
        {
            var stats = player.GetComponent<Player.PlayerStats>();
            if (stats == null) return;

            if (!stats.HasSuppliesForTea())
            {
                if (CartItem.IsHoldingCup)
                {
                    if (CartItem.IsHoldingDirtyCup)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Bạn đang cầm ly dơ! Hãy mang đến bồn rửa ly để rửa tái sử dụng trước.");
                        return;
                    }
                    string brewBase = CartItem.CoffeeInCup > 0
                        ? $"Cà phê {CartItem.CoffeeInCup}g/30g"
                        : $"Trà {CartItem.TeaInCup}g/50g";
                    EventManager.TriggerDialogueLine("Hoàng Hôn", $"Ly chưa pha xong! (Hiện có: {brewBase}, Nước {Mathf.RoundToInt(CartItem.WaterInCup * 1000f)}ml/200ml, Đá {CartItem.IceInCup}%/5%)");
                }
                else
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có ly nào được pha! Hãy nhấp vào ly nước trên bàn để lấy ly và bắt đầu pha chế.");
                }
                return;
            }

            // Tìm khách hàng đang Wait gần nhất
            var npcs = FindObjectsByType<NPC.NPCController>(FindObjectsInactive.Exclude);
            NPC.NPCController closestWaiting = null;
            float minDist = float.MaxValue;

            foreach (var npc in npcs)
            {
                if (npc.CurrentState == NPCState.Waiting)
                {
                    float dist = Vector3.Distance(transform.position, npc.transform.position);
                    if (dist < 3f && dist < minDist) // Khách hàng ở gần xe
                    {
                        minDist = dist;
                        closestWaiting = npc;
                    }
                }
            }

            if (closestWaiting != null)
            {
                string preparedDrinkName = CartItem.PreparedDrinkName;
                if (CartItem.PreparedDrinkId >= 0 && CartItem.PreparedDrinkId != closestWaiting.OrderedDrinkId)
                {
                    EventManager.TriggerDialogueLine("Hoàng Hôn", $"Khách gọi {closestWaiting.OrderedDrinkName}, nhưng món đang có là {preparedDrinkName}. Chuẩn bị lại đúng món trước đã.");
                    return;
                }

                // Play animation serving
                player.SetState(PlayerState.Serving);
                stats.UseTeaSupplies();
                closestWaiting.ServeDrink();

                // Đặt ly trà đá tĩnh lên bàn ảo trước mặt khách nếu ghế ngồi hợp lệ
                if (closestWaiting.TargetSeat != null)
                {
                    Vector3 tablePos = closestWaiting.TargetSeat.transform.position + closestWaiting.TargetSeat.transform.forward * 0.5f;
                    tablePos.y = closestWaiting.TargetSeat.GetTableSurfaceY();
                    GameObject placedOrder = CartItem.CreateStaticPreparedOrderModel(CartItem.PreparedDrinkId, tablePos);
                    closestWaiting.TargetSeat.PlacedCupObj = placedOrder;
                }

                // Tháo mô hình ly trà đá khỏi tay nhân vật sau khi phục vụ
                CartItem.DetachTeaCup();
                EventManager.TriggerDialogueLine("Hoàng Hôn", $"Đã trao {preparedDrinkName} cho khách! Cảm ơn vì đã đến ủng hộ.");
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Chưa có ai gọi món cả.");
            }
        }

        private void RepairCart(Player.PlayerStats stats)
        {
            if (stats == null) return;

            if (stats.SpendMoney(Constants.CART_REPAIR_COST))
            {
                needsRepair = false;
                promptText = "Nhấn F để tương tác xe đẩy";
                EventManager.TriggerDialogueLine("Hoàng Hôn", "May quá, xe vẫn còn dùng được.");
            }
            else
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Không đủ tiền sửa xe rồi...");
            }
        }
    }
}
