# Hướng Dẫn Setup DungeonDoorDiamondPanel Minigame

## Tổng Quan

Minigame này cho phép player kéo 3 kim cương từ hotbar vào 3 slot trên panel để mở cổng dungeon. Panel sẽ tự động hiện lên khi player chạm vào collider và tự động đóng khi đủ 3 item.

---

## Bước 1: Tạo Collider Trigger

### 1.1. Tạo GameObject cho Trigger Zone
1. Trong Hierarchy, tạo một GameObject mới (ví dụ: `DungeonDoorTrigger`)
2. Đặt GameObject này ở vị trí bạn muốn player chạm vào để mở panel

### 1.2. Thêm Collider2D
1. Chọn GameObject `DungeonDoorTrigger`
2. Add Component → `Box Collider 2D` hoặc `Circle Collider 2D`
3. Trong Inspector:
   - ✅ Tick **Is Trigger** = true
   - Điều chỉnh **Size** để phù hợp với vùng trigger

### 1.3. Thêm DungeonDoorInteractor Script
1. Add Component → `DungeonDoorInteractor`
2. Trong Inspector, cấu hình:
   - **Player Tag**: "Player" (mặc định)
   - **UI Panel**: Kéo GameObject chứa `DungeonDoorDiamondPanel` vào đây
   - **Diamond Panel**: Kéo component `DungeonDoorDiamondPanel` vào đây (hoặc để trống, script sẽ tự tìm)
   - **Hint** (Optional): GameObject hiển thị hint [E] (có thể để trống)

---

## Bước 2: Setup UI Panel

### 2.1. Tạo Panel GameObject
1. Trong Canvas, tạo một GameObject mới (ví dụ: `DungeonDoorPanel`)
2. Add Component → `RectTransform` (tự động có)
3. Setup RectTransform:
   - **Anchor**: Center (0.5, 0.5)
   - **Pivot**: Center (0.5, 0.5)
   - Điều chỉnh **Width** và **Height** theo ý muốn

### 2.2. Tạo Slots Parent
1. Trong `DungeonDoorPanel`, tạo child GameObject (ví dụ: `DiamondSlotsParent`)
2. Add Component → `RectTransform`
3. Add Component → `Grid Layout Group` (optional, để tự động sắp xếp slots)
   - **Cell Size**: (100, 100) hoặc kích thước phù hợp
   - **Spacing**: (10, 10)
   - **Child Alignment**: Middle Center

### 2.3. Tạo Slot Prefab
1. Tạo một GameObject mới (ví dụ: `DiamondSlot`)
2. Add Component → `RectTransform`
3. Add Component → `Image` (background cho slot)
4. Add Component → `Slot` script
5. Save thành Prefab:
   - Kéo GameObject vào thư mục Prefabs
   - Xóa instance trong scene (nếu có)

### 2.4. Thêm DungeonDoorDiamondPanel Script
1. Chọn `DungeonDoorPanel` GameObject
2. Add Component → `DungeonDoorDiamondPanel`
3. Trong Inspector, cấu hình:

#### Diamond Settings:
- **Required Diamond IDs**: Array các ID của kim cương cần thiết
  - Ví dụ: [7, 8, 9] cho Diamond1, Diamond2, Diamond3
  - Hoặc để trống nếu dùng `checkByName`
- **Required Diamond Count**: 3 (số lượng kim cương cần)
- **Check By Name**: 
  - ✅ True: Kiểm tra tên item có chứa "diamond" (không phân biệt hoa thường)
  - ❌ False: Chỉ kiểm tra theo ID

#### UI References:
- **Diamond Slots Parent**: Kéo `DiamondSlotsParent` GameObject vào đây
- **Diamond Slot Prefab**: Kéo prefab `DiamondSlot` vào đây
- **Slot Count**: 3 (số lượng slot sẽ spawn)
- **Confirm Button** (Optional): Nút xác nhận (có thể để trống nếu dùng auto-close)
- **Status Text** (Optional): TextMeshProUGUI hiển thị "2/3" (có thể để trống)
- **Status Format**: "{0}/{1}" (format cho status text)

#### Door Reference:
- **Door Controller**: Kéo `DoorClockController` vào đây (nếu có)
- **Door Collider**: Kéo Collider2D của cổng vào đây (sẽ tắt khi mở)
- **Door Interactor**: Kéo `DungeonDoorInteractor` vào đây (optional)

#### Auto Close:
- **Auto Close On Complete**: ✅ True (tự động đóng panel khi đủ item)

---

## Bước 3: Setup Items (Diamonds)

### 3.1. Đảm bảo Diamond Items có đúng ID
1. Mở prefab Diamond1, Diamond2, Diamond3
2. Kiểm tra `Item` component:
   - **ID**: Phải khớp với `requiredDiamondIDs` trong `DungeonDoorDiamondPanel`
   - **Name**: Nếu dùng `checkByName = true`, tên phải chứa "diamond"

