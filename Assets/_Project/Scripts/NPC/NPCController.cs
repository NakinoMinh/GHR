using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Interaction;
using System.Collections;
using TMPro;

namespace GanhHangRong.NPC
{
    /// <summary>
    /// AI điều khiển một NPC khách hàng.
    /// Dùng State Machine đơn giản để mô phỏng hành vi.
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float stopDistance = 0.1f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float seatApproachDistance = 0.95f;
        [SerializeField] private float seatEntryDistance = 0.18f;

        private NPCProfile profile;
        private NPCState currentState = NPCState.Spawning;
        private CustomerSeat targetSeat;
        private Transform exitPoint;
        private float startY;
        
        private float waitTimer = 0f;
        private float maxWaitTime;
        private float drinkTimer = 0f;
        private float drinkDuration;
        private bool isServed = false;
        
        private GameObject speechBubble;
        private TextMeshPro bubbleText;

        private int orderedDrink = 0; // 0: Trà đá, 1: Cà phê
        private NPCInteractable interactable;
        private Player.PlayerController interactingPlayer;
        private Transform visualRoot;
        private Transform visualModel;
        private Animator visualAnimator;
        private bool visualReferencesCached;
        private Vector3 visualModelOriginalLocalPosition;
        private Quaternion visualModelOriginalLocalRotation;
        private Vector3 visualModelOriginalLocalScale = Vector3.one;
        private const float VisualFeetGroundClearance = 0.005f;
        private const float SittingVisualDrop = 0.55f;

        public NPCState CurrentState => currentState;
        public CustomerSeat TargetSeat => targetSeat;
        public int OrderedDrinkId => orderedDrink;
        public string OrderedDrinkName => GetDrinkName(orderedDrink);

