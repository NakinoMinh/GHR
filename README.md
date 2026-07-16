# Gánh Hàng Rong

Game mô phỏng bán hàng rong 3D lấy bối cảnh khu bến tàu Rạch Giá, Kiên Giang. Người chơi vào vai Hoàng Hôn, chuẩn bị nguyên liệu, chọn thực đơn, mở quán, pha chế và phục vụ khách trong một vòng lặp ngày liên tục.

> Gameplay hiện tại tập trung vào game loop theo ngày, không còn phụ thuộc vào tiến trình chapter. Scene gameplay vẫn mang tên kỹ thuật cũ `Chapter1` để giữ tương thích với save, build settings và các hệ thống đang có.

## Game Loop Hiện Tại

1. **06:00 - Bắt đầu ngày:** người chơi thức dậy tại nhà, kiểm tra kho và đi mua nguyên liệu hoặc sách công thức.
2. **08:00 - Chuẩn bị quán:** đồng hồ game tạm dừng để người chơi mở `Tab`, chọn tối đa 3 món và lưu thực đơn.
3. **Mở cửa:** nhấn `Space` hai lần để xác nhận. Khách chỉ gọi các món có trong thực đơn đã lưu.
4. **08:00-22:00 - Kinh doanh:** pha đồ uống, chuẩn bị món, phục vụ khách, thu tiền và xử lý ly bẩn.
5. **22:00 - Đóng quán:** tương tác với bảng đóng/mở cửa bằng `F`. Quán ngừng nhận khách mới nhưng vẫn hoàn tất các đơn còn lại.
6. **Kết thúc ngày:** trở về nhà, tương tác với giường để xem báo cáo doanh thu và bắt đầu ngày tiếp theo.

Nếu người chơi chưa về nhà trước 00:00, game tự kết thúc ngày, đưa nhân vật về nhà và áp dụng phạt thức dậy muộn.

## Tính Năng Gameplay

- Di chuyển nhân vật 3D theo hướng camera, đi bộ và chạy.
- Camera tự do với độ nhạy chuột có thể điều chỉnh.
- Góc nhìn pha chế riêng tại xe trà, giữ nguyên quy trình lấy ly, thêm nguyên liệu, đun/rót nước, thêm đá và phục vụ.
- NPC đến quán, gọi món, chờ, sử dụng chỗ ngồi, thanh toán hoặc rời đi nếu chờ quá lâu.
- Hệ thống ngày đêm, giờ mở cửa, đóng cửa và báo cáo cuối ngày.
- Chợ gồm nhiều quầy bán nguyên liệu, vật dụng và sách công thức.
- Kho hàng, giỏ mua sắm, tiền, giá mua/bán và tiêu hao nguyên liệu.
- Mở khóa món bằng cách mua sách công thức ở chợ.
- Tab Menu gồm `Thực đơn`, `Công thức`, `Liên hệ` và `Kho hàng`.
- Lưu/tiếp tục game, bao gồm thời gian, tiền, kho, công thức và thực đơn đang bán.
- Intro cốt truyện khi bắt đầu game mới.
- Âm thanh thao tác, khách hàng, thanh toán, mở/đóng quán và ambience môi trường.
- Menu tạm dừng với độ nhạy chuột, âm lượng tổng, nhạc nền, hiệu ứng và môi trường.
- Môi trường bến cảng với thuyền di chuyển, khu vé, chợ và các tuyến đường ven biển.

## Thực Đơn

Hai món được mở sẵn:

- Trà Đá Nguyên Chất
- Cà Phê Đen Đá

Các món cần mua sách công thức trước khi có thể đưa vào thực đơn:

| Món | Loại |
| --- | --- |
| Bún Cá Kiên Giang | Món ăn |
| Bánh Canh Ghẹ | Món ăn |
| Tôm Rim Nước Mắm | Món ăn |
| Mực Nướng Muối Ớt | Món ăn |
| Nghêu Xào Cay | Món ăn |
| Nước Mía | Đồ uống |
| Trà Chanh | Đồ uống |
| Nước Dừa | Đồ uống |

Sách công thức và nguyên liệu tương ứng được bán tại các quầy chợ. Trước khi mua sách, món hiển thị trạng thái `CHƯA CÓ CÔNG THỨC`. Khi ca bán đã bắt đầu, thực đơn được khóa để bảo đảm khách không gọi món ngoài danh sách đã lưu.

