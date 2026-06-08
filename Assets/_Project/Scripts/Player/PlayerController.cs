using UnityEngine;
using GanhHangRong.Core;
using UnityEngine.InputSystem;

namespace GanhHangRong.Player
{
    /// <summary>
    /// Điều khiển nhân vật chính — Nguyễn Hoàng Hôn.
    /// Di chuyển 3D WASD, tương tác, và quản lý trạng thái.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Di Chuyển 3D")]
        [SerializeField] private float walkSpeed = Constants.PLAYER_WALK_SPEED;
        [SerializeField] private float runSpeed = Constants.PLAYER_RUN_SPEED;
        [SerializeField] private float pushCartSpeed = Constants.PLAYER_PUSH_CART_SPEED;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Tương Tác")]
        [SerializeField] private float interactionRange = 1.5f;

        // Components
        private Rigidbody rb;
        private PlayerAnimator playerAnimator;
        private PlayerStats playerStats;

        // State
        private PlayerState currentState = PlayerState.Idle;
        private Vector3 moveDirection;
        private float horizontalInput;
        private float verticalInput;
        private bool canMove = true;
        private bool isPushingCart = false;
        private bool isRunning = false;
        private Interaction.Interactable nearestInteractable;

        public PlayerState CurrentState => currentState;
        public bool FacingRight => transform.forward.x >= 0;
        public float HorizontalInput => moveDirection.magnitude; // Tương thích ngược

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerAnimator = GetComponent<PlayerAnimator>();
            playerStats = GetComponent<PlayerStats>();

            // Cấu hình Rigidbody
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.freezeRotation = true;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void OnEnable()
        {
            EventManager.OnGamePhaseChanged += HandleGamePhaseChanged;
            EventManager.OnDialogueStarted += HandleDialogueStarted;
            EventManager.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            EventManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
            EventManager.OnDialogueStarted -= HandleDialogueStarted;
            EventManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void Start()
        {
            UpdateCursorState();
        }

        private void Update()
        {
            if (!GameManager.HasInstance) return;

            if (!GameManager.Instance.IsPlaying)
            {
                moveDirection = Vector3.zero;
                horizontalInput = 0f;
                verticalInput = 0f;
                return;
            }

            // Nếu đang tương tác với xe đẩy (góc nhìn thứ 1 từ mặt bàn)
            if (currentState == PlayerState.Interacting)
            {
                moveDirection = Vector3.zero;
                horizontalInput = 0f;
                verticalInput = 0f;
                isRunning = false;

                // Phục vụ khách hàng bằng phím Space
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    var cart = FindAnyObjectByType<Interaction.TeaCart>();
                    if (cart != null)
                    {
                        cart.ServeFromFirstPerson(this);
                    }
                }

                // Nhấn F để thoát góc nhìn xe đẩy
                if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                {
                    var cart = FindAnyObjectByType<Interaction.TeaCart>();
                    if (cart != null)
                    {
                        cart.Interact(this); // Sẽ gọi ExitCartInteraction
                    }
                }
                return;
            }

            // Nếu không thể di chuyển (ví dụ: đang thoại) thì chặn input đi lại
            if (!canMove)
            {
                moveDirection = Vector3.zero;
                horizontalInput = 0f;
                verticalInput = 0f;
                return;
            }

            // Input di chuyển
            horizontalInput = 0f;
            verticalInput = 0f;
            isRunning = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput = 1f;
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput = -1f;

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;

                isRunning = Keyboard.current.shiftKey.isPressed;
            }

