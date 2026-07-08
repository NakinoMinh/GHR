using System.Collections.Generic;
using UnityEngine;
using GanhHangRong.Core;
using GanhHangRong.Economy;
using GanhHangRong.Player;

namespace GanhHangRong.Interaction
{
    public class FurniturePlacementManager : MonoBehaviour
    {
        public static FurniturePlacementManager Instance { get; private set; }

        private bool isPlacementModeActive = false;
        private string currentItemType = "";
        private GameObject previewObject;
        private float currentRotationY = 0f;
        private Material greenOutlineMaterial;

        // For relocating existing furniture
        private CustomerSeat pickedUpSeat = null;
        private Transform pickedUpTable = null;

        public bool IsPlacementModeActive => isPlacementModeActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("FurniturePlacementManager_Runtime");
                obj.AddComponent<FurniturePlacementManager>();
                DontDestroyOnLoad(obj);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateGreenMaterial();
        }

        private void CreateGreenMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            greenOutlineMaterial = new Material(shader);
            greenOutlineMaterial.color = new Color(0.1f, 0.95f, 0.2f, 0.75f);
            greenOutlineMaterial.SetFloat("_Mode", 3);
            greenOutlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            greenOutlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            greenOutlineMaterial.SetInt("_ZWrite", 0);
            greenOutlineMaterial.renderQueue = 3000;
            greenOutlineMaterial.EnableKeyword("_EMISSION");
            greenOutlineMaterial.SetColor("_EmissionColor", new Color(0.15f, 1f, 0.35f, 1f) * 1.5f);
        }

        private void Update()
        {
            if (!isPlacementModeActive)
            {
                // Check if aiming at existing furniture to relocate (Right click or Key M)
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.M))
                {
                    TryPickUpExistingFurniture();
                }
                return;
            }

            if (previewObject == null)
            {
                ExitPlacementMode();
                return;
            }

            // Raycast from camera to floor
            Vector3 targetPos = GetRaycastFloorPosition();
            previewObject.transform.position = Vector3.Lerp(previewObject.transform.position, targetPos, Time.deltaTime * 20f);
            previewObject.transform.rotation = Quaternion.Slerp(previewObject.transform.rotation, Quaternion.Euler(0f, currentRotationY, 0f), Time.deltaTime * 20f);

            // Rotate with R or Mouse Wheel
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentRotationY += 45f;
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã xoay bàn ghế 45 độ.");
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentRotationY += scroll > 0 ? 45f : -45f;
            }

            // Confirm placement with Left Click or F or Space
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
            {
                PlaceCurrentFurniture();
            }

            // Cancel with ESC or X or Right Click
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }

        private Vector3 GetRaycastFloorPosition()
        {
            Transform player = FindPlayerTransform();
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)) : new Ray(player.position + Vector3.up * 1.5f, player.forward);

            // Ignore player and triggers
            int mask = ~0;
            if (Physics.Raycast(ray, out RaycastHit hit, 15f, mask, QueryTriggerInteraction.Ignore))
            {
                // Check if hit point is reasonable ground level (y below 1.5f)
                if (hit.point.y < 1.5f)
                {
                    return hit.point;
                }
            }

            // Intersect with default floor plane at Y = 0.05f
            Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, 0.05f, 0f));
            if (floorPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            Vector3 fallback = player != null ? player.position + player.forward * 3f : Vector3.zero;
            fallback.y = 0.05f;
            return fallback;
        }

        public void EnterPlacementMode(string itemType)
        {
            if (isPlacementModeActive)
            {
                ExitPlacementMode();
            }

            if (greenOutlineMaterial == null) CreateGreenMaterial();

            isPlacementModeActive = true;
            currentItemType = itemType;
            currentRotationY = FindPlayerTransform() != null ? FindPlayerTransform().eulerAngles.y : 0f;

            BuildPreviewObject();

            EventManager.TriggerDialogueLine("Hoàng Hôn", $"[CHẾ ĐỘ SẮP XẾP BÀN GHẾ]: Chuột trái/F để ĐẶT | Phím R/Cuộn chuột để XOAY | ESC/X để HỦY.");
            Debug.Log($"[FurniturePlacementManager] Đã vào chế độ sắp xếp: {itemType}");
        }

        private void BuildPreviewObject()
        {
            if (previewObject != null) Destroy(previewObject);

            previewObject = new GameObject("Furniture_Preview_GreenOutline");
            previewObject.transform.position = GetRaycastFloorPosition();

            if (currentItemType == "ban_doi")
            {
                CreatePreviewTable(previewObject.transform, Vector3.zero, new Vector3(1.6f, 0.7f, 0.9f));
                CreatePreviewChair(previewObject.transform, new Vector3(-0.9f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                CreatePreviewChair(previewObject.transform, new Vector3(0.9f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));
            }
            else if (currentItemType == "ban_bon")
            {
                CreatePreviewTable(previewObject.transform, Vector3.zero, new Vector3(1.8f, 0.7f, 1.4f));
                CreatePreviewChair(previewObject.transform, new Vector3(-0.9f, 0f, -0.5f), Quaternion.Euler(0f, 90f, 0f));
                CreatePreviewChair(previewObject.transform, new Vector3(-0.9f, 0f, 0.5f), Quaternion.Euler(0f, 90f, 0f));
                CreatePreviewChair(previewObject.transform, new Vector3(0.9f, 0f, -0.5f), Quaternion.Euler(0f, -90f, 0f));
                CreatePreviewChair(previewObject.transform, new Vector3(0.9f, 0f, 0.5f), Quaternion.Euler(0f, -90f, 0f));
            }
            else if (currentItemType == "ghe_nhua" || currentItemType == "relocate_chair")
            {
                CreatePreviewChair(previewObject.transform, Vector3.zero, Quaternion.identity);
            }
            else if (currentItemType == "relocate_table")
            {
                CreatePreviewTable(previewObject.transform, Vector3.zero, new Vector3(1.6f, 0.7f, 0.9f));
            }

            // Remove colliders from preview so raycast ignores it
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                Destroy(col);
            }
        }

        private void CreatePreviewTable(Transform parent, Vector3 localPos, Vector3 size)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Preview_Table";
            table.transform.SetParent(parent, false);
            table.transform.localPosition = localPos + new Vector3(0f, size.y * 0.5f, 0f);
            table.transform.localScale = size;
            ApplyGreenMaterial(table);
        }

        private void CreatePreviewChair(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = "Preview_Chair";
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPos + new Vector3(0f, 0.22f, 0f);
            chair.transform.localRotation = localRot;
            chair.transform.localScale = new Vector3(0.55f, 0.44f, 0.55f);
            ApplyGreenMaterial(chair);
        }

        private void ApplyGreenMaterial(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && greenOutlineMaterial != null)
            {
                renderer.material = greenOutlineMaterial;
            }
        }

        private void PlaceCurrentFurniture()
        {
            if (previewObject == null) return;

            Vector3 placePos = previewObject.transform.position;
            Quaternion placeRot = previewObject.transform.rotation;

            if (currentItemType == "relocate_chair" && pickedUpSeat != null)
            {
                pickedUpSeat.transform.position = placePos;
                pickedUpSeat.transform.rotation = placeRot;
                pickedUpSeat.gameObject.SetActive(true);
                pickedUpSeat = null;
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã di chuyển ghế đến vị trí mới.");
                ExitPlacementMode();
                return;
            }
            else if (currentItemType == "relocate_table" && pickedUpTable != null)
            {
                pickedUpTable.position = placePos;
                pickedUpTable.rotation = placeRot;
                pickedUpTable.gameObject.SetActive(true);
                pickedUpTable = null;
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã di chuyển bàn đến vị trí mới.");
                ExitPlacementMode();
                return;
            }

            // Placing from inventory
            if (PlayerInventory.Instance != null && !PlayerInventory.Instance.HasItem(currentItemType, 1))
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Trong túi không còn bàn ghế này!");
                ExitPlacementMode();
                return;
            }

            GameObject root = new GameObject($"Placed_{currentItemType}_{Time.time}");
            root.transform.position = placePos;
            root.transform.rotation = placeRot;

            if (currentItemType == "ban_doi")
            {
                SpawnRealTable(root.transform, Vector3.zero, new Vector3(1.6f, 0.7f, 0.9f));
                SpawnRealChair(root.transform, new Vector3(-0.9f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                SpawnRealChair(root.transform, new Vector3(0.9f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));
            }
            else if (currentItemType == "ban_bon")
            {
                SpawnRealTable(root.transform, Vector3.zero, new Vector3(1.8f, 0.7f, 1.4f));
                SpawnRealChair(root.transform, new Vector3(-0.9f, 0f, -0.5f), Quaternion.Euler(0f, 90f, 0f));
                SpawnRealChair(root.transform, new Vector3(-0.9f, 0f, 0.5f), Quaternion.Euler(0f, 90f, 0f));
                SpawnRealChair(root.transform, new Vector3(0.9f, 0f, -0.5f), Quaternion.Euler(0f, -90f, 0f));
                SpawnRealChair(root.transform, new Vector3(0.9f, 0f, 0.5f), Quaternion.Euler(0f, -90f, 0f));
            }
            else if (currentItemType == "ghe_nhua")
            {
                SpawnRealChair(root.transform, Vector3.zero, Quaternion.identity);
            }

            // Deduct from inventory
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveItem(currentItemType, 1);
            }

            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã đặt bàn ghế thành công! Khách có thể tới ngồi.");
            
            // Re-apply seating layout heights
            CustomerSeat[] allSeats = FindObjectsByType<CustomerSeat>(FindObjectsInactive.Exclude);
            
            // Continue placement mode if more exist in inventory
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem(currentItemType, 1))
            {
                EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã đặt xong! Tiếp tục chọn vị trí đặt bộ tiếp theo (ESC/X để hủy).");
            }
            else
            {
                ExitPlacementMode();
            }
        }

        private GameObject SpawnRealTable(Transform parent, Vector3 localPos, Vector3 size)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "TeaTable";
            table.transform.SetParent(parent, false);
            table.transform.localPosition = localPos + new Vector3(0f, size.y * 0.5f, 0f);
            table.transform.localScale = size;

            Renderer ren = table.GetComponent<Renderer>();
            if (ren != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.75f, 0.75f, 0.78f, 1f); // Inox table color
                ren.material = mat;
            }
            return table;
        }

        private GameObject SpawnRealChair(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = $"PlasticChair_Runtime_{Time.time}_{Random.Range(100, 999)}";
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPos + new Vector3(0f, 0.22f, 0f);
            chair.transform.localRotation = localRot;
            chair.transform.localScale = new Vector3(0.55f, 0.44f, 0.55f);

            Renderer ren = chair.GetComponent<Renderer>();
            if (ren != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Red plastic chair
                ren.material = mat;
            }

            BoxCollider box = chair.GetComponent<BoxCollider>();
            if (box == null) box = chair.AddComponent<BoxCollider>();
            box.isTrigger = true;

            chair.AddComponent<CustomerSeat>();
            return chair;
        }

        private void CancelPlacement()
        {
            if (currentItemType == "relocate_chair" && pickedUpSeat != null)
            {
                pickedUpSeat.gameObject.SetActive(true);
                pickedUpSeat = null;
            }
            else if (currentItemType == "relocate_table" && pickedUpTable != null)
            {
                pickedUpTable.gameObject.SetActive(true);
                pickedUpTable = null;
            }
            ExitPlacementMode();
            EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã thoát chế độ sắp xếp bàn ghế.");
        }

        private void ExitPlacementMode()
        {
            isPlacementModeActive = false;
            currentItemType = "";
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }

        private void TryPickUpExistingFurniture()
        {
            Transform player = FindPlayerTransform();
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)) : new Ray(player.position + Vector3.up * 1.5f, player.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
                CustomerSeat seat = hit.collider.GetComponentInParent<CustomerSeat>();
                if (seat != null)
                {
                    if (seat.IsOccupied)
                    {
                        EventManager.TriggerDialogueLine("Hoàng Hôn", "Đang có khách ngồi ở ghế này, không thể dời đi!");
                        return;
                    }
                    pickedUpSeat = seat;
                    seat.gameObject.SetActive(false);
                    EnterPlacementMode("relocate_chair");
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã nhấc ghế lên! Chọn vị trí mới và bấm Chuột trái/F để đặt lại.");
                    return;
                }

                if (hit.collider.name.Contains("Table") || hit.collider.name.Contains("Ban"))
                {
                    pickedUpTable = hit.collider.transform;
                    pickedUpTable.gameObject.SetActive(false);
                    EnterPlacementMode("relocate_table");
                    EventManager.TriggerDialogueLine("Hoàng Hôn", "Đã nhấc bàn lên! Chọn vị trí mới và bấm Chuột trái/F để đặt lại.");
                    return;
                }
            }
        }

        private Transform FindPlayerTransform()
        {
            try
            {
                GameObject p = GameObject.FindGameObjectWithTag(Constants.TAG_PLAYER);
                if (p != null) return p.transform;
            }
            catch (UnityException) { }

            GameObject pByName = GameObject.Find("Player_HoangHon") ?? GameObject.Find("Player");
            return pByName != null ? pByName.transform : null;
        }
    }
}