        private void Awake()
        {
            // Create speech bubble
            speechBubble = new GameObject("SpeechBubble");
            speechBubble.transform.SetParent(transform);
            speechBubble.transform.localPosition = new Vector3(0, 2.2f, 0);
            
            bubbleText = speechBubble.AddComponent<TextMeshPro>();
            bubbleText.fontSize = 4;
            bubbleText.alignment = TextAlignmentOptions.Center;
            bubbleText.color = Color.black;
            
            // Thêm background trắng cho text (đơn giản bằng SpriteRenderer)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(speechBubble.transform);
            bgObj.transform.localPosition = new Vector3(0, 0, 0.1f);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            
            // Generate a simple white texture for the background
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            bgSr.sprite = sprite;
            bgObj.transform.localScale = new Vector3(2f, 1f, 1f);

            speechBubble.SetActive(false);

            interactable = gameObject.AddComponent<NPCInteractable>();

            // Thêm Collider để PlayerController có thể Raycast/OverlapSphere detect được NPC
            var col = gameObject.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.4f;
                col.center = new Vector3(0f, 0.9f, 0f);
                col.isTrigger = true; // Trigger để không chặn vật lý
            }
        }

        private void OnEnable()
        {
            EventManager.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            EventManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void OnDestroy()
        {
            if (targetSeat != null && targetSeat.IsOccupied)
            {
                targetSeat.FreeSeat();
            }
        }

        public void Initialize(NPCProfile profile, CustomerSeat seat, Transform exit, float walkSpd)
        {
            if (profile == null || seat == null || exit == null)
            {
                Debug.LogWarning("[NPCController] Missing profile, seat, or exit point. Cancelled NPC spawn.");
                if (seat != null) seat.FreeSeat();
                Destroy(gameObject);
                return;
            }

            this.profile = profile;
            this.targetSeat = seat;
            this.exitPoint = exit;
            this.walkSpeed = walkSpd;
            SnapToGround();
            this.startY = transform.position.y;
            
            this.maxWaitTime = Random.Range(profile.minPatience, profile.maxPatience);
            this.drinkDuration = Random.Range(profile.minDrinkTime, profile.maxDrinkTime);
            
            // Gọi factory tạo model
            var factory = FindAnyObjectByType<NPCVisualFactory>();
            if (factory != null)
            {
                factory.CreateNPCVisual(profile.npcType, transform);
            }
                
            ChangeState(NPCState.WalkingIn);
        }

        private void SnapToGround()
        {
            Vector3 origin = transform.position + Vector3.up * 2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y > 0.45f)
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                }
            }
        }

        private void Update()
        {
            if (!GameManager.HasInstance || GameManager.Instance.IsPaused) return;
            if (profile == null || targetSeat == null || exitPoint == null) return;

            switch (currentState)
            {
                case NPCState.WalkingIn:
                    MoveTowards(GetSeatApproachPosition(), NPCState.Approaching);
                    break;

                case NPCState.Approaching:
                    MoveTowards(GetSeatEntryPosition(), NPCState.SittingDown);
                    break;
                    
                case NPCState.SittingDown:
                    // Animation ngồi, sau đó chuyển sang Order
                    transform.position = targetSeat.transform.position; // Snap vào ghế
                    
                    // Chỉ lấy góc xoay trục Y (Yaw) để NPC không bị ngã/nằm nếú ghế bị xoay trục X/Z
                    Vector3 seatEuler = targetSeat.transform.rotation.eulerAngles;
                    transform.rotation = Quaternion.Euler(0, seatEuler.y, 0); 
                    
                    targetSeat.OccupySeat(this);
                    ChangeState(NPCState.Ordering);
                    if (interactable != null) interactable.SetInteractable(true);
                    // Hiển thị bong bóng "..." để gợi ý
                    ShowSpeechBubble("...");
                    break;
                    
                case NPCState.Ordering:
                    // Đợi người chơi nhấn F (NPCInteractable sẽ gọi StartOrderingDialogue)
                    break;
                    
                case NPCState.Waiting:
                    waitTimer += Time.deltaTime;
                    
                    // Hiện cảnh báo nếu chờ quá lâu
                    if (!isServed && waitTimer > maxWaitTime * 0.7f)
                    {
                        ShowSpeechBubble("Nhanh lên!\n(!!)", Color.red);
                    }

                    if (isServed)
                    {
                        HideSpeechBubble();
                        ChangeState(NPCState.Drinking);
                        EventManager.TriggerCustomerServed(profile.npcType);
                    }
                    else if (waitTimer >= maxWaitTime)
                    {
                        ChangeState(NPCState.LeavingSad);
                        EventManager.TriggerCustomerOrderCleared();
                        EventManager.TriggerCustomerLeftSad(profile.npcType);
                        if (targetSeat != null) targetSeat.FreeSeat();
                    }
                    break;
                    
                case NPCState.Drinking:
                    drinkTimer += Time.deltaTime;
                    if (drinkTimer >= drinkDuration)
                    {
                        ChangeState(NPCState.Paying);
                    }
                    break;
                    
                case NPCState.Paying:
                    PayForDrink();
                    ChangeState(NPCState.LeavingHappy);
                    EventManager.TriggerCustomerLeftHappy(profile.npcType);
                    if (targetSeat != null) targetSeat.FreeSeat();
                    break;
                    
                case NPCState.LeavingHappy:
                    ShowSpeechBubble("Ngon!", Color.blue);
                    transform.position = new Vector3(transform.position.x, startY, transform.position.z); // Trả lại độ cao mặt đất
                    ChangeState(NPCState.LeavingSeat);
                    break;
                    
                case NPCState.LeavingSad:
                    ShowSpeechBubble("Tệ quá!", Color.red);
                    transform.position = new Vector3(transform.position.x, startY, transform.position.z); // Trả lại độ cao mặt đất
                    ChangeState(NPCState.LeavingSeat);
                    break;
                    
                case NPCState.LeavingSeat:
                    MoveTowards(GetSeatApproachPosition(), NPCState.WalkingOut);
                    break;

                case NPCState.WalkingOut:
                    MoveTowards(exitPoint.position, NPCState.Spawning /* Destroy */);
                    if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(exitPoint.position.x, 0, exitPoint.position.z)) <= stopDistance)
                    {
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        private void LateUpdate()
        {
            ApplyVisualPoseForState();
        }

        private void ApplyVisualPoseForState()
        {
            CacheVisualReferences();
            bool isWalking = currentState == NPCState.WalkingIn ||
                             currentState == NPCState.Approaching ||
                             currentState == NPCState.LeavingSeat ||
                             currentState == NPCState.WalkingOut;
            bool isSitting = currentState == NPCState.SittingDown ||
                             currentState == NPCState.Ordering ||
                             currentState == NPCState.Waiting ||
                             currentState == NPCState.Drinking ||
                             currentState == NPCState.Paying;

            if (visualAnimator != null && isWalking)
            {
                visualAnimator.enabled = true;
                visualAnimator.speed = 0.35f;
                SetAnimatorStateParameter(0);
            }
            else if (visualAnimator != null && isSitting)
            {
                visualAnimator.enabled = true;
                visualAnimator.speed = 1f;
                SetAnimatorStateParameter(1);
            }

            RestoreVisualModelRoot();
            if (isWalking)
            {
                PinVisualModelFeetToGround();
            }
            else if (isSitting)
            {
                ApplyLowSittingPose();
            }
            else
            {
                PinVisualModelFeetToGround();
            }
        }

        private void CacheVisualReferences()
        {
            if (visualReferencesCached && visualRoot != null) return;

            visualRoot = transform.Find("VisualRoot");
            visualModel = visualRoot != null ? visualRoot.Find("NPCModel") : null;
            visualAnimator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : null;
            if (visualModel != null)
            {
                visualModelOriginalLocalPosition = visualModel.localPosition;
                visualModelOriginalLocalRotation = visualModel.localRotation;
                visualModelOriginalLocalScale = visualModel.localScale;
            }
            visualReferencesCached = true;
        }

        private void RestoreVisualModelRoot()
        {
            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
            }

            if (visualModel == null) return;

            visualModel.localPosition = visualModelOriginalLocalPosition;
            visualModel.localRotation = visualModelOriginalLocalRotation;
            visualModel.localScale = visualModelOriginalLocalScale;
        }

        private void PinVisualModelFeetToGround()
        {
            if (visualModel == null) return;

            Renderer[] renderers = visualModel.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float targetBottomY = transform.position.y + VisualFeetGroundClearance;
            float deltaWorldY = targetBottomY - bounds.min.y;
            Transform localSpace = visualModel.parent != null ? visualModel.parent : transform;
            Vector3 localDelta = localSpace.InverseTransformVector(Vector3.up * deltaWorldY);
            visualModel.localPosition += localDelta;
        }

        private void ApplyLowSittingPose()
        {
            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.down * SittingVisualDrop;
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void SetAnimatorStateParameter(int value)
        {
            if (visualAnimator == null || visualAnimator.runtimeAnimatorController == null) return;

            foreach (var parameter in visualAnimator.parameters)
            {
                if (parameter.name == "State")
                {
                    visualAnimator.SetInteger("State", value);
                    return;
                }
            }
        }

        private void MoveTowards(Vector3 target, NPCState nextState)
        {
            Vector3 targetPos = new Vector3(target.x, transform.position.y, target.z);
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            // Xoay mặt về hướng đang di chuyển trước khi dịch chuyển (giống player)
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * 360f * Time.deltaTime);
            }

            float step = walkSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
            SnapToGround();

            if (Vector3.Distance(transform.position, targetPos) <= stopDistance)
            {
                if (nextState != NPCState.Spawning) // Mượn Spawning làm cờ Destroy
                    ChangeState(nextState);
            }
        }

        private Vector3 GetSeatApproachPosition()
        {
            if (targetSeat == null) return transform.position;

            Vector3 seatForward = GetSeatYawForward(targetSeat.transform);
            Vector3 approach = targetSeat.transform.position - seatForward * seatApproachDistance;
            return new Vector3(approach.x, transform.position.y, approach.z);
        }

        private Vector3 GetSeatEntryPosition()
        {
            if (targetSeat == null) return transform.position;

            Vector3 seatForward = GetSeatYawForward(targetSeat.transform);
            Vector3 entry = targetSeat.transform.position - seatForward * seatEntryDistance;
            return new Vector3(entry.x, transform.position.y, entry.z);
        }

        private static Vector3 GetSeatYawForward(Transform seatTransform)
        {
            if (seatTransform == null) return Vector3.forward;

            Vector3 forward = Quaternion.Euler(0f, seatTransform.eulerAngles.y, 0f) * Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private void ChangeState(NPCState newState)
        {
            currentState = newState;
            ApplyVisualPoseForState();
        }

        public void ServeDrink()
        {
            if (currentState == NPCState.Waiting && !isServed)
            {
                isServed = true;
                EventManager.TriggerCustomerOrderCleared();
            }
        }

        private void PayForDrink()
        {
            int basePrice = ChapterOrderCatalog.GetOrderPrice(orderedDrink);
            int total = basePrice;
            
            // Tính tip
            if (Random.value <= profile.tipChance)
            {
                total += 2000; // Tip 2k
            }
            
            // Tìm PlayerStats để cộng tiền
            var playerStats = FindAnyObjectByType<Player.PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddMoney(total);
                playerStats.RecordCustomerServed();
            }
        }

        public void StartOrderingDialogue(Player.PlayerController player)
        {
            interactingPlayer = player;
            HideSpeechBubble();

            // Đổi góc camera sang NPC
            var cam = FindAnyObjectByType<Player.CinematicCamera>();
            if (cam != null) cam.FocusOnNPC(transform, player.transform);

            // Random món theo chapter hiện tại.
            int chapter = GameManager.HasInstance ? GameManager.Instance.CurrentChapter : 1;
            orderedDrink = ChapterOrderCatalog.GetRandomOrderId(chapter);
            string drinkName = GetDrinkName(orderedDrink);
            string text = $"Cho tui một ly {drinkName} nha!";
            if (ChapterOrderCatalog.IsChapter2Order(orderedDrink))
            {
                text = $"Cho tui một phần {drinkName} nha!";
            }

            // Kích hoạt thoại
            Narrative.DialogueManager.Instance.StartSingleDialogue(profile.npcType.ToString(), text);
        }

        private void HandleDialogueEnded()
        {
            if (interactingPlayer != null)
            {
                // Reset camera
                var cam = FindAnyObjectByType<Player.CinematicCamera>();
                if (cam != null) cam.ResetFocus(interactingPlayer.transform);

                interactingPlayer = null;

                // Chuyển state
                EventManager.TriggerCustomerArrived(profile.npcType);
                ChangeState(NPCState.Waiting);
                string drinkName = GetDrinkName(orderedDrink);
                EventManager.TriggerCustomerOrderPlaced(orderedDrink, drinkName);
                ShowSpeechBubble(drinkName);
            }
        }

        private static string GetDrinkName(int drinkId)
        {
            return ChapterOrderCatalog.GetOrderName(drinkId);
        }

        private void ShowSpeechBubble(string text, Color? textColor = null)
        {
            if (speechBubble != null)
            {
                speechBubble.SetActive(true);
                bubbleText.text = text;
                bubbleText.color = textColor ?? Color.black;
            }
        }

        private void HideSpeechBubble()
        {
            if (speechBubble != null)
                speechBubble.SetActive(false);
        }
    }
}
