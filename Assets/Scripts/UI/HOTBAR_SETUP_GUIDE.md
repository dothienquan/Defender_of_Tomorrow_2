    # Hướng Dẫn Setup Hotbar Tương Thích Với Inventory

## Tổng Quan

Hướng dẫn này sẽ giúp bạn setup hotbar để tương thích với inventory system, bao gồm:
- Drag & Drop items từ inventory vào hotbar
- Left-click để tự động thêm item vào slot trống đầu tiên
- Sử dụng số (1-9, 0) để sử dụng items trong hotbar
- Tương thích với weapon system và ActiveInventory

---

## Bước 1: Kiểm Tra Prefabs

### 1.1. Slot Prefab
- **Path**: `Assets/Prefabs/Scene Setup/InventoryUI/Slot.prefab`
- **Components cần có**:
  - ✅ `RectTransform`
  - ✅ `Image` (background của slot)
  - ✅ `Slot` component (script)
  - ✅ `CanvasRenderer`

### 1.2. Item Prefab
- Mỗi item prefab cần có:
  - ✅ `RectTransform`
  - ✅ `Image` (icon của item)
  - ✅ `Item` component
  - ✅ `ItemDragHandler` component
  - ✅ `CanvasGroup` component (cho drag & drop)

---

## Bước 2: Setup Hotbar Panel trong Scene

### 2.1. Tạo Hotbar Panel
1. Trong Hierarchy, tạo GameObject mới: **HotbarPanel**
2. Thêm components:
   - `RectTransform`
   - `Image` (optional - background)
   - `GridLayoutGroup` (để tự động sắp xếp slots)

### 2.2. Cấu Hình GridLayoutGroup
- **Cell Size**: (55, 55) - kích thước mỗi slot
- **Spacing**: (15, 0) - khoảng cách giữa các slot
- **Start Corner**: Upper Left
- **Child Alignment**: Middle Center
- **Constraint**: Fixed Column Count = 10 (hoặc số slot bạn muốn)

### 2.3. Tạo Slots trong Hotbar
**Cách 1: Tự động tạo (khuyến nghị)**
- HotbarController sẽ tự động tạo slots khi `SetHotbarItems()` được gọi
- Hoặc thêm code trong `Start()` để tự động tạo slots

**Cách 2: Tạo thủ công**
- Kéo `Slot.prefab` vào `HotbarPanel` 10 lần (hoặc số lượng bạn muốn)
- Đảm bảo mỗi slot có `Slot` component

---

## Bước 3: Setup HotbarController

### 3.1. Tạo GameObject cho HotbarController
1. Tạo GameObject mới: **HotbarController** (hoặc đặt trên GameObject có sẵn)
2. Add Component → `HotbarController`

### 3.2. Gán References trong Inspector
- **Hotbar Panel**: Kéo `HotbarPanel` GameObject vào đây
- **Slot Prefab**: Kéo `Slot.prefab` vào đây
- **Slot Count**: 10 (hoặc số lượng slot bạn muốn)

### 3.3. Kiểm Tra ItemDictionary
- Đảm bảo có `ItemDictionary` GameObject trong scene
- `HotbarController` sẽ tự động tìm `ItemDictionary` trong `Awake()`

---

## Bước 4: Setup Inventory Panel

### 4.1. Tạo Inventory Panel
1. Tạo GameObject mới: **InventoryPanel**
2. Thêm components:
   - `RectTransform`
   - `Image` (optional - background)
   - `GridLayoutGroup` (để tự động sắp xếp slots)

### 4.2. Cấu Hình GridLayoutGroup cho Inventory
- **Cell Size**: (55, 55)
- **Spacing**: (10, 10)
- **Start Corner**: Upper Left
- **Child Alignment**: Upper Left
- **Constraint**: Flexible (hoặc Fixed Row Count/Column Count tùy layout)

### 4.3. Tạo Slots trong Inventory
- Kéo `Slot.prefab` vào `InventoryPanel` (số lượng tùy ý, ví dụ: 18 slots)

---

## Bước 5: Setup InventoryController

### 5.1. Tạo GameObject cho InventoryController
1. Tạo GameObject mới: **InventoryController** (hoặc đặt trên GameObject có sẵn)
2. Add Component → `InventoryController`

### 5.2. Gán References trong Inspector
- **Inventory Panel**: Kéo `InventoryPanel` GameObject vào đây
- **Slot Prefab**: Kéo `Slot.prefab` vào đây (cùng prefab với hotbar)
- **Slot Count**: 18 (hoặc số lượng slot bạn muốn)

---

## Bước 6: Setup ActiveInventory

### 6.1. Tạo GameObject cho ActiveInventory
1. Tạo GameObject mới: **ActiveInventory**
2. Add Component → `ActiveInventory` (Singleton)

### 6.2. Gán References trong Inspector
- **Hotbar Controller**: Kéo `HotbarController` GameObject vào đây
- Hoặc để trống, script sẽ tự động tìm trong `Awake()`

---

## Bước 7: Kiểm Tra Item Prefabs

### 7.1. Mỗi Item Prefab cần có:
1. **Item Component**:
   - `ID`: Unique ID cho item
   - `Name`: Tên item
   - `weaponInfo`: Nếu là vũ khí, gán `WeaponInfo` ScriptableObject

