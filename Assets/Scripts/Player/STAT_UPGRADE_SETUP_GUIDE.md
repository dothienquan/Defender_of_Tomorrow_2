# Hướng Dẫn Setup Hệ Thống Nâng Cấp Chỉ Số

## Tổng Quan

Hệ thống này cho phép người chơi sử dụng upgrade points để nâng cấp các chỉ số:
- **Máu tối đa** (Max Health)
- **Stamina tối đa** (Max Stamina)
- **Tốc độ di chuyển** (Movement Speed)
- **Tốc độ hồi stamina** (Stamina Regen Speed - giảm thời gian chờ giữa các lần hồi)

Mỗi khi lên cấp, người chơi nhận **2 upgrade points**.

## Các Component Đã Tạo

1. **PlayerStatUpgradeSystem.cs** - Quản lý upgrade points và stat upgrades
2. **StatUpgradeUI.cs** - UI để hiển thị và nâng cấp stats
3. Các method upgrade đã được thêm vào:
   - `PlayerHealth.UpgradeMaxHealth(int amount)`
   - `Stamina.UpgradeMaxStamina(int amount)`
   - `Stamina.UpgradeRegenSpeed(float amount)` - Giảm thời gian hồi stamina
   - `PlayerController.UpgradeMovementSpeed(float amount)`

## Bước 1: Setup PlayerStatUpgradeSystem

1. Thêm component `PlayerStatUpgradeSystem` vào Player GameObject
2. Trong Inspector, kéo các reference:
   - **Level System**: Kéo `PlayerLevelSystemLinear` component
   - **Player Health**: Kéo `PlayerHealth` component
   - **Stamina**: Kéo `Stamina` component
   - **Player Controller**: Kéo `PlayerController` component
   - **Mana**: Kéo `Mana` component (nếu đã có)

3. Tùy chỉnh giá trị upgrade (tùy chọn):
   - **Max Health Per Upgrade**: Mỗi điểm nâng cấp tăng bao nhiêu máu (mặc định: 1)
   - **Max Stamina Per Upgrade**: Mỗi điểm nâng cấp tăng bao nhiêu stamina (mặc định: 1)
   - **Movement Speed Per Upgrade**: Mỗi điểm nâng cấp tăng bao nhiêu tốc độ (mặc định: 0.5)
   - **Stamina Regen Speed Per Upgrade**: Mỗi điểm nâng cấp giảm bao nhiêu giây thời gian hồi stamina (mặc định: 0.2)

## Bước 2: Setup UI

### 2.1. Tạo UI Panel

1. Tạo một Canvas mới hoặc sử dụng Canvas hiện có
2. Tạo một Panel làm container cho upgrade UI
3. Thêm component `StatUpgradeUI` vào Panel này

### 2.2. Setup UI Elements

Trong `StatUpgradeUI` component, kéo các UI elements:

**Upgrade Points Display:**
- **Upgrade Points Text**: TextMeshProUGUI hiển thị số điểm nâng cấp còn lại

**Stat Display Texts:**
- **Health Value Text**: TextMeshProUGUI hiển thị "Máu tối đa: X"
- **Stamina Value Text**: TextMeshProUGUI hiển thị "Stamina tối đa: X"
- **Movement Speed Value Text**: TextMeshProUGUI hiển thị "Tốc độ di chuyển: X"
- **Stamina Regen Value Text**: TextMeshProUGUI hiển thị "Thời gian hồi stamina: Xs"

**Upgrade Count Texts (tùy chọn):**
- **Health Upgrade Count Text**: TextMeshProUGUI hiển thị "+X" số lần đã nâng cấp
- **Stamina Upgrade Count Text**: TextMeshProUGUI hiển thị "+X"
- **Movement Speed Upgrade Count Text**: TextMeshProUGUI hiển thị "+X"
- **Stamina Regen Upgrade Count Text**: TextMeshProUGUI hiển thị "+X"

**Upgrade Buttons:**
- **Upgrade Health Button**: Button để nâng cấp máu
- **Upgrade Stamina Button**: Button để nâng cấp stamina
- **Upgrade Movement Speed Button**: Button để nâng cấp tốc độ
- **Upgrade Stamina Regen Button**: Button để nâng cấp tốc độ hồi stamina

**References:**
- **Upgrade System**: Kéo `PlayerStatUpgradeSystem` component
- **Upgrade Panel**: Kéo Panel chứa UI này
- **Toggle Key**: Phím để mở/đóng panel (mặc định: U)

### 2.3. Ví Dụ UI Layout

