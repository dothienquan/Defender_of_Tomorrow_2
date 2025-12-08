# Hướng Dẫn Setup Sword Weapon (Vũ Khí Kiếm)

## Tổng Quan

Hướng dẫn này sẽ giúp bạn setup một vũ khí Sword để player có thể nhặt được và sử dụng trong game.

---

## Bước 1: Tạo/Copy Sword Pickup Prefab

### 1.1. Tạo Prefab Mới Hoặc Copy Từ Prefab Có Sẵn

**Option A: Copy từ prefab có sẵn**
- Mở `Assets/Prefabs/Weapons/Sword.prefab`
- Duplicate (Ctrl+D) để tạo bản copy
- Đổi tên thành `Sword_Pickup.prefab` (để phân biệt với weapon prefab khi equip)

**Option B: Tạo mới từ đầu**
- Tạo GameObject mới trong scene
- Đặt tên: `Sword_Pickup`
- Kéo vào thư mục `Assets/Prefabs/Weapons/` để tạo prefab

---

## Bước 2: Setup Components Cơ Bản

### 2.1. SpriteRenderer (Để Hiển Thị Sword)

1. Chọn GameObject `Sword_Pickup`
2. Add Component → `SpriteRenderer`
3. Kéo sprite của sword vào **Sprite** field
4. Điều chỉnh **Sorting Order** nếu cần (ví dụ: 1)

### 2.2. Item Component (QUAN TRỌNG)

1. Add Component → `Item`
2. Cấu hình:
   - **ID**: Gán ID duy nhất (ví dụ: 10)
     - ⚠️ **QUAN TRỌNG**: ID này phải khớp với ID trong `ItemDictionary`!
   - **Name**: "Sword" (hoặc tên bạn muốn)
   - **Weapon Info**: Kéo `Sword.asset` ScriptableObject vào đây
     - File: `Assets/Scriptable Objects/Sword.asset`
     - ⚠️ **QUAN TRỌNG**: Phải có `WeaponInfo` để sword hoạt động đúng!

### 2.3. Collider2D (Để Nhặt Được)

1. Add Component → `BoxCollider2D` hoặc `CircleCollider2D`
2. Cấu hình:
   - **Is Trigger**: ✅ **BẬT** (quan trọng!)
   - **Size**: Điều chỉnh để phù hợp với kích thước sword
     - Ví dụ: Size = (1, 1) cho BoxCollider2D
     - Hoặc Radius = 0.5 cho CircleCollider2D

### 2.4. Tag = "Item"

1. Chọn GameObject `Sword_Pickup`
2. Trong Inspector, tìm **Tag** dropdown
3. Chọn **"Item"**
   - Nếu chưa có tag "Item", tạo mới:
     - Edit → Project Settings → Tags and Layers → Tags → Add Tag → "Item"

---

## Bước 3: Setup ItemMagnet (Tùy Chọn Nhưng Khuyến Nghị)

### 3.1. Add ItemMagnet Component

1. Add Component → `ItemMagnet`

### 3.2. Cấu Hình ItemMagnet

- **Magnet Distance**: 3-5 (khoảng cách để bắt đầu hút)
- **Magnet Speed**: 8-10 (tốc độ bay vào player)
- **Acceleration Rate**: 2-5 (tốc độ tăng tốc)
- **Rotation Speed**: 360 (tốc độ xoay khi bay)
- **Enable Rotation**: ✅ Bật (nếu muốn sword xoay khi bay)
- **Scale Up On Magnet**: 1.2 (phóng to khi bắt đầu hút)
- **Scale Duration**: 0.2 (thời gian phóng to)

**Lưu Ý**: `ItemMagnet` làm sword bay vào player khi đến gần, tạo trải nghiệm tốt hơn.

---

## Bước 4: Setup Rigidbody2D (Nếu Cần)

### 4.1. Khi Nào Cần Rigidbody2D?

- Nếu sword cần rơi xuống (gravity)
- Nếu sword cần va chạm với vật thể khác
- Nếu sword cần physics simulation

### 4.2. Cấu Hình Rigidbody2D

1. Add Component → `Rigidbody2D`
2. Cấu hình:
   - **Body Type**: 
     - `Dynamic` - nếu muốn sword rơi và có physics
     - `Kinematic` - nếu chỉ muốn di chuyển bằng code (không bị gravity)
   - **Gravity Scale**: 
     - 0 = không rơi
     - 1 = rơi bình thường
   - **Freeze Rotation**: ✅ Bật (nếu không muốn sword xoay khi rơi)

