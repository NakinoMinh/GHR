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
        private Interaction.CustomerSeat currentSeat; // Ghế đang ngồi

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
            if (!canMove || !GameManager.Instance.IsPlaying)
            {
                moveDirection = Vector3.zero;
                horizontalInput = 0f;
                verticalInput = 0f;
                return;
            }

            // Nếu đang ngồi, chặn mọi di chuyển bằng WASD, chỉ cho phép nhấn E để đứng lên
            if (currentState == PlayerState.Sitting)
            {
                moveDirection = Vector3.zero;
                horizontalInput = 0f;
                verticalInput = 0f;
                isRunning = false;

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Đứng dậy trực tiếp từ ghế đang ngồi
                    if (currentSeat != null)
                    {
                        currentSeat.OnPlayerStandUp(this);
                        currentSeat = null;
                    }
                    else
                    {
                        SetState(PlayerState.Idle);
                    }
                    EventManager.TriggerInteractionPromptHide();
                }
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
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteract();
            }

            // Cập nhật trạng thái
            UpdatePlayerState();
        }

        private void FixedUpdate()
        {
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
            // Dùng QueryTriggerInteraction.Collide để detect cả BoxCollider có isTrigger = true (như ghế, xe đẩy)
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, ~0, QueryTriggerInteraction.Collide);

            Interaction.Interactable closest = null;
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
                // Nếu tương tác với ghế, lưu lại tham chiếu để đứng dậy sau
                var seat = nearestInteractable as Interaction.CustomerSeat;
                if (seat != null && currentState != PlayerState.Sitting)
                {
                    currentSeat = seat;
                }
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
            DisableMovement();
        }

        private void HandleDialogueEnded()
        {
            EnableMovement();
        }

        public void UpdateCursorState()
        {
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
