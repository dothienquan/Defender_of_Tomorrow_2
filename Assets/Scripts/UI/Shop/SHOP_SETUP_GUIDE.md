# Hướng Dẫn Setup Shop Panel

## Tổng Quan

Hướng dẫn này sẽ giúp bạn setup một shop panel đơn giản để mua vũ khí bằng gold coin.

---

## Bước 1: Setup EconomyManager

### 1.1. Kiểm Tra EconomyManager

- `EconomyManager` đã được cập nhật để:
  - Lưu/load gold coin
  - Có methods `AddGold()`, `SpendGold()`, `SetGold()`
  - Tự động cập nhật UI text

### 1.2. Setup Gold Text UI

1. Tạo GameObject với tên **"Gold Amount Text"** (chính xác tên này)
2. Add Component → `TextMeshPro - Text (UI)`
3. Đặt trong Canvas (thường là HUD hoặc UI overlay)
4. `EconomyManager` sẽ tự động tìm và cập nhật text này

---

## Bước 2: Thêm Giá Cho Vũ Khí

### 2.1. Mở WeaponInfo ScriptableObject

- Ví dụ: `Assets/Scriptable Objects/Sword.asset`
- Hoặc: `Assets/Scriptable Objects/Fire Sword.asset`

### 2.2. Set Shop Price

1. Trong Inspector, tìm section **"Shop Settings"**
2. Set **Shop Price**:
   - `0` = không thể mua trong shop
   - `> 0` = giá mua (gold coin)
   - Ví dụ: `100`, `250`, `500`, etc.

### 2.3. Lưu Ý

- Chỉ weapons có `shopPrice > 0` mới xuất hiện trong shop
- Có thể set giá khác nhau cho mỗi weapon

---

## Bước 3: Tạo Shop Panel UI

### 3.1. Tạo Shop Panel GameObject

1. Trong Canvas, tạo GameObject mới: **"ShopPanel"**
2. Add Component → `RectTransform` (tự động có)
3. Setup:
   - **Anchor**: Center (0.5, 0.5)
   - **Size**: (600, 400) hoặc tùy chỉnh
   - **Position**: (0, 0)

### 3.2. Tạo ScrollRect và Content Container

1. Tạo GameObject con: **"ScrollView"**
2. Add Component → `ScrollRect`
3. Setup ScrollRect:
   - **Content**: Sẽ được set sau
   - **Horizontal**: ❌ Tắt (chỉ scroll dọc)
   - **Vertical**: ✅ Bật
   - **Movement Type**: Elastic
   - **Scroll Sensitivity**: 40

4. Tạo GameObject con của ScrollView: **"Content"** (hoặc "ShopSlotsContainer")
5. Add Component → `RectTransform`
6. Add Component → `Vertical Layout Group` hoặc `Grid Layout Group`
7. Setup Layout Group:
   - **Spacing**: 10
   - **Padding**: 10
   - **Child Alignment**: Upper Center
   - **Child Force Expand**: Width = ✅, Height = ❌

8. ⚠️ **QUAN TRỌNG**: Add Component → `Content Size Fitter`
   - **Horizontal Fit**: Unconstrained
   - **Vertical Fit**: Preferred Size (tự động tăng height theo children)

9. Setup Content RectTransform:
   - **Anchor**: Top-Left (0, 1)
   - **Pivot**: (0.5, 1)
   - **Width**: Match parent (stretch) - hoặc để Layout Group tự tính
   - **Height**: Auto (ContentSizeFitter sẽ tự động tính)

9. Quay lại ScrollView:
   - **Content**: Kéo `Content` GameObject vào đây

10. Tạo Scrollbar (optional nhưng khuyến nghị):
    - Tạo GameObject con của ScrollView: **"Scrollbar Vertical"**
    - Add Component → `Scrollbar`
    - Setup:
      - **Direction**: Bottom To Top
    - Quay lại ScrollView:
      - **Vertical Scrollbar**: Kéo Scrollbar vào đây

### 3.3. Tạo Shop Slot Prefab

1. Tạo GameObject: **"ShopSlot"**
2. Add Component → `RectTransform`
3. Add Component → `ShopSlot` (script)
4. Add Component → `Button` (optional, nếu muốn click vào slot để mua)
5. Setup UI:
   - **Icon** (Image): Hiển thị weapon icon
   - **Name** (TextMeshPro): Hiển thị weapon name
   - **Price** (TextMeshPro): Hiển thị price
   - **BuyButton** (Button): Button mua (optional)

#### Cấu Trúc ShopSlot Prefab:

```
ShopSlot (GameObject)
├── RectTransform
├── ShopSlot (Component)
├── Button (Component, optional)
├── Icon (Image)
│   └── Sprite: [Weapon Icon]
├── Name (TextMeshPro)
│   └── Text: "Weapon Name"
├── Price (TextMeshPro)
│   └── Text: "100"
└── BuyButton (Button, optional)
    └── OnClick: [ShopSlot.OnBuyButtonClicked]
```

