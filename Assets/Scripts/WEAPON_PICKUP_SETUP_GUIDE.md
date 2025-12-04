# Hướng Dẫn Setup Weapon Pickup (Nhặt Vũ Khí)

## Tổng Quan

Hướng dẫn này sẽ giúp bạn setup vũ khí (như Fire Bow) để player có thể nhặt được, tương tự như nhặt key.

---

## Vấn Đề Thường Gặp

Khi kéo prefab vũ khí vào map, vũ khí có thể:
- Tự động snap vào một vị trí cố định
- Không thể nhặt được
- Không phản ứng khi player đến gần

---

## Bước 1: Kiểm Tra Prefab Weapon

### 1.1. Mở Prefab Weapon
- Mở prefab weapon (ví dụ: `Fire Bow.prefab`)
- Kiểm tra các components hiện có

### 1.2. Components Cần Có

**Bắt buộc:**
- ✅ `Transform` (tự động có)
- ✅ `SpriteRenderer` hoặc `Image` (để hiển thị weapon)
- ✅ `Item` component
- ✅ `Collider2D` (với `Is Trigger = true`)
- ✅ `Rigidbody2D` (nếu muốn weapon có physics)

**Tùy chọn (khuyến nghị):**
- ✅ `ItemMagnet` component (để weapon bay vào player khi đến gần)
- ✅ Tag = "Item" (để `PlayerItemCollector` nhận diện)

---

## Bước 2: Setup Item Component

### 2.1. Add Item Component
1. Chọn GameObject weapon trong prefab
2. Add Component → `Item`

### 2.2. Cấu Hình Item Component
- **ID**: Gán ID duy nhất cho weapon (ví dụ: 1, 2, 3...)
  - ⚠️ **QUAN TRỌNG**: ID này phải khớp với ID trong `ItemDictionary`!
- **Name**: Tên weapon (ví dụ: "Fire Bow")
- **Weapon Info**: Kéo `WeaponInfo` ScriptableObject vào đây
  - ⚠️ **QUAN TRỌNG**: Phải có `WeaponInfo` để weapon hoạt động đúng!

### 2.3. Kiểm Tra WeaponInfo
- `WeaponInfo` phải có:
  - `itemName`: Tên weapon
  - `icon`: Sprite icon (dùng trong inventory)
  - `weaponPrefab`: Prefab của weapon khi equip (khác với pickup prefab)

---

## Bước 3: Setup Collider2D

### 3.1. Add Collider2D
1. Chọn GameObject weapon trong prefab
2. Add Component → `BoxCollider2D` hoặc `CircleCollider2D`

### 3.2. Cấu Hình Collider2D
- **Is Trigger**: ✅ **BẬT** (quan trọng!)
- **Size**: Điều chỉnh để phù hợp với kích thước weapon
- **Offset**: Điều chỉnh nếu cần

### 3.3. Lưu Ý
- Nếu weapon có nhiều child objects, có thể đặt Collider2D trên child object thay vì parent
- Đảm bảo Collider2D không quá lớn hoặc quá nhỏ

---

## Bước 4: Setup Tag

### 4.1. Set Tag = "Item"
1. Chọn GameObject weapon trong prefab
2. Trong Inspector, tìm **Tag** dropdown
3. Chọn **"Item"**
   - Nếu chưa có tag "Item", tạo mới: Edit → Project Settings → Tags and Layers → Tags → Add Tag

### 4.2. Tại Sao Cần Tag "Item"?
- `PlayerItemCollector` chỉ nhận diện items có tag "Item"
- Nếu không có tag này, weapon sẽ không được nhặt tự động

---

## Bước 5: Setup ItemMagnet (Tùy Chọn Nhưng Khuyến Nghị)

### 5.1. Add ItemMagnet Component
1. Chọn GameObject weapon trong prefab
2. Add Component → `ItemMagnet`