## Điều Khiển

| Phím | Chức năng |
| --- | --- |
| `WASD` / phím mũi tên | Di chuyển |
| `Shift` | Chạy |
| Chuột | Xoay camera / chọn vật phẩm khi pha chế |
| `F` | Tương tác, vào/thoát góc pha chế, mở quầy chợ, đóng quán hoặc ngủ |
| `Tab` | Mở/đóng bảng thực đơn, công thức và kho |
| `Space` | Xác nhận mở/đóng quán hoặc phục vụ khi đang ở xe trà |
| `E` | Trò chuyện hoặc tương tác phụ theo ngữ cảnh |
| `Q` | Phục vụ/đặt món cho khách theo ngữ cảnh |
| `R` | Cầm hoặc dọn ly bẩn theo ngữ cảnh |
| `Z` | Rửa hoặc đổ bỏ ly đang cầm tại bồn rửa |
| `Esc` | Đóng UI, hủy xác nhận hoặc mở menu tạm dừng |

## Yêu Cầu Kỹ Thuật

- Unity `6000.4.6f1`
- Universal Render Pipeline `17.4.0`
- Input System `1.19.0`
- AI Navigation `2.0.12`
- Git LFS được khuyến nghị cho các asset 3D và texture lớn

## Chạy Dự Án

1. Clone repository và checkout nhánh cần làm việc.
2. Mở Unity Hub, chọn **Add project from disk** và trỏ tới thư mục repository.
3. Mở project bằng Unity `6000.4.6f1` để tránh reimport hoặc nâng cấp asset không cần thiết.
4. Chờ Unity hoàn tất import package và compile script.
5. Mở scene `Assets/_Project/Scenes/MainMenu/MainMenu.unity`.
6. Nhấn **Play**, chọn **Chơi mới** để xem intro hoặc **Tiếp tục** để tải save hiện có.

Không cần chạy lại các menu dựng scene cũ. Dữ liệu chợ, công thức và prefab thuyền runtime được lưu sẵn trong project; các editor setup chỉ bổ sung asset nếu phát hiện nội dung còn thiếu.

## Scene Trong Build

- `MainMenu`: điểm vào chính của game.
- `Chapter1`: scene game loop hiện tại; tên được giữ để tương thích kỹ thuật.
- `Chapter2`: scene cũ còn trong Build Settings để tương thích, không phải luồng gameplay chính hiện tại.

## Cấu Trúc Chính

```text
Assets/_Project/
├── Art/                  # Nhân vật, môi trường và props
├── Resources/            # Recipe, UI atlas, audio và prefab runtime
├── Scenes/               # Main Menu và scene gameplay
├── ScriptableObjects/    # ItemData, ShopData và dữ liệu chợ
└── Scripts/
    ├── Core/             # Constants, state và event nền tảng
    ├── Interaction/      # Xe trà, vật phẩm, giường và tương tác
    ├── Market/           # Chợ, kho, nấu món và mở khóa công thức
    ├── NPC/              # Sinh khách và hành vi khách hàng
    ├── Player/           # Điều khiển, camera và chỉ số nhân vật
    ├── Systems/          # Game loop, save, thời gian và thuyền
    └── UI/               # HUD, Tab Menu, chợ, pause và báo cáo ngày
```

## Kiểm Tra Trước Khi Push

- Mở game từ `MainMenu`, không chạy thẳng scene gameplay khi kiểm tra luồng intro/save.
- Kiểm tra Console không có error sau khi vào game và sau một lần chuyển ngày.
- Kiểm tra món khóa không thể thêm vào thực đơn trước khi mua sách.
- Kiểm tra thực đơn chỉ được chỉnh trong giai đoạn chuẩn bị.
- Kiểm tra thuyền vẫn xuất hiện và di chuyển sau khi đi từ Main Menu vào gameplay.
- Không commit thư mục `Library`, `Temp`, `Logs` hoặc file build cục bộ.

## Đóng Góp

Tạo nhánh riêng cho thay đổi, giữ phạm vi chỉnh sửa rõ ràng, kiểm tra Play Mode và gửi Pull Request kèm mô tả gameplay đã kiểm chứng.
