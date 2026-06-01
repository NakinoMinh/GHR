using UnityEngine;

namespace GanhHangRong.Systems
{
    /// <summary>
    /// Định nghĩa một lớp Parallax đơn lẻ.
    /// Xử lý cuộn, tự động lặp lại (tiling) nếu cần.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Tốc Độ Cuộn")]
        [Tooltip("Càng nhỏ càng xa. 0 = đứng yên so với camera. 1 = gần như không parallax.")]
        [SerializeField] private float speedMultiplierX = 0.5f;
        [Tooltip("Thường dùng cho parallax dọc nhẹ (ví dụ núi).")]
        [SerializeField] private float speedMultiplierY = 0f;

        [Header("Tự Động Lặp Lại (Tiling)")]
        [SerializeField] private bool isLooping = false;
        [Tooltip("Kích thước chiều ngang của ảnh/mesh để tự động lặp. Để 0 nếu không tự tính được.")]
        [SerializeField] private float spriteWidth = 0f;

        private float startPosX;
        private float startPosY;

        private void Start()
        {
            startPosX = transform.position.x;
            startPosY = transform.position.y;

            // Cố gắng tự tính sprite width nếu được yêu cầu lặp
            if (isLooping && spriteWidth <= 0)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    spriteWidth = sr.bounds.size.x;
                }
                else
                {
                    // Nếu là Mesh, lấy bounds
                    MeshRenderer mr = GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        spriteWidth = mr.bounds.size.x;
                    }
                }
            }
        }

        public void Move(float deltaX, float deltaY, float smoothing)
        {
            // Tính toán khoảng cách cuộn
            float moveX = deltaX * speedMultiplierX;
            float moveY = deltaY * speedMultiplierY;

            // Cập nhật vị trí
            Vector3 targetPosition = transform.position;
            targetPosition.x += moveX;
            targetPosition.y += moveY;

            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing);

            // Xử lý tự động lặp (Tiling)
            if (isLooping && spriteWidth > 0)
            {
                // Khoảng cách camera đã đi so với startPos của layer này
                float camX = Camera.main.transform.position.x;
                
                // Hiệu ứng cuộn tương đối: tốc độ parallax thực tế so với camera = (1 - speedMultiplierX)
                // Cần tính toán lại startPosX nếu đi quá xa
                float temp = camX * (1 - speedMultiplierX); 
                float dist = camX * speedMultiplierX;

                transform.position = new Vector3(startPosX + dist, transform.position.y, transform.position.z);

                if (temp > startPosX + spriteWidth)
                {
                    startPosX += spriteWidth;
                }
                else if (temp < startPosX - spriteWidth)
                {
                    startPosX -= spriteWidth;
                }
            }
        }
    }
}