### 3.4. Tạo Gold Display

1. Tạo GameObject: **"GoldDisplay"**
2. Add Component → `TextMeshPro - Text (UI)`
3. Đặt trong ShopPanel
4. Reference vào `ShopController.goldText`

### 3.5. Tạo Close Button

1. Tạo GameObject: **"CloseButton"**
2. Add Component → `Button`
3. Add Component → `TextMeshPro - Text (UI)` (cho text "Close" hoặc "X")
4. Reference vào `ShopController.closeButton`

---

## Bước 4: Setup ShopController

### 4.1. Add ShopController Component

1. Chọn **ShopPanel** GameObject
2. Add Component → `ShopController`

### 4.2. Cấu Hình ShopController

1. **Shop Slots Parent**: Kéo `Content` (hoặc `ShopSlotsContainer`) vào đây
   - ⚠️ **QUAN TRỌNG**: Đây phải là Content của ScrollRect
2. **Shop Slot Prefab**: Kéo `ShopSlot` prefab vào đây
3. **Gold Text**: Kéo `GoldDisplay` TextMeshPro vào đây
4. **Close Button**: Kéo `CloseButton` Button vào đây
5. **Scroll Rect**: Kéo `ScrollView` GameObject (có ScrollRect component) vào đây
   - Optional: Nếu không gán, script sẽ tự động tìm ScrollRect trong children

### 4.3. Thêm Weapons Vào Shop

1. Trong **Shop Weapons** list:
   - **Size**: Số lượng weapons muốn bán
   - **Element 0, 1, 2...**: Kéo các `WeaponInfo` ScriptableObject vào đây
     - Ví dụ: `Sword.asset`, `Fire Sword.asset`, `Fire Bow.asset`, etc.

### 4.4. Lưu Ý

- Chỉ weapons có `shopPrice > 0` mới được tạo slot
- Weapons có `shopPrice = 0` sẽ bị bỏ qua

---

## Bước 5: Setup Save/Load Gold

### 5.1. Kiểm Tra SaveController

- `SaveController` đã được cập nhật để:
  - Lưu gold coin khi save game
  - Load gold coin khi load game
  - Tự động gọi `EconomyManager.SetGold()` khi load

### 5.2. Test Save/Load

1. Chơi game và nhặt gold coin
2. Save game
3. Load game
4. Kiểm tra gold coin có được restore không

---

## Bước 6: Tích Hợp Vào Game

### 6.1. Tạo NPC Shop (Khuyến Nghị)

**Cách 1: Sử dụng ShopNPC Script (Đơn giản nhất)**

1. Chọn NPC GameObject trong scene
2. Add Component → `ShopNPC`
3. Cấu hình:
   - **Shop Controller**: Kéo ShopPanel (có ShopController component) vào đây
     - Hoặc để `Auto Find Shop = true` để tự động tìm
   - **Interact Key**: KeyCode.F (mặc định)
   - **Interaction UI**: Kéo InteractionUI GameObject vào đây (optional)
   - **Interaction Text**: "Mở Shop" (hoặc tên NPC)

4. Đảm bảo NPC có:
   - **Collider2D** với **Is Trigger = ✅**
   - **Tag = "Player"** không cần (NPC không phải Player)

**Cách 2: Sử dụng ShopInteractable (Tương thích với PlayerInteraction system)**

1. Chọn NPC GameObject trong scene
2. Add Component → `ShopInteractable`
3. Cấu hình:
   - **Shop Controller**: Kéo ShopPanel vào đây
   - **Display Name**: "Shop Keeper" (hiển thị khi player đến gần)

4. Đảm bảo Player có `PlayerInteraction` component:
   - Player GameObject → Add Component → `PlayerInteraction`
   - **Interaction UI**: Kéo InteractionUI GameObject vào đây

**Cách 3: Button Trong Menu**

1. Tạo button "Shop" trong main menu hoặc pause menu
2. OnClick → `ShopController.OpenShop()`

### 6.2. Setup Input System (Nếu Dùng Unity Input System)

1. Tạo Input Action: "OpenShop"
2. Bind key (ví dụ: "B" hoặc "Tab")
3. Trong PlayerController hoặc InputHandler:

```csharp
private void OnEnable()
{
    playerControls.UI.OpenShop.performed += _ => OpenShop();
}

private void OpenShop()
{
    ShopController shop = FindFirstObjectByType<ShopController>();
    if (shop != null)
    {
        shop.OpenShop();
    }
}
```

---

## Checklist Setup Shop

- [ ] EconomyManager được setup và có gold text UI
- [ ] WeaponInfo ScriptableObjects có `shopPrice > 0` cho weapons muốn bán
- [ ] ShopPanel GameObject được tạo với ShopController component
- [ ] ShopSlot prefab được tạo với ShopSlot component
- [ ] ShopController được cấu hình:
  - [ ] Shop Slots Parent
  - [ ] Shop Slot Prefab
  - [ ] Gold Text
  - [ ] Close Button
  - [ ] Shop Weapons list (có ít nhất 1 weapon)
