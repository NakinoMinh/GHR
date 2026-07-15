using UnityEngine;
using GanhHangRong.Core;
using UnityEngine.InputSystem;

namespace GanhHangRong.Player
{
    /// <summary>
    /// Camera góc nhìn thứ ba (Third Person Orbit Follow Camera) xoay bằng chuột.
    /// Hỗ trợ chế độ quay quanh xe đẩy (Cart Orbit) khi tương tác.
    /// </summary>
    public class CinematicCamera : MonoBehaviour
    {
        public const string MouseSensitivityPreferenceKey = "GHR_MouseSensitivity";
        public const float DefaultMouseSensitivity = 2f;
        public const float MinimumMouseSensitivity = 0.25f;
        public const float MaximumMouseSensitivity = 5f;

        [Header("Mục Tiêu")]
        [SerializeField] private Transform target;

        [Header("Theo Dõi")]
        [SerializeField] private float smoothTime = Constants.CAMERA_SMOOTH_TIME;
        [SerializeField] private float yOffset = Constants.CAMERA_Y_OFFSET;
        [SerializeField] private float zOffset = Constants.CAMERA_Z_OFFSET;

        [Header("Over The Shoulder Settings")]
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.7f, 0.15f, 0f);

        [Header("Third Person Orbit Settings")]
        [SerializeField] private float mouseSensitivity = DefaultMouseSensitivity;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float distance = 3f;

        [Header("Góc nhìn bàn trà (First Person / Cart Table)")]
        [SerializeField] private Transform customViewTarget;
        [SerializeField] private bool useCustomView = false;
        [SerializeField] private float customViewSmoothSpeed = 5f;

        [Header("Cart First Person Settings — Góc nhìn thứ 1 từ mặt bàn xe đẩy")]
        [SerializeField] private float cartFPYOffset = 1.0f;
        [Tooltip("Lùi ra phía sau mặt bàn bao nhiêu (m) khi vào góc nhìn thứ 1")]
        [SerializeField] private float cartFPBackOffset = 0.3f;
        [SerializeField] private float cartFPSmoothSpeed = 15f;

        [Header("Cart Orbit Settings — Góc nhìn thứ 3 quanh xe đẩy (legacy)")]
        [SerializeField] private float cartOrbitDistance = 2.5f;
        [SerializeField] private float cartOrbitYOffset = 1.2f;
        [SerializeField] private float cartOrbitSensitivity = 3f;
        [SerializeField] private float cartOrbitMinPitch = -10f;
        [SerializeField] private float cartOrbitMaxPitch = 60f;
        [SerializeField] private float cartOrbitSmoothSpeed = 12f;

        private float yaw = 0f;
        private float pitch = 12f;
        private Vector3 currentVelocity = Vector3.zero;

        // Cart orbit state
        private bool isCartOrbitMode = false;
        private Transform cartOrbitCenter;
        private float cartOrbitYaw = 0f;
        private float cartOrbitPitch = 30f;

        // First Person Cart View state
        private bool isCartFirstPersonMode = false;
        private Transform cartFPCenter;
        private Vector3 cartFPTargetPos;
        private Quaternion cartFPTargetRot;
        private float fpYaw = 0f;
        private float fpPitch = 30f;

        // Raycast cho tương tác vật phẩm trên xe đẩy
        [SerializeField] private float cartItemRaycastInterval = 0.04f;
        private Interaction.CartItem currentHoveredItem;
        private Interaction.Chapter2FoodItem currentHoveredFoodItem;
        private Camera cachedCamera;
        private PlayerController cachedPlayer;
        private PlayerStats cachedPlayerStats;
        private Interaction.TeaCart cachedTeaCart;
        private readonly RaycastHit[] cartItemRaycastBuffer = new RaycastHit[128];
        private float nextCartItemRaycastTime;

        // NPC Focus state
        private bool isFocusingNPC = false;
        private Vector3 focusPos;
        private Quaternion focusRot;

        public bool IsCartOrbitMode => isCartOrbitMode;
        public bool IsCartFirstPersonMode => isCartFirstPersonMode;
        public float MouseSensitivity => mouseSensitivity;

