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

        public NPCState CurrentState => currentState;
        public CustomerSeat TargetSeat => targetSeat;

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
        }

        private void OnEnable()
        {
            EventManager.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            EventManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        public void Initialize(NPCProfile profile, CustomerSeat seat, Transform exit, float walkSpd)
        {
            this.profile = profile;
            this.targetSeat = seat;
            this.exitPoint = exit;
            this.walkSpeed = walkSpd;
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

        private void Update()
        {
            if (GameManager.Instance.IsPaused) return;

            switch (currentState)
            {
                case NPCState.WalkingIn:
                    MoveTowards(targetSeat.transform.position, NPCState.SittingDown);
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
                    ChangeState(NPCState.WalkingOut);
                    break;
                    
                case NPCState.LeavingSad:
                    ShowSpeechBubble("Tệ quá!", Color.red);
                    transform.position = new Vector3(transform.position.x, startY, transform.position.z); // Trả lại độ cao mặt đất
                    ChangeState(NPCState.WalkingOut);
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

            if (Vector3.Distance(transform.position, targetPos) <= stopDistance)
            {
                if (nextState != NPCState.Spawning) // Mượn Spawning làm cờ Destroy
                    ChangeState(nextState);
            }
        }

        private void ChangeState(NPCState newState)
        {
            currentState = newState;
        }

        public void ServeDrink()
        {
            if (currentState == NPCState.Waiting && !isServed)
            {
                isServed = true;
            }
        }

        private void PayForDrink()
        {
            int basePrice = orderedDrink == 1 ? Constants.COFFEE_SELL_PRICE : Constants.TRA_DA_SELL_PRICE;
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

            // Random món
            orderedDrink = Random.value > 0.5f ? 1 : 0;
            string drinkName = orderedDrink == 1 ? "Cà Phê đá" : "Trà Đá";
            string text = $"Cho tui một ly {drinkName} nha!";

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
                string drinkName = orderedDrink == 1 ? "Cà Phê!" : "Trà Đá!";
                ShowSpeechBubble(drinkName);
            }
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