- [ ] SaveController lưu/load gold coin đúng
- [ ] Có cách mở shop (NPC, button, hoặc input)

---

## Ví Dụ Setup

### 1. ShopPanel Hierarchy:

```
ShopPanel (GameObject với ShopController)
├── Background (Image)
├── Title (TextMeshPro) - "SHOP"
├── GoldDisplay (TextMeshPro) - "Gold: 000"
├── ScrollView (GameObject với ScrollRect)
│   ├── Viewport (Image với Mask component)
│   │   └── Content (RectTransform + Vertical Layout Group)
│   │       ├── ShopSlot (Instance 1)
│   │       ├── ShopSlot (Instance 2)
│   │       └── ShopSlot (Instance 3)
│   └── Scrollbar Vertical (Scrollbar)
└── CloseButton (Button)
```

**Lưu Ý**: Unity tự động tạo Viewport khi thêm ScrollRect. Nếu không có, tạo thủ công:
1. Tạo GameObject con của ScrollView: **"Viewport"**
2. Add Component → `RectTransform`
3. Add Component → `Image` (optional, để có background)
4. Add Component → `Mask` (quan trọng! Để clip content)
5. Setup Viewport RectTransform:
   - **Anchor**: Stretch-Stretch (0, 0, 1, 1)
   - **Offset**: (0, 0, 0, 0)
6. Di chuyển `Content` vào trong `Viewport`
7. Quay lại ScrollView:
   - **Viewport**: Kéo `Viewport` vào đây

### 2. ShopController Settings:

```
Shop Slots Parent: [ShopSlotsContainer]
Shop Slot Prefab: [ShopSlot.prefab]
Gold Text: [GoldDisplay]
Close Button: [CloseButton]
Shop Weapons:
  - Element 0: [Sword.asset] (Price: 100)
  - Element 1: [Fire Sword.asset] (Price: 250)
  - Element 2: [Fire Bow.asset] (Price: 300)
```

### 3. Kết Quả:

- Shop hiển thị 3 weapons với giá tương ứng
- Player có thể click vào slot để mua
- Gold được trừ khi mua thành công
- Item được thêm vào inventory
- Gold được lưu khi save game

---

## Troubleshooting

### Vấn Đề: Shop không hiển thị weapons

**Nguyên nhân có thể:**
1. Weapons trong list có `shopPrice = 0`
2. ShopSlotPrefab không có ShopSlot component
3. ShopSlotsParent không được gán

**Giải pháp:**
- Kiểm tra `shopPrice > 0` trong WeaponInfo
- Đảm bảo ShopSlotPrefab có ShopSlot component
- Gán ShopSlotsParent trong ShopController

### Vấn Đề: Không mua được weapon

**Nguyên nhân có thể:**
1. Không đủ gold
2. Inventory đã đầy
3. ItemDictionary không có weapon prefab

**Giải pháp:**
- Kiểm tra gold hiện tại
- Kiểm tra inventory còn slot trống không
- Kiểm tra ItemDictionary có weapon prefab với WeaponInfo tương ứng

### Vấn Đề: Gold không được lưu

**Nguyên nhân có thể:**
1. SaveController chưa được cập nhật
2. EconomyManager.Instance là null

**Giải pháp:**
- Kiểm tra SaveController có code lưu gold không
- Kiểm tra EconomyManager có được khởi tạo không (Singleton)

### Vấn Đề: ScrollRect không scroll được bằng mouse wheel hoặc tự reset về vị trí cũ

**Nguyên nhân có thể:**
1. Content không có `ContentSizeFitter` → size không được tính đúng
2. Content không có `VerticalLayoutGroup` → items không được sắp xếp
3. Viewport không có `Mask` component → content không bị clip
4. Content size nhỏ hơn Viewport size → không cần scroll
5. ScrollRect Viewport không được gán đúng

**Giải pháp:**
- ✅ Đảm bảo Content có `ContentSizeFitter` với **Vertical Fit = Preferred Size**
- ✅ Đảm bảo Content có `VerticalLayoutGroup` để sắp xếp items
- ✅ Đảm bảo Viewport có `Mask` component để clip content
- ✅ Kiểm tra Content size > Viewport size (cần có nhiều items hơn để scroll)
- ✅ Đảm bảo ScrollRect → **Viewport** được gán đúng
- ✅ Đảm bảo ScrollRect → **Content** được gán đúng
- ✅ Kiểm tra Canvas có `GraphicRaycaster` component (cần để nhận mouse input)
- ✅ Đảm bảo ShopSlot không có drag handlers conflict với ScrollRect

---

## Kết Luận

Sau khi setup đúng, shop sẽ:
- ✅ Hiển thị weapons có thể mua
- ✅ Cho phép mua weapons bằng gold coin
- ✅ Tự động thêm items vào inventory
- ✅ Lưu/load gold coin khi save/load game

Chúc bạn setup thành công! 🛒💰
