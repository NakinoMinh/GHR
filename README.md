# GanhHangRongDemo

## Giới thiệu

Dự án **Gánh Hàng Rong** là một trò chơi mô phỏng 2.5D phong cách indie, mang không khí cô đơn, ấm áp, mưa và ánh đèn đêm trên các chợ hải đường Việt Nam. Mục tiêu của prototype là tạo một cảnh chơi đầu tiên (Chapter 1) với nhân vật 3D, chuyển động mượt mà, camera phong cách INSIDE và hệ thống UI chính (Main Menu).

## Tính năng chính

- Unity 2022+ với Universal Render Pipeline (URP)
- Nhân vật 3D (`Meshy_AI_biped`) với animation Walk & Run
- Điều khiển 2D (`Rigidbody2D`) + sprint (Shift)
- Cinematic Camera giống INSIDE
- Main Menu UI có nền ảnh, nút Play & Quit
- Hệ thống chuyển cảnh (`TransitionUI`)

## Hướng dẫn chạy

1. Mở Unity Hub > Add > `d:/G/GHR`.
2. Đảm bảo đã cài `Input System` và `glTFast` (đã được import trong `manifest.json`).
3. Chọn **Gánh Hàng Rong > Dựng Scene Main Menu** để tạo Menu.
4. Chọn **Gánh Hàng Rong > Dựng Scene Chapter 1** để tạo Chapter 1.
5. Nhấn **Play** và dùng `A/D` di chuyển, giữ `Shift` để chạy.

## Đóng góp

Clone repo, tạo nhánh mới, thực hiện thay đổi và gửi Pull Request.

---

*Đây là file README tự động tạo bởi Antigravity.*