**Lưu Ý**: Nếu dùng `ItemMagnet`, có thể không cần Rigidbody2D.

---

## Bước 5: Thêm Sword Vào ItemDictionary

### 5.1. Tìm ItemDictionary

1. Trong scene, tìm GameObject có `ItemDictionary` component
2. Hoặc tìm trong `Scene Setup` prefab

### 5.2. Thêm Sword Vào Dictionary

1. Mở `ItemDictionary` trong Inspector
2. Tìm list `itemPrefabs`
3. Thêm entry mới:
   - **Size**: Tăng số lượng (ví dụ: từ 9 lên 10)
   - **Element 9**: Kéo `Sword_Pickup.prefab` vào đây

### 5.3. Kiểm Tra ID

⚠️ **QUAN TRỌNG**: 
- `ItemDictionary` tự động set ID = index + 1
- Nếu sword ở index 9 → ID = 10
- Đảm bảo ID trong `Item` component của sword prefab = 10 (hoặc để 0 để tự động set)

**Cách kiểm tra:**
- Mở `Sword_Pickup.prefab`
- Kiểm tra `Item` component → **ID**
- Nếu ID = 0, nó sẽ tự động được set = index + 1 khi ItemDictionary khởi tạo
- Nếu ID > 0, đảm bảo nó khớp với index trong ItemDictionary

---

## Bước 6: Kiểm Tra WeaponInfo ScriptableObject

### 6.1. Mở Sword.asset

- File: `Assets/Scriptable Objects/Sword.asset`

### 6.2. Kiểm Tra Các Trường

- **id**: 10 (phải khớp với ID trong Item component)
- **itemName**: "Sword"
- **icon**: Sprite icon (dùng trong inventory)
- **weaponPrefab**: Prefab của sword khi equip (khác với pickup prefab)
  - Ví dụ: `Assets/Prefabs/Weapons/Sword.prefab` (prefab khi equip)
- **weaponCooldown**: 0.5 (thời gian cooldown)
- **weaponDamage**: 1 (damage của sword)
- **weaponRange**: 0 (range của sword)

### 6.3. Lưu Ý

- `weaponPrefab` trong WeaponInfo phải là prefab của sword khi equip (có `Sword` component)
- `Sword_Pickup.prefab` là prefab để nhặt (có `Item` component)
- Hai prefab này khác nhau!

---

## Bước 7: Setup WorldItemUIHandler (Nếu Sword Có UI Components)

### 7.1. Khi Nào Cần WorldItemUIHandler?

- Nếu sword prefab có `RectTransform`, `Image`, `CanvasRenderer` (UI components)
- Để ngăn sword bị snap vào Canvas khi spawn

### 7.2. Add WorldItemUIHandler

1. Add Component → `WorldItemUIHandler`
2. Cấu hình:
   - **Auto Disable UI In World**: ✅ Bật
   - **Auto Enable UI In Inventory**: ✅ Bật

**Lưu Ý**: Nếu sword chỉ có `SpriteRenderer` (không có UI components), không cần component này.

---

## Bước 8: Kiểm Tra Player Setup

### 8.1. PlayerItemCollector

- Đảm bảo Player có `PlayerItemCollector` component
- Component này sẽ tự động nhặt items có tag "Item"

### 8.2. InventoryController

- Đảm bảo scene có `InventoryController` GameObject
- `PlayerItemCollector` cần `InventoryController` để thêm items vào inventory

---

## Checklist Setup Sword Pickup

- [ ] Sword prefab có `Item` component với ID hợp lệ
- [ ] `Item` component có `WeaponInfo` được gán (Sword.asset)
- [ ] Sword prefab có `Collider2D` với `Is Trigger = true`
- [ ] Sword prefab có Tag = "Item"
- [ ] Sword prefab có `ItemMagnet` component (tùy chọn nhưng khuyến nghị)
- [ ] Sword được thêm vào `ItemDictionary` với ID khớp
- [ ] `WeaponInfo` (Sword.asset) có `weaponPrefab` được gán đúng
- [ ] Player có `PlayerItemCollector` component
- [ ] Scene có `InventoryController` GameObject
- [ ] `Rigidbody2D` được cấu hình đúng (nếu có)
- [ ] `WorldItemUIHandler` được thêm (nếu sword có UI components)

---

## Ví Dụ Setup Sword

### 1. Components Trên Sword_Pickup Prefab:

```
Sword_Pickup (GameObject)
├── Transform
├── SpriteRenderer
│   └── Sprite: [Sword Sprite]
├── Item
│   ├── ID: 10 (hoặc 0 để auto)
│   ├── Name: "Sword"
│   └── Weapon Info: [Sword.asset]
├── BoxCollider2D
│   ├── Is Trigger: ✅
│   └── Size: (1, 1)
├── ItemMagnet
│   ├── Magnet Distance: 5
│   ├── Magnet Speed: 8
│   └── ...
├── WorldItemUIHandler (nếu có UI components)
│   ├── Auto Disable UI In World: ✅
│   └── Auto Enable UI In Inventory: ✅
└── Tag: "Item"
```

### 2. ItemDictionary Entry:

```
Index: 9
Prefab: [Sword_Pickup.prefab]
Auto ID: 10 (index + 1)
```

### 3. WeaponInfo (Sword.asset):

```
id: 10
itemName: "Sword"
icon: [Sword Icon Sprite]
weaponPrefab: [Sword.prefab] (prefab khi equip)
weaponCooldown: 0.5
weaponDamage: 1
weaponRange: 0
```

### 4. Kết Quả:

- Player đến gần Sword → Sword bay vào player (nếu có ItemMagnet)
- Player chạm Sword → Sword được thêm vào inventory
- Sword xuất hiện trong inventory và có thể drag vào hotbar
- Player equip Sword → Sword xuất hiện và có thể tấn công

---

## Troubleshooting

### Vấn Đề: Sword tự snap vào một vị trí

**Nguyên nhân có thể:**
1. `Rigidbody2D` có constraint (Freeze Position X/Y)
2. `Rigidbody2D` có `Body Type = Static`
3. Có script khác đang lock position
4. Sword có UI components và bị snap vào Canvas

**Giải pháp:**
- Kiểm tra `Rigidbody2D` constraints
- Đổi `Body Type` thành `Dynamic` hoặc `Kinematic`
- Kiểm tra các scripts khác trên sword
- Thêm `WorldItemUIHandler` nếu sword có UI components

### Vấn Đề: Sword không nhặt được

**Nguyên nhân có thể:**
1. Không có tag "Item"
2. `Collider2D` không có `Is Trigger = true`
3. `Item` component không có ID hoặc ID không khớp với `ItemDictionary`
4. Player không có `PlayerItemCollector` component

**Giải pháp:**
- Kiểm tra tag của sword
- Kiểm tra `Collider2D.Is Trigger`
- Kiểm tra ID trong `Item` component và `ItemDictionary`
- Kiểm tra Player có `PlayerItemCollector` không

### Vấn Đề: Sword nhặt được nhưng không vào inventory

**Nguyên nhân có thể:**
1. `ItemDictionary` không có sword với ID tương ứng
2. `InventoryController` không tồn tại trong scene
3. Inventory đã đầy

**Giải pháp:**
- Kiểm tra `ItemDictionary` có sword với ID khớp không
- Kiểm tra scene có `InventoryController` không
- Kiểm tra inventory còn slot trống không

### Vấn Đề: Sword không equip được hoặc không tấn công

**Nguyên nhân có thể:**
1. `WeaponInfo` không có `weaponPrefab` được gán
2. `weaponPrefab` không có `Sword` component
3. `weaponPrefab` không có `Animator` với animation controller

**Giải pháp:**
- Kiểm tra `WeaponInfo` có `weaponPrefab` không
- Kiểm tra `weaponPrefab` có `Sword` component không
- Kiểm tra `weaponPrefab` có `Animator` với animation controller không

---

## So Sánh Với Fire Bow

### Fire Bow Setup:
- ✅ `Item` component với ID và `WeaponInfo`
- ✅ `Collider2D` với `Is Trigger = true`
- ✅ Tag = "Item"
- ✅ `ItemMagnet` component
- ✅ `WorldItemUIHandler` (nếu có UI components)

### Sword Setup (Tương Tự):
- ✅ `Item` component với ID và `WeaponInfo`
- ✅ `Collider2D` với `Is Trigger = true`
- ✅ Tag = "Item"
- ✅ `ItemMagnet` component (khuyến nghị)
- ✅ `WorldItemUIHandler` (nếu có UI components)

---

## Kết Luận

Sau khi setup đúng, sword sẽ:
- ✅ Có thể nhặt được khi player đến gần
- ✅ Tự động bay vào player (nếu có `ItemMagnet`)
- ✅ Được thêm vào inventory
- ✅ Có thể drag/click vào hotbar
- ✅ Có thể equip và sử dụng để tấn công

Chúc bạn setup thành công! ⚔️