### 5.2. Cấu Hình ItemMagnet
- **Magnet Distance**: 3-5 (khoảng cách để bắt đầu hút)
- **Magnet Speed**: 8-10 (tốc độ bay vào player)
- **Acceleration Rate**: 2-5 (tốc độ tăng tốc)
- **Rotation Speed**: 360 (tốc độ xoay khi bay)
- **Enable Rotation**: ✅ Bật (nếu muốn weapon xoay khi bay)
- **Scale Up On Magnet**: 1.2 (phóng to khi bắt đầu hút)
- **Scale Duration**: 0.2 (thời gian phóng to)

### 5.3. Lưu Ý
- `ItemMagnet` làm weapon bay vào player khi đến gần, tạo trải nghiệm tốt hơn
- Nếu không muốn hiệu ứng này, có thể bỏ qua bước này

---

## Bước 6: Setup Rigidbody2D (Nếu Cần)

### 6.1. Khi Nào Cần Rigidbody2D?
- Nếu weapon cần rơi xuống (gravity)
- Nếu weapon cần va chạm với vật thể khác
- Nếu weapon cần physics simulation

### 6.2. Cấu Hình Rigidbody2D
1. Add Component → `Rigidbody2D`
2. **Body Type**: 
   - `Dynamic` - nếu muốn weapon rơi và có physics
   - `Kinematic` - nếu chỉ muốn di chuyển bằng code (không bị gravity)
3. **Gravity Scale**: 
   - 0 = không rơi
   - 1 = rơi bình thường
4. **Freeze Rotation**: ✅ Bật (nếu không muốn weapon xoay khi rơi)

### 6.3. Lưu Ý
- Nếu dùng `ItemMagnet`, có thể không cần Rigidbody2D
- Nếu weapon "snap" vào một vị trí, có thể do Rigidbody2D đang bị lock hoặc có constraint

---

## Bước 7: Thêm Weapon Vào ItemDictionary

### 7.1. Tìm ItemDictionary
1. Trong scene, tìm GameObject có `ItemDictionary` component
2. Hoặc tạo mới: GameObject → Add Component → `ItemDictionary`

### 7.2. Thêm Weapon Vào Dictionary
1. Mở `ItemDictionary` trong Inspector
2. Tìm array `items` hoặc `itemPrefabs`
3. Thêm entry mới:
   - **ID**: Khớp với ID trong `Item` component của weapon prefab
   - **Prefab**: Kéo weapon prefab vào đây

### 7.3. Lưu Ý
- ⚠️ **QUAN TRỌNG**: ID trong `ItemDictionary` phải khớp với ID trong `Item` component!
- Nếu không khớp, weapon sẽ không được thêm vào inventory khi nhặt

---

## Bước 8: Kiểm Tra Player Setup

### 8.1. PlayerItemCollector
- Đảm bảo Player có `PlayerItemCollector` component
- Component này sẽ tự động nhặt items có tag "Item"

### 8.2. InventoryController
- Đảm bảo scene có `InventoryController` GameObject
- `PlayerItemCollector` cần `InventoryController` để thêm items vào inventory

---

## Checklist Setup Weapon Pickup

- [ ] Weapon prefab có `Item` component với ID hợp lệ
- [ ] `Item` component có `WeaponInfo` được gán
- [ ] Weapon prefab có `Collider2D` với `Is Trigger = true`
- [ ] Weapon prefab có Tag = "Item"
- [ ] Weapon prefab có `ItemMagnet` component (tùy chọn)
- [ ] Weapon được thêm vào `ItemDictionary` với ID khớp
- [ ] Player có `PlayerItemCollector` component
- [ ] Scene có `InventoryController` GameObject
- [ ] `Rigidbody2D` được cấu hình đúng (nếu có)

---

## Troubleshooting

### Vấn Đề: Weapon tự snap vào một vị trí