2. **ItemDragHandler Component**:
   - Tự động được thêm khi item được instantiate
   - Hoặc thêm vào prefab

3. **CanvasGroup Component**:
   - `Blocks Raycasts`: ✅ Bật
   - `Interactable`: ✅ Bật
   - `Alpha`: 1

4. **Image Component**:
   - `Sprite`: Icon của item
   - `Raycast Target`: ✅ Bật (để có thể click)

---

## Bước 8: Test Setup

### 8.1. Kiểm Tra Hierarchy
```
Canvas
├── HotbarPanel
│   ├── Slot (0)
│   ├── Slot (1)
│   ├── ...
│   └── Slot (9)
├── InventoryPanel
│   ├── Slot (0)
│   ├── Slot (1)
│   ├── ...
│   └── Slot (17)
├── HotbarController (GameObject với HotbarController component)
├── InventoryController (GameObject với InventoryController component)
├── ActiveInventory (GameObject với ActiveInventory component)
└── ItemDictionary (GameObject với ItemDictionary component)
```

### 8.2. Test Chức Năng
1. **Drag & Drop**:
   - Kéo item từ inventory vào hotbar → Item di chuyển
   - Kéo item từ hotbar vào inventory → Item di chuyển
   - Kéo item giữa 2 slot trong hotbar → Swap items

2. **Left-Click**:
   - Click trái vào item trong inventory → Item tự động vào slot trống đầu tiên của hotbar
   - Click trái vào item đã ở trong hotbar → Không làm gì

3. **Keyboard Shortcuts**:
   - Nhấn 1-9, 0 → Sử dụng item ở slot tương ứng
   - Nếu là vũ khí → Tự động equip

---

## Bước 9: Troubleshooting

### Vấn Đề: Item không thể drag
**Giải pháp**:
- Kiểm tra `ItemDragHandler` component có trên item không
- Kiểm tra `CanvasGroup` có `Blocks Raycasts` = true không
- Kiểm tra `Image` có `Raycast Target` = true không

### Vấn Đề: Left-click không hoạt động
**Giải pháp**:
- Kiểm tra `HotbarController` có được gán `hotbarPanel` không
- Kiểm tra `hotbarPanel` có slots không
- Kiểm tra Console có lỗi gì không

### Vấn Đề: Weapon không equip khi nhấn số
**Giải pháp**:
- Kiểm tra `ActiveInventory` có reference đến `HotbarController` không
- Kiểm tra `InventorySlot` component có trên hotbar slots không (tự động được thêm)
- Kiểm tra `WeaponInfo` có được gán vào item không

### Vấn Đề: Hotbar slots không tự động tạo
**Giải pháp**:
- Kiểm tra `HotbarController.slotPrefab` có được gán không
- Kiểm tra `HotbarController.slotCount` > 0
- Thêm code trong `Start()` để tự động tạo slots nếu chưa có

---

## Bước 10: Tự Động Tạo Slots (Optional)

Nếu muốn hotbar tự động tạo slots khi Start, thêm code này vào `HotbarController`:

```csharp
private void Start()
{
    // Tự động tạo slots nếu chưa có
    if (hotbarPanel != null && hotbarPanel.transform.childCount == 0)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slotPrefab != null)
            {
                Instantiate(slotPrefab, hotbarPanel.transform);
            }
        }
    }
}
```

---

## Checklist Setup

- [ ] Slot Prefab có `Slot` component
- [ ] HotbarPanel được tạo và có `GridLayoutGroup`
- [ ] HotbarController được setup với `hotbarPanel` và `slotPrefab`
- [ ] InventoryPanel được tạo và có `GridLayoutGroup`
- [ ] InventoryController được setup với `inventoryPanel` và `slotPrefab`
- [ ] ActiveInventory được setup và có reference đến `HotbarController`
- [ ] ItemDictionary tồn tại trong scene
- [ ] Item prefabs có `Item`, `ItemDragHandler`, `CanvasGroup`, `Image` components
- [ ] Test drag & drop hoạt động
- [ ] Test left-click hoạt động
- [ ] Test keyboard shortcuts (1-9, 0) hoạt động

---

## Lưu Ý Quan Trọng

1. **Slot Prefab**: Hotbar và Inventory nên dùng **cùng một Slot prefab** để đảm bảo tương thích
2. **ItemDragHandler**: Phải có trên mọi item để có thể drag và left-click
3. **CanvasGroup**: Cần thiết cho drag & drop hoạt động đúng
4. **HotbarController**: Phải có reference đến `hotbarPanel` để left-click hoạt động
5. **ActiveInventory**: Phải có reference đến `HotbarController` để weapon system hoạt động

---

## Kết Luận

Sau khi setup xong, bạn sẽ có:
- ✅ Hotbar với 10 slots (hoặc số lượng tùy chỉnh)
- ✅ Inventory với nhiều slots
- ✅ Drag & Drop items giữa inventory và hotbar
- ✅ Left-click để tự động thêm item vào hotbar
- ✅ Keyboard shortcuts để sử dụng items
- ✅ Weapon system tự động equip khi nhấn số

Chúc bạn setup thành công! 🎮