### 3.2. Đảm bảo Items có trong ItemDictionary
1. Mở `ItemDictionary` ScriptableObject
2. Đảm bảo Diamond1, Diamond2, Diamond3 đã được thêm vào dictionary với đúng ID

---

## Bước 4: Setup Hotbar (Nếu chưa có)

### 4.1. Đảm bảo Hotbar có thể nhận items
1. Kiểm tra `HotbarController` có `hotbarPanel` được gán
2. Đảm bảo hotbar slots có `Slot` component
3. Đảm bảo items có thể kéo từ inventory vào hotbar

---

## Bước 5: Test Setup

### 5.1. Kiểm tra trong Editor
1. Play game
2. Điều khiển player đến vùng trigger
3. Panel sẽ tự động hiện lên
4. Kiểm tra xem 3 slots đã được spawn chưa

### 5.2. Test Drag & Drop
1. Đảm bảo player có 3 diamond items trong hotbar
2. Kéo từng diamond vào 3 slots trên panel
3. Khi đủ 3 item, panel sẽ tự động đóng
4. Cổng sẽ được mở (nếu đã setup `doorController`)

---

## Troubleshooting

### Panel không hiện lên khi chạm vào trigger
- ✅ Kiểm tra `DungeonDoorInteractor` có `uiPanel` được gán chưa
- ✅ Kiểm tra collider có `Is Trigger = true` chưa
- ✅ Kiểm tra player có tag "Player" chưa

### Slots không spawn
- ✅ Kiểm tra `diamondSlotsParent` có được gán chưa
- ✅ Kiểm tra `diamondSlotPrefab` có được gán chưa
- ✅ Kiểm tra `slotCount` có = 3 chưa

### Không thể kéo item vào slot
- ✅ Kiểm tra items có `ItemDragHandler` component chưa
- ✅ Kiểm tra slots có `Slot` component chưa
- ✅ Kiểm tra Canvas có `GraphicRaycaster` component chưa
- ✅ Kiểm tra EventSystem có trong scene chưa

### Panel không tự động đóng khi đủ 3 item
- ✅ Kiểm tra `autoCloseOnComplete` có = true chưa
- ✅ Kiểm tra `GetDiamondCount()` có trả về đúng số lượng chưa
- ✅ Kiểm tra `IsValidDiamond()` có hoạt động đúng chưa

### Items không được remove khỏi inventory
- ✅ Kiểm tra `inventoryController` có được tìm thấy chưa
- ✅ Kiểm tra `inventoryPanel` có được gán chưa
- ✅ Kiểm tra items có đúng ID/Name không

---

## Cấu Trúc Hierarchy Mẫu

```
Canvas
└── DungeonDoorPanel (GameObject với DungeonDoorDiamondPanel)
    ├── Background (Image - optional)
    ├── Title (TextMeshProUGUI - optional)
    ├── DiamondSlotsParent (Transform với GridLayoutGroup)
    │   ├── DiamondSlot (sẽ được spawn tự động)
    │   ├── DiamondSlot
    │   └── DiamondSlot
    ├── StatusText (TextMeshProUGUI - optional)
    └── ConfirmButton (Button - optional)
```

---

## Lưu Ý Quan Trọng

1. **Slots sẽ được spawn mỗi lần panel mở**: Khi panel tắt, slots sẽ bị xóa. Khi mở lại, slots sẽ được spawn lại.

2. **Items từ hotbar**: Player phải kéo items từ **hotbar** vào slots, không phải từ inventory.

3. **Auto-close**: Nếu `autoCloseOnComplete = true`, panel sẽ tự động đóng khi đủ 3 item. Nếu muốn player phải nhấn confirm button, set `autoCloseOnComplete = false` và gán `confirmButton`.

4. **Item IDs**: Đảm bảo IDs của diamonds khớp với `requiredDiamondIDs` trong `DungeonDoorDiamondPanel`.

---

## Ví Dụ Cấu Hình

### DungeonDoorInteractor:
```
Player Tag: "Player"
UI Panel: DungeonDoorPanel (GameObject)
Diamond Panel: DungeonDoorDiamondPanel (Component)
Hint: (Optional) EKeyHint GameObject
```

### DungeonDoorDiamondPanel:
```
Required Diamond IDs: [7, 8, 9]
Required Diamond Count: 3
Check By Name: false
Diamond Slots Parent: DiamondSlotsParent (Transform)
Diamond Slot Prefab: DiamondSlot (Prefab)
Slot Count: 3
Auto Close On Complete: true
```

---

Chúc bạn setup thành công! 🎮