**Nguyên nhân có thể:**
1. `Rigidbody2D` có constraint (Freeze Position X/Y)
2. `Rigidbody2D` có `Body Type = Static`
3. Có script khác đang lock position

**Giải pháp:**
- Kiểm tra `Rigidbody2D` constraints
- Đổi `Body Type` thành `Dynamic` hoặc `Kinematic`
- Kiểm tra các scripts khác trên weapon

### Vấn Đề: Weapon không nhặt được

**Nguyên nhân có thể:**
1. Không có tag "Item"
2. `Collider2D` không có `Is Trigger = true`
3. `Item` component không có ID hoặc ID không khớp với `ItemDictionary`
4. Player không có `PlayerItemCollector` component

**Giải pháp:**
- Kiểm tra tag của weapon
- Kiểm tra `Collider2D.Is Trigger`
- Kiểm tra ID trong `Item` component và `ItemDictionary`
- Kiểm tra Player có `PlayerItemCollector` không

### Vấn Đề: Weapon nhặt được nhưng không vào inventory

**Nguyên nhân có thể:**
1. `ItemDictionary` không có weapon với ID tương ứng
2. `InventoryController` không tồn tại trong scene
3. Inventory đã đầy

**Giải pháp:**
- Kiểm tra `ItemDictionary` có weapon với ID khớp không
- Kiểm tra scene có `InventoryController` không
- Kiểm tra inventory còn slot trống không

### Vấn Đề: Weapon không bay vào player (ItemMagnet không hoạt động)

**Nguyên nhân có thể:**
1. Không có `ItemMagnet` component
2. `PlayerController.Instance` là null
3. `Magnet Distance` quá nhỏ

**Giải pháp:**
- Thêm `ItemMagnet` component
- Kiểm tra Player có `PlayerController` component không
- Tăng `Magnet Distance` trong `ItemMagnet`

---

## So Sánh Với Key Setup

### Key Prefab Thường Có:
- ✅ `KeyPickup` component (xử lý nhặt key)
- ✅ `Item` component với ID
- ✅ `Collider2D` với `Is Trigger = true`
- ✅ Tag = "Item" (nếu dùng inventory system)
- ✅ `ItemMagnet` component (tùy chọn)

### Weapon Prefab Cần Có:
- ✅ `Item` component với ID và `WeaponInfo`
- ✅ `Collider2D` với `Is Trigger = true`
- ✅ Tag = "Item"
- ✅ `ItemMagnet` component (khuyến nghị)
- ❌ Không cần `KeyPickup` (dùng `PlayerItemCollector` thay thế)

---

## Ví Dụ Setup Fire Bow

### 1. Components Trên Fire Bow Prefab:
```
Fire Bow (GameObject)
├── Transform
├── SpriteRenderer (hoặc Image)
├── Item
│   ├── ID: 1 (ví dụ)
│   ├── Name: "Fire Bow"
│   └── Weapon Info: [FireBowWeaponInfo ScriptableObject]
├── BoxCollider2D
│   ├── Is Trigger: ✅
│   └── Size: (1, 1)
├── ItemMagnet
│   ├── Magnet Distance: 5
│   ├── Magnet Speed: 8
│   └── ...
└── Tag: "Item"
```

### 2. ItemDictionary Entry:
```
ID: 1
Prefab: [Fire Bow.prefab]
```

### 3. Kết Quả:
- Player đến gần Fire Bow → Fire Bow bay vào player
- Player chạm Fire Bow → Fire Bow được thêm vào inventory
- Fire Bow xuất hiện trong inventory và có thể drag vào hotbar

---

## Kết Luận

Sau khi setup đúng, weapon sẽ:
- ✅ Có thể nhặt được khi player đến gần
- ✅ Tự động bay vào player (nếu có `ItemMagnet`)
- ✅ Được thêm vào inventory
- ✅ Có thể drag/click vào hotbar
- ✅ Có thể equip và sử dụng

Chúc bạn setup thành công! 🎮