        private void Start()
        {
            cachedCamera = GetComponent<Camera>();
            SetMouseSensitivity(PlayerPrefs.GetFloat(MouseSensitivityPreferenceKey, mouseSensitivity), false);

            if (target != null)
            {
                // Lấy góc quay ban đầu của camera
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                SnapToTarget();
            }
        }

        public void SetMouseSensitivity(float value, bool save = true)
        {
            mouseSensitivity = Mathf.Clamp(value, MinimumMouseSensitivity, MaximumMouseSensitivity);
            if (save)
            {
                PlayerPrefs.SetFloat(MouseSensitivityPreferenceKey, mouseSensitivity);
            }
        }

        private void LateUpdate()
        {
            // ═══ CHẾ ĐỘ FOCUS VÀO NPC (HỘI THOẠI) ═══
            if (isFocusingNPC)
            {
                transform.position = Vector3.Lerp(transform.position, focusPos, Time.deltaTime * customViewSmoothSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, focusRot, Time.deltaTime * customViewSmoothSpeed);
                return;
            }

            // ═══ CHẾ ĐỘ NHÌN THỨ 1 TỪ MẶT BÀN XE ĐẨY ═══
            if (isCartFirstPersonMode && cartFPCenter != null)
            {
                UpdateCartFirstPerson();
                return;
            }

            // ═══ CHẾ ĐỘ ORBIT QUANH XE ĐẨY (legacy) ═══
            if (isCartOrbitMode && cartOrbitCenter != null)
            {
                UpdateCartOrbit();
                return;
            }

            // ═══ CHẾ ĐỘ CUSTOM VIEW (cũ, giữ tương thích) ═══
            if (useCustomView && customViewTarget != null)
            {
                transform.position = Vector3.Lerp(transform.position, customViewTarget.position, Time.deltaTime * customViewSmoothSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, customViewTarget.rotation, Time.deltaTime * customViewSmoothSpeed);
                return;
            }

            if (target == null) return;

            // ═══ CHẾ ĐỘ THEO DÕI NHÂN VẬT (mặc định) ═══
            if (GameManager.Instance.IsPlaying && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Mouse.current != null)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    yaw += mouseDelta.x * mouseSensitivity * 0.1f;
                    pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                }
            }

            Vector3 targetLookAt = target.position + Vector3.up * yOffset;
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetPosition = targetLookAt + (targetRotation * shoulderOffset) - (targetRotation * Vector3.forward * distance);

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }

        /// <summary>
        /// Cập nhật camera orbit quanh xe đẩy.
        /// Chuột phải giữ để xoay camera, click trái để tương tác vật phẩm.
        /// </summary>
        private void UpdateCartOrbit()
        {
            // Xoay camera bằng chuột phải (giữ) hoặc chuột di chuyển
            if (Mouse.current != null)
            {
                bool isRightMouseHeld = Mouse.current.rightButton.isPressed;

                if (isRightMouseHeld)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    float sensitivityScale = mouseSensitivity / DefaultMouseSensitivity;
                    cartOrbitYaw += mouseDelta.x * cartOrbitSensitivity * sensitivityScale * 0.1f;
                    cartOrbitPitch -= mouseDelta.y * cartOrbitSensitivity * sensitivityScale * 0.1f;
                    cartOrbitPitch = Mathf.Clamp(cartOrbitPitch, cartOrbitMinPitch, cartOrbitMaxPitch);

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // Tính vị trí orbit
            Vector3 orbitCenter = cartOrbitCenter.position + Vector3.up * cartOrbitYOffset;
            Quaternion orbitRotation = Quaternion.Euler(cartOrbitPitch, cartOrbitYaw, 0f);
            Vector3 orbitPosition = orbitCenter - (orbitRotation * Vector3.forward * cartOrbitDistance);

            // Nội suy mượt mà
            transform.position = Vector3.Lerp(transform.position, orbitPosition, Time.deltaTime * cartOrbitSmoothSpeed);
            Quaternion lookRotation = Quaternion.LookRotation(orbitCenter - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * cartOrbitSmoothSpeed);

            // ═══ RAYCAST ĐỂ DETECT VẬT PHẨM ═══
            UpdateCartItemRaycast();
        }

        private void UpdateCartItemRaycast()
        {
            if (Mouse.current == null)
            {
                return;
            }

            bool leftClickPressed = Mouse.current.leftButton.wasPressedThisFrame;
            if (!leftClickPressed && Time.time < nextCartItemRaycastTime)
            {
                return;
            }

            nextCartItemRaycastTime = Time.time + cartItemRaycastInterval;

            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            if (cachedCamera == null)
            {
                return;
            }
            // Nếu không phải là chế độ góc nhìn thứ 1, và chuột đang bị khóa (đang giữ chuột phải xoay orbit camera)
            if (!isCartFirstPersonMode && Cursor.lockState == CursorLockMode.Locked)
            {
                if (currentHoveredItem != null)
                {
                    currentHoveredItem.SetHighlighted(false);
                    currentHoveredItem = null;
                }
                if (currentHoveredFoodItem != null)
                {
                    currentHoveredFoodItem.SetHighlighted(false);
                    currentHoveredFoodItem = null;
                }
                return;
            }

            Ray ray;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // Khi chuột bị khóa trong góc nhìn thứ 1, bắn raycast từ tâm màn hình
                ray = cachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                // Khi hiện chuột, bắn từ vị trí con trỏ chuột
                ray = cachedCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            }

            RaycastHit hit;
            if (TryGetCartItemHit(ray, out hit))
            {
                var cartItem = hit.collider.GetComponent<Interaction.CartItem>();
                if (cartItem == null)
                {
                    cartItem = hit.collider.GetComponentInParent<Interaction.CartItem>();
                }

                var foodItem = hit.collider.GetComponent<Interaction.Chapter2FoodItem>();
                if (foodItem == null)
                {
                    foodItem = hit.collider.GetComponentInParent<Interaction.Chapter2FoodItem>();
                }

                if (cartItem != null)
                {
                    if (currentHoveredFoodItem != null)
                    {
                        currentHoveredFoodItem.SetHighlighted(false);
                        currentHoveredFoodItem = null;
                    }

                    // Highlight item mới
                    if (currentHoveredItem != cartItem)
                    {
                        if (currentHoveredItem != null)
                            currentHoveredItem.SetHighlighted(false);

                        currentHoveredItem = cartItem;
                        currentHoveredItem.SetHighlighted(true);

                        // Hiển thị tên vật phẩm và số lượng hiện có
                        if (cachedPlayerStats == null)
                            cachedPlayerStats = FindAnyObjectByType<PlayerStats>();

                        var stats = cachedPlayerStats;
                        string quantityText = "";
                        if (stats != null)
                        {
                            if (cartItem.ItemType == Interaction.CartItem.CartItemType.TeaTin)
                            {
                                quantityText = $" (Hiện có: {stats.TeaSupply}g)";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.SugarcaneJuice ||
                                     cartItem.ItemType == Interaction.CartItem.CartItemType.SugarJar)
                            {
                                quantityText = $" (Hiện có: {stats.SugarSupply}g)";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.Coffee)
                            {
                                quantityText = $" (Hiện có: {stats.CoffeeSupply}g)";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.IceCooler)
                            {
                                quantityText = $" (Đá: {Mathf.RoundToInt(stats.IceLevel)}%)";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.WaterCup)
                            {
                                quantityText = $" (Hiện có: {stats.CupSupply})";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.WaterKettle)
                            {
                                string boiledStatus = Interaction.CartItem.IsWaterBoiled ? "Sôi" : "Nguội";
                                quantityText = $" (Nước ấm: {Interaction.CartItem.KettleWater:F1}L / 1.2L - {boiledStatus})";
                            }
                            else if (cartItem.ItemType == Interaction.CartItem.CartItemType.WaterBottle)
                            {
                                quantityText = $" (Bình nước: {Interaction.CartItem.BottleWater:F1}L / 30L)";
                            }
                        }
                        EventManager.TriggerInteractionPromptShow($"{cartItem.ItemName}{quantityText}");
                    }

                    // Click trái để tương tác
                    if (leftClickPressed)
                    {
                        if (cachedPlayer == null)
                        {
                            cachedPlayer = FindAnyObjectByType<PlayerController>();
                        }

                        var player = cachedPlayer;
                        if (player != null)
                        {
                            cartItem.OnItemClicked(player);
                        }
                    }
                }
                else if (foodItem != null)
                {
                    if (currentHoveredItem != null)
                    {
                        currentHoveredItem.SetHighlighted(false);
                        currentHoveredItem = null;
                    }

                    if (currentHoveredFoodItem != foodItem)
                    {
                        if (currentHoveredFoodItem != null)
                            currentHoveredFoodItem.SetHighlighted(false);

                        currentHoveredFoodItem = foodItem;
                        currentHoveredFoodItem.SetHighlighted(true);
                        EventManager.TriggerInteractionPromptShow(foodItem.ItemName);
                    }

                    if (leftClickPressed)
                    {
                        if (cachedPlayer == null)
                        {
                            cachedPlayer = FindAnyObjectByType<PlayerController>();
                        }

                        var player = cachedPlayer;
                        if (player != null)
                        {
                            foodItem.OnItemClicked(player);
                        }
                    }
                }
                else
                {
                    // Không hover vật phẩm nào
                    if (currentHoveredItem != null)
                    {
                        currentHoveredItem.SetHighlighted(false);
                        currentHoveredItem = null;

                        // Khôi phục prompt mặc định của xe đẩy
                        var cart = GetCachedTeaCart();
                        if (cart != null)
                            EventManager.TriggerInteractionPromptShow(cart.PromptText);
                        else
                            EventManager.TriggerInteractionPromptHide();
                    }
                    if (currentHoveredFoodItem != null)
                    {
                        currentHoveredFoodItem.SetHighlighted(false);
                        currentHoveredFoodItem = null;

                        var cart = GetCachedTeaCart();
                        if (cart != null)
                            EventManager.TriggerInteractionPromptShow(cart.PromptText);
                        else
                            EventManager.TriggerInteractionPromptHide();
                    }
                }
            }
            else
            {
                if (currentHoveredItem != null)
                {
                    currentHoveredItem.SetHighlighted(false);
                    currentHoveredItem = null;

                    var cart = GetCachedTeaCart();
                    if (cart != null)
                        EventManager.TriggerInteractionPromptShow(cart.PromptText);
                    else
                        EventManager.TriggerInteractionPromptHide();
                }
                if (currentHoveredFoodItem != null)
                {
                    currentHoveredFoodItem.SetHighlighted(false);
                    currentHoveredFoodItem = null;

                    var cart = GetCachedTeaCart();
                    if (cart != null)
                        EventManager.TriggerInteractionPromptShow(cart.PromptText);
                    else
                        EventManager.TriggerInteractionPromptHide();
                }
            }
        }

        // ═══════════════════════════════════════════
        // CART FIRST PERSON PUBLIC API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bật chế độ góc nhìn thứ 1 từ mặt bàn xe đẩy.
        /// Camera đứng phía sau mặt bàn, nhìn thẳng theo hướng xe.
        /// </summary>
        private Interaction.TeaCart GetCachedTeaCart()
        {
            if (cachedTeaCart == null)
                cachedTeaCart = FindAnyObjectByType<Interaction.TeaCart>();

            return cachedTeaCart;
        }

        private bool TryGetCartItemHit(Ray ray, out RaycastHit bestHit)
        {
            bestHit = default;
            bool found = false;
            float bestDistance = float.MaxValue;

            int hitCount = Physics.RaycastNonAlloc(ray, cartItemRaycastBuffer, 15f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit rayHit = cartItemRaycastBuffer[i];
                Collider hitCollider = rayHit.collider;
                if (hitCollider == null) continue;

                bool isCartItem = hitCollider.GetComponent<Interaction.CartItem>() != null ||
                                  hitCollider.GetComponentInParent<Interaction.CartItem>() != null ||
                                  hitCollider.GetComponent<Interaction.Chapter2FoodItem>() != null ||
                                  hitCollider.GetComponentInParent<Interaction.Chapter2FoodItem>() != null;

                if (!isCartItem || rayHit.distance >= bestDistance) continue;

                bestHit = rayHit;
                bestDistance = rayHit.distance;
                found = true;
            }

            return found;
        }

        public void EnableCartFirstPerson(Transform cartCenter)
        {
            isCartFirstPersonMode = true;
            cartFPCenter = cartCenter;

            RecalcCartFPPose();
            SetCartFirstPersonAnglesFromRotation(cartFPTargetRot);

            transform.position = cartFPTargetPos;
            transform.rotation = cartFPTargetRot;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetCartFirstPersonAnglesFromRotation(Quaternion targetRotation)
        {
            Vector3 camForward = targetRotation * Vector3.forward;
            if (camForward.sqrMagnitude < 0.001f && cartFPCenter != null)
            {
                camForward = cartFPCenter.forward;
            }

            if (camForward.sqrMagnitude < 0.001f)
            {
                camForward = transform.forward;
            }

            fpYaw = Mathf.Atan2(camForward.x, camForward.z) * Mathf.Rad2Deg;
            float horizontalDist = new Vector2(camForward.x, camForward.z).magnitude;
            fpPitch = -Mathf.Atan2(camForward.y, horizontalDist) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Tắt chế độ góc nhìn thứ 1 từ mặt bàn, trở về theo dõi nhân vật.
        /// </summary>
        public void DisableCartFirstPerson()
        {
            isCartFirstPersonMode = false;
            cartFPCenter = null;

            // Cleanup highlight
            if (currentHoveredItem != null)
            {
                currentHoveredItem.SetHighlighted(false);
                currentHoveredItem = null;
            }
            if (currentHoveredFoodItem != null)
            {
                currentHoveredFoodItem.SetHighlighted(false);
                currentHoveredFoodItem = null;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RecalcCartFPPose()
        {
            if (cartFPCenter == null) return;

            // Nếu đây là điểm camera góc nhìn thứ nhất (được gán từ Scene), lấy trực tiếp vị trí/góc quay đã thiết kế
            if (IsDesignedCartViewPoint(cartFPCenter))
            {
                cartFPTargetPos = cartFPCenter.position;
                cartFPTargetRot = cartFPCenter.rotation;
            }
            else
            {
                // Vị trí dự phòng: trên mặt bàn + lùi ra phía sau theo forward của xe
                Vector3 backDir = -cartFPCenter.forward;
                cartFPTargetPos = cartFPCenter.position
                                  + Vector3.up * cartFPYOffset
                                  + backDir * cartFPBackOffset;

                // Nhìn thẳng theo hướng trước của xe (từ phía sau mặt bàn nhìn ra)
                cartFPTargetRot = Quaternion.LookRotation(cartFPCenter.forward, Vector3.up);
            }
        }

        private void UpdateCartFirstPerson()
        {
            // Đọc di chuyển chuột để xoay góc nhìn tự do trong góc nhìn thứ nhất
            if (GameManager.Instance.IsPlaying && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Mouse.current != null)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    fpYaw += mouseDelta.x * mouseSensitivity * 0.1f;
                    fpPitch -= mouseDelta.y * mouseSensitivity * 0.1f;

                    // Tính yaw ngang thực tế của xe đẩy từ hướng forward dọc xe
                    Vector3 cartForward;
                    if (IsDesignedCartViewPoint(cartFPCenter))
                    {
                        cartForward = cartFPCenter.parent != null 
                            ? (cartFPCenter.parent.position - cartFPCenter.position)
                            : cartFPCenter.forward;
                    }
                    else
                    {
                        cartForward = cartFPCenter.forward;
                    }
                    cartForward.y = 0f;
                    cartForward.Normalize();

                    float cartYaw = Mathf.Atan2(cartForward.x, cartForward.z) * Mathf.Rad2Deg;
                    float deltaYaw = Mathf.DeltaAngle(cartYaw, fpYaw);
                    deltaYaw = Mathf.Clamp(deltaYaw, -85f, 85f); // Quay trái/phải tối đa 85 độ
                    fpYaw = cartYaw + deltaYaw;

                    // Giới hạn góc quay dọc (ngước lên/cúi xuống)
                    fpPitch = Mathf.Clamp(fpPitch, -30f, 60f);
                }
            }

            // Vị trí camera cố định tại viewpoint
            transform.position = Vector3.Lerp(transform.position, cartFPTargetPos, Time.deltaTime * cartFPSmoothSpeed);

            // Quay camera theo góc nhìn tự do của người chơi
            Quaternion targetRot = Quaternion.Euler(fpPitch, fpYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * cartFPSmoothSpeed);

            // Raycast để highlight / click vật phẩm trên mặt bàn
            UpdateCartItemRaycast();
        }

        private static bool IsDesignedCartViewPoint(Transform viewPoint)
        {
            if (viewPoint == null) return false;
            return viewPoint.name == "FirstPersonCameraPoint" || viewPoint.name == "RuntimeCartCameraPoint";
        }

        // ═══════════════════════════════════════════
        // CART ORBIT PUBLIC API (legacy)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bật chế độ quay quanh xe đẩy (góc nhìn thứ 3) — legacy.
        /// </summary>
        public void EnableCartOrbit(Transform cartCenter)
        {
            isCartOrbitMode = true;
            cartOrbitCenter = cartCenter;

            // Tính góc ban đầu dựa trên vị trí hiện tại
            Vector3 dirToCart = cartCenter.position - transform.position;
            cartOrbitYaw = Mathf.Atan2(dirToCart.x, dirToCart.z) * Mathf.Rad2Deg;
            cartOrbitPitch = 30f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Tắt chế độ quay quanh xe đẩy, trở về theo dõi nhân vật.
        /// </summary>
        public void DisableCartOrbit()
        {
            isCartOrbitMode = false;
            cartOrbitCenter = null;

            // Cleanup highlight
            if (currentHoveredItem != null)
            {
                currentHoveredItem.SetHighlighted(false);
                currentHoveredItem = null;
            }
            if (currentHoveredFoodItem != null)
            {
                currentHoveredFoodItem.SetHighlighted(false);
                currentHoveredFoodItem = null;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ═══════════════════════════════════════════
        // CUSTOM VIEW (giữ tương thích ngược)
        // ═══════════════════════════════════════════

        public void EnableCustomView(Transform viewPoint)
        {
            customViewTarget = viewPoint;
            useCustomView = true;
        }

        public void DisableCustomView()
        {
            useCustomView = false;
            customViewTarget = null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                SnapToTarget();
            }
        }

        // Tương thích ngược với các lời gọi cũ
        public void SetBounds(float min, float max) { }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 targetLookAt = target.position + Vector3.up * yOffset;
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = targetLookAt + (targetRotation * shoulderOffset) - (targetRotation * Vector3.forward * distance);
            transform.rotation = targetRotation;
            currentVelocity = Vector3.zero;
        }

        // ═══════════════════════════════════════════
        // NPC FOCUS (DIALOGUE)
        // ═══════════════════════════════════════════

        public void FocusOnNPC(Transform npc, Transform player)
        {
            isFocusingNPC = true;
            // Tính toán vị trí góc nhìn thứ nhất (từ mắt người chơi nhìn về NPC)
            Vector3 dirToNPC = (npc.position - player.position).normalized;
            dirToNPC.y = 0;
            if (dirToNPC.sqrMagnitude < 0.01f) dirToNPC = player.forward;
            dirToNPC.Normalize();

            // Góc nhìn thứ nhất: Đặt camera ngay tại vị trí mắt của người chơi (cao 1.6m), nhích lên trước 0.2m để không bị vướng model
            focusPos = player.position + Vector3.up * 1.6f + dirToNPC * 0.2f;
            
            // Nhìn thẳng vào mặt NPC (cao khoảng 1.5m)
            focusRot = Quaternion.LookRotation(npc.position + Vector3.up * 1.5f - focusPos);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResetFocus(Transform playerTransform)
        {
            isFocusingNPC = false;
            target = playerTransform;
            
            // Xoay hướng nhìn về phía người chơi đang đứng
            yaw = playerTransform.eulerAngles.y;
            pitch = 12f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