            // Tính toán hướng di chuyển tương đối so với Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 camForward = mainCam.transform.forward;
                Vector3 camRight = mainCam.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;
            }
            else
            {
                moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
            }

            // Xoay nhân vật về hướng di chuyển
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }

            // Tương tác
            CheckInteraction();
            if (currentState != PlayerState.Interacting)
            {
                // Nhấn F để tương tác với vật thể gần nhất/đang trỏ vào
                if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                {
                    if (nearestInteractable != null)
                    {
                        nearestInteractable.Interact(this);
                        return;
                    }
                }
            }

            // Cập nhật trạng thái
            UpdatePlayerState();
        }

        private void FixedUpdate()
        {
            if (!GameManager.HasInstance) return;

            if (!canMove || !GameManager.Instance.IsPlaying)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }

            // Di chuyển
            float speed = isPushingCart ? pushCartSpeed : (isRunning ? runSpeed : walkSpeed);

            // Áp dụng penalty mệt mỏi
            if (playerStats != null)
            {
                float fatiguePenalty = Mathf.Lerp(1f, 1f - Constants.PLAYER_SPEED_FATIGUE_PENALTY,
                    playerStats.Fatigue / Constants.PLAYER_FATIGUE_MAX);
                speed *= fatiguePenalty;
            }

            Vector3 targetVelocity = moveDirection * speed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        private void UpdatePlayerState()
        {
            // Không override trạng thái Sitting — do logic ngồi/đứng tự quản lý
            if (currentState == PlayerState.Sitting) return;

            PlayerState newState;
            float movementMagnitude = moveDirection.magnitude;

            if (isPushingCart && movementMagnitude > 0.1f)
                newState = PlayerState.PushingCart;
            else if (movementMagnitude > 0.1f)
                newState = isRunning ? PlayerState.Running : PlayerState.Walking;
            else
                newState = PlayerState.Idle;

            if (newState != currentState)
            {
                currentState = newState;
                EventManager.TriggerPlayerStateChanged(currentState);
                if (playerAnimator != null)
                    playerAnimator.SetState(currentState);
            }
        }

        private void CheckInteraction()
        {
            Interaction.Interactable closest = null;

            // 1. Ưu tiên Raycast từ tâm camera (hỗ trợ trỏ tâm ngắm vào xe đẩy)
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                // Tầm quét rộng hơn một chút cho thoải mái khi xoay camera
                if (Physics.Raycast(ray, out hit, interactionRange + 2.5f, ~0, QueryTriggerInteraction.Collide))
                {
                    var interactable = hit.collider.GetComponent<Interaction.Interactable>();
                    if (interactable == null)
                    {
                        interactable = hit.collider.GetComponentInParent<Interaction.Interactable>();
                    }

                    if (interactable != null && interactable.CanInteract)
                    {
                        // Đảm bảo người chơi không đứng quá xa vật thể
                        float dist = Vector3.Distance(transform.position, interactable.transform.position);
                        if (dist <= interactionRange + 2.0f)
                        {
                            closest = interactable;
                        }
                    }
                }
            }

            // 2. Dự phòng: Quét OverlapSphere nếu không trỏ thẳng tâm camera
            if (closest == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, ~0, QueryTriggerInteraction.Collide);
                float closestDist = float.MaxValue;

                foreach (var hit in hits)
                {
                    var interactable = hit.GetComponent<Interaction.Interactable>();
                    if (interactable != null && interactable.CanInteract)
                    {
                        float dist = Vector3.Distance(transform.position, hit.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = interactable;
                        }
                    }
                }
            }

            if (closest != nearestInteractable)
            {
                nearestInteractable = closest;
                if (nearestInteractable != null)
                    EventManager.TriggerInteractionPromptShow(nearestInteractable.PromptText);
                else
                    EventManager.TriggerInteractionPromptHide();
            }
        }

        private void TryInteract()
        {
            if (nearestInteractable != null && nearestInteractable.CanInteract)
            {
                nearestInteractable.Interact(this);
            }
        }

        public void SetPushingCart(bool pushing)
        {
            isPushingCart = pushing;
        }

        public void SetState(PlayerState state)
        {
            currentState = state;
            EventManager.TriggerPlayerStateChanged(state);
            if (playerAnimator != null)
                playerAnimator.SetState(state);
        }

        public void EnableMovement()
        {
            canMove = true;
            UpdateCursorState();
        }

        public void DisableMovement()
        {
            canMove = false;
            UpdateCursorState();
        }

        private void HandleGamePhaseChanged(GamePhase phase)
        {
            canMove = phase == GamePhase.Playing;
            UpdateCursorState();
        }

        private void HandleDialogueStarted()
        {
            if (currentState == PlayerState.Interacting) return;
            DisableMovement();
        }

        private void HandleDialogueEnded()
        {
            if (currentState == PlayerState.Interacting) return;
            EnableMovement();
        }

        public void UpdateCursorState()
        {
            if (!GameManager.HasInstance) return;

            if (canMove && GameManager.Instance.IsPlaying && !GameManager.Instance.IsPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
