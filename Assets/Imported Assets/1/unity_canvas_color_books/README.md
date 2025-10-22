# Canvas Mini-Game (UI) — Sắp xếp sách theo màu (colorId)

Toàn bộ thao tác diễn ra trên **Canvas** (UI). Kéo/Thả là UI-based.

## Setup nhanh
1. **Canvas**
   - Tạo `Canvas` (Screen Space - Overlay hoặc Screen Space - Camera đều được).
   - Canvas tự có `GraphicRaycaster`. Scene cần **EventSystem**.
2. **Panel mini-game**
   - Tạo `PanelRoot` (GameObject con của Canvas) → **Disable** sẵn.
   - Trong `PanelRoot`, tạo **6 Slot** (Image UI) → gắn `UISlot.cs`, đặt `colorId` (0..5).
   - Trong `PanelRoot`, tạo **6 Book** (Image UI) → gắn `UIBookDrag.cs`, đặt `colorId` (0..5).
3. **Controller**
   - Tạo object chứa `UIMiniGamePanelController.cs` (có thể đặt trên Canvas).
   - Gán:
     - `panelRoot` = `PanelRoot`.
     - `door` = tham chiếu Door (world object).
     - `books` = kéo 6 Book vào mảng.
     - `slots` = kéo 6 Slot vào mảng.
     - `totalBooks = 6`.
   - Ở mỗi **UIBookDrag**, kéo `UIMiniGamePanelController` vào field `controller`.
4. **Door (world)**
   - Tạo `Door` (Sprite 2D), thêm `Collider2D` + `DoorController.cs`.
   - Main Camera thêm `Physics2DRaycaster` (để nhận click).
   - Trong `DoorController`:
     - **On Door Clicked**: kéo đối tượng có `UIMiniGamePanelController` vào → chọn `UIMiniGamePanelController.OpenPanel()`.
5. **Chạy**
   - Play → click Door → `PanelRoot` bật.
   - Kéo sách (UI) lên slot (UI) cùng `colorId`. Đủ 6 → `PanelRoot` tắt, Door mở (trượt lên).

## Ghi chú
- Phát hiện "đang ở trên slot" dùng `RectTransformUtility.RectangleContainsScreenPoint()`, hoạt động cho cả Overlay lẫn Camera mode (nhớ set `Canvas.worldCamera` nếu dùng Screen Space - Camera).
- Snap dùng toạ độ **anchoredPosition** (UI). Có thể tinh chỉnh `snapOffset` trong `UISlot`.
- Reset puzzle: `UIMiniGamePanelController.ResetPuzzle()`.

## Tài sản mẫu
Thư mục `Placeholders/` có PNG đơn giản cho Books/Slots để test nhanh.
