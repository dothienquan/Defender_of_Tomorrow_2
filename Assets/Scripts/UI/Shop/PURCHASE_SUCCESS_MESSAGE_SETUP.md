# Hướng dẫn Setup Purchase Success Message

## Tổng quan
Hệ thống hiển thị thông báo mua thành công với hiệu ứng fade in/out khi người chơi mua item trong shop. Text sẽ mặc định ẩn và chỉ hiển thị khi mua thành công.

## Cách Setup

### Bước 1: Tạo UI Text cho thông báo
1. Trong Shop Panel, tạo một GameObject mới (ví dụ: `PurchaseSuccessText`)
2. Thêm component `TextMeshProUGUI` vào GameObject này
3. Đặt vị trí text ở nơi bạn muốn hiển thị (ví dụ: giữa màn hình, trên shop panel)
4. Cấu hình text:
   - Font size: Tùy chỉnh (ví dụ: 24-32)
   - Color: Màu bạn muốn (ví dụ: xanh lá, vàng)
   - Alignment: Center (căn giữa)
   - Text: Có thể để trống (sẽ được set tự động khi mua thành công)

### Bước 2: Kết nối với ShopController
1. Chọn GameObject có `ShopController` component
2. Trong Inspector, tìm section **Purchase Success Message**
3. Kéo GameObject có `TextMeshProUGUI` vào field **Purchase Success Text**
4. (Tùy chọn) Điều chỉnh các thời gian animation:
   - **Fade In Duration**: Thời gian fade in (mặc định: 0.3 giây)
   - **Display Duration**: Thời gian hiển thị text (mặc định: 1.5 giây)
   - **Fade Out Duration**: Thời gian fade out (mặc định: 0.5 giây)

## Cách hoạt động

Khi người chơi mua thành công một item:
1. `ShopController.BuyWeapon()` trả về `true`
2. `ShopController` gọi `ShowPurchaseSuccessMessage(itemName)`
3. Text sẽ:
   - Set nội dung: "Đã mua {itemName}!"
   - Fade in từ trong suốt (alpha = 0) → hiển thị đầy đủ (alpha = original)
   - Giữ nguyên trong thời gian `displayDuration`
   - Fade out từ hiển thị đầy đủ → trong suốt (alpha = 0)
   - Text vẫn ở trạng thái ẩn (alpha = 0) sau khi fade out xong

## Tùy chỉnh

### Thay đổi nội dung thông báo
Bạn có thể chỉnh sửa trong `ShopController.cs`, method `ShowPurchaseSuccessMessage()`:
```csharp
purchaseSuccessText.text = $"Đã mua {itemName}!";
// Có thể đổi thành:
// purchaseSuccessText.text = $"Mua thành công: {itemName}";
// purchaseSuccessText.text = $"+ {itemName}";
```

### Thay đổi thời gian animation
Trong Inspector của `ShopController`, section **Purchase Success Message**:
- Tăng `Fade In Duration` để fade in chậm hơn
- Tăng `Display Duration` để text hiển thị lâu hơn
- Tăng `Fade Out Duration` để fade out chậm hơn

## Lưu ý

- Script sử dụng DOTween, đảm bảo DOTween đã được import vào project
- Text mặc định sẽ ẩn (alpha = 0) khi Start
- Nếu không gán `Purchase Success Text`, thông báo sẽ không hiển thị (không ảnh hưởng đến chức năng mua hàng)
- Text sẽ tự động fade in/out khi mua thành công, không cần setup thêm

