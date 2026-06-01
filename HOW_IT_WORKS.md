# frmesetunins — Safe Mode Restart Utility

## Tổng quan

Ứng dụng WinForms nhỏ gọn tự động khởi động lại Windows vào **Safe Mode** sau một đếm ngược 10 giây. Được thiết kế để chạy với quyền Administrator.

---

## Luồng hoạt động

```
Khởi động app
    │
    ▼
Form1_Load → SetupUI()
    │
    ├─ Tạo các control UI (lblStatus, lblCountdown, btnAction)
    └─ Khởi động countdownTimer (interval = 1000ms)
            │
            ▼
        CountdownTimer_Tick (mỗi giây)
            │
            ├─ Giảm countdownSeconds
            ├─ Cập nhật lblCountdown
            └─ countdownSeconds == 0 → RestartToSafeMode()

Người dùng click "Restart to Safe Mode"
    └─ BtnAction_Click → RestartToSafeMode()
```

---

## RestartToSafeMode() — Chi tiết

### Bước 1: Xác định thư mục System32

```csharp
string sysDir = Environment.Is64BitProcess
    ? Path.Combine(winDir, "System32")   // process 64-bit
    : Path.Combine(winDir, "Sysnative"); // process 32-bit chạy trên Windows 64-bit
```

> Dùng `Sysnative` thay vì `System32` khi app được compile x86 chạy trên OS 64-bit, để tránh WOW64 redirect sang `SysWOW64`.

### Bước 2: Đặt boot flag Safe Mode

```
bcdedit.exe /set {current} safeboot minimal
```

Ghi vào BCD (Boot Configuration Data) — lần khởi động tiếp theo sẽ vào Safe Mode Minimal.

### Bước 3: Lên lịch restart

```
shutdown.exe /r /t 5
```

Hệ thống sẽ restart sau 5 giây.

### Bước 4: Đóng app

```csharp
Thread.Sleep(2000);
this.Close();
```

---

## Sơ đồ thành phần UI

| Control        | Vị trí       | Mục đích                              |
|----------------|--------------|---------------------------------------|
| `lblStatus`    | (0, 30)      | Hiển thị trạng thái hiện tại          |
| `lblCountdown` | (0, 100)     | Đếm ngược còn bao nhiêu giây          |
| `btnAction`    | (170, 145)   | Trigger restart ngay lập tức          |
| `countdownTimer` | —          | Tick mỗi 1000ms, đếm ngược về 0      |

---

## Yêu cầu

- **Quyền Administrator** — `bcdedit.exe` và `shutdown.exe` yêu cầu elevated privileges.
- **Windows** — dùng các công cụ hệ thống Windows-specific (`bcdedit`, `shutdown`).

---

## Lưu ý quan trọng

Sau khi restart vào Safe Mode, boot flag **vẫn còn trong BCD**. Để thoát Safe Mode sau khi hoàn thành công việc, chạy lệnh sau trong Safe Mode:

```
bcdedit /deletevalue {current} safeboot
```

rồi restart lại bình thường.