```
Upgrade Panel
├── Upgrade Points Text: "Điểm nâng cấp: 2"
├── Health Section
│   ├── Health Value Text: "Máu tối đa: 10"
│   ├── Health Upgrade Count Text: "+3"
│   └── Upgrade Health Button
├── Stamina Section
│   ├── Stamina Value Text: "Stamina tối đa: 5"
│   ├── Stamina Upgrade Count Text: "+2"
│   └── Upgrade Stamina Button
├── Movement Speed Section
│   ├── Movement Speed Value Text: "Tốc độ di chuyển: 6.5"
│   ├── Movement Speed Upgrade Count Text: "+1"
│   └── Upgrade Movement Speed Button
└── Stamina Regen Section
    ├── Stamina Regen Value Text: "Thời gian hồi stamina: 2.4s"
    ├── Stamina Regen Upgrade Count Text: "+2"
    └── Upgrade Stamina Regen Button
```

## Bước 3: Tích Hợp với Level System

Hệ thống đã tự động tích hợp với `PlayerLevelSystemLinear`:
- Khi player lên cấp, `OnLevelUp` event được gọi
- `PlayerStatUpgradeSystem` lắng nghe event này và tự động cấp 2 upgrade points

**Lưu ý:** Nếu bạn muốn disable hệ thống tự động upgrade cũ (`LevelUpApplier`), bạn có thể:
- Disable component `LevelUpApplier` trong Inspector
- Hoặc comment/uncomment code trong `LevelUpApplier.cs`

## Bước 4: Test

1. Chạy game và lên cấp để nhận upgrade points
2. Nhấn phím **U** (hoặc phím bạn đã set) để mở upgrade panel
3. Click các button để nâng cấp stats
4. Kiểm tra xem stats có được cập nhật đúng không

## API Reference

### PlayerStatUpgradeSystem

**Properties:**
- `int AvailableUpgradePoints` - Số điểm nâng cấp còn lại
- `int MaxHealthUpgrades` - Số lần đã nâng cấp máu
- `int MaxStaminaUpgrades` - Số lần đã nâng cấp stamina
- `int MovementSpeedUpgrades` - Số lần đã nâng cấp tốc độ
- `int StaminaRegenSpeedUpgrades` - Số lần đã nâng cấp tốc độ hồi stamina

**Methods:**
- `bool UpgradeMaxHealth()` - Nâng cấp máu tối đa
- `bool UpgradeMaxStamina()` - Nâng cấp stamina tối đa
- `bool UpgradeMovementSpeed()` - Nâng cấp tốc độ di chuyển
- `bool UpgradeStaminaRegenSpeed()` - Nâng cấp tốc độ hồi stamina (giảm thời gian chờ)
- `int GetCurrentMaxHealth()` - Lấy máu tối đa hiện tại
- `int GetCurrentMaxStamina()` - Lấy stamina tối đa hiện tại
- `float GetCurrentMovementSpeed()` - Lấy tốc độ di chuyển hiện tại
- `float GetCurrentStaminaRegenSpeed()` - Lấy thời gian hồi stamina hiện tại (giây)

**Events:**
- `OnUpgradePointsChanged(int points)` - Khi số điểm nâng cấp thay đổi
- `OnStatsUpgraded()` - Khi stats được nâng cấp

### StatUpgradeUI

**Methods:**
- `void TogglePanel()` - Mở/đóng upgrade panel
- `void OpenPanel()` - Mở panel
- `void ClosePanel()` - Đóng panel

## Troubleshooting

### Upgrade points không được cấp khi lên cấp
- Kiểm tra `PlayerStatUpgradeSystem` có được attach vào Player không
- Kiểm tra `Level System` reference có được set đúng không
- Kiểm tra `PlayerLevelSystemLinear` có gọi `OnLevelUp` event không

### UI không hiển thị đúng
- Kiểm tra tất cả các reference trong `StatUpgradeUI` đã được set chưa
- Kiểm tra `Upgrade System` reference có trỏ đúng `PlayerStatUpgradeSystem` không

### Stats không được cập nhật sau khi upgrade
- Kiểm tra các component reference (PlayerHealth, Stamina, PlayerController) có được set đúng không
- Kiểm tra các method upgrade có được gọi đúng không

## Lưu Ý

- Hệ thống này sử dụng **event-driven** để tránh Update() mỗi frame
- Tất cả các upgrade được **cache** để tránh GetComponent trong Update
- UI sử dụng **DOTween** cho animation mượt mà
- Hệ thống hỗ trợ **save/load** thông qua PlayerPrefs (có thể mở rộng)

