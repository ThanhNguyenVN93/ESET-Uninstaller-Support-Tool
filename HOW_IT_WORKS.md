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

## Cơ chế tự khởi động lại trong Safe Mode (2 lớp dự phòng)

Windows **bỏ qua** các entry `Run`/`RunOnce` khi boot vào Safe Mode, trừ khi tên value có tiền tố `*`. Vì vậy app đăng ký **2 trigger độc lập** để chắc chắn được chạy lại:

1. **RunOnce (`*FrmEseUnins`)** — tiền tố `*` bắt buộc để entry được thực thi trong Safe Mode (chạy ở bước `userinit`, trước shell).
2. **AlternateShell fallback** — app tự đăng ký làm *shell* của Safe Mode (`HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\AlternateShell` + `bcdedit /set {current} safebootalternateshell yes`). Nếu RunOnce bị policy/AV chặn, app vẫn khởi động vì nó chính là shell.

Giá trị `AlternateShell` gốc được sao lưu vào `HKLM\SOFTWARE\frmesetunins\OrigAlternateShell` để khôi phục sau.

Một **mutex** (`Global\frmesetunins_safemode`) đảm bảo chỉ một instance thực thi gỡ cài đặt khi cả hai trigger cùng kích hoạt; instance còn lại giữ phiên sống đến khi máy restart.

### Chạy ESET Uninstaller (unattended)

ESET Uninstaller **mặc định tương tác** (hỏi gõ `y`, chọn số…). App chạy nó với cờ **`/force`** để gỡ toàn bộ ESET **không cần nhập liệu**, kèm:

- **Timeout 5 phút** — nếu uninstaller treo sẽ bị `Kill()`, tránh kẹt Safe Mode vĩnh viễn.
- **Ghi log**: trạng thái app → `%TEMP%\ESETUninstallerSupport.log` (tự xoay khi >1MB); log của ESET (`~ESETUninstaller.log`) được sao chép ra `%TEMP%\ESETUninstallerSupport_ESETLog_*.log`.
- **Không có hộp thoại chặn** trong Safe Mode (vì unattended không có người bấm); luôn dọn dẹp + restart kể cả khi lỗi.
- Sau khi gỡ, app **kiểm tra lại** ESET còn không và ghi kết quả vào log.

### Các tính năng khác

- **Tải uninstaller mới nhất**: ở chế độ bình thường (còn mạng), app tải bản `latest` từ ESET về `%ProgramData%\ESETUninstallerSupport\uninstaller.exe` và kiểm tra hợp lệ (PE/MZ, >50KB, chữ ký ESET). Safe Mode minimal không có mạng nên dùng bản này; nếu không tải được thì fallback bản tích hợp.
- **Phát hiện ESET** qua 3 lớp: service (`ekrn`/`epfw`/`ehdrv`) → registry Uninstall (cả 64-bit & 32-bit) → thư mục.
- **AutoLogonCount=1**: lớp bảo hiểm để Windows tự tắt autologon sau đúng 1 lần đăng nhập.
- **Màn hình kết quả**: lần boot bình thường kế tiếp, app tự chạy (RunOnce `--result`) hiển thị thành công / cảnh báo / lỗi + nút mở log.
- **Nút "Hủy bỏ"** trên màn đếm ngược để dừng trước khi restart.
- UI **tiếng Việt có dấu** (compiler đọc source UTF-8 nhờ `<CodePage>65001</CodePage>`).
- **Song ngữ Việt / Anh** ([Loc.cs](Loc.cs)): tự chọn theo ngôn ngữ Windows, có nút chuyển đổi trực tiếp trên UI.
- **Nút "Báo lỗi / Góp ý"** mở trang Issues của repo trên GitHub.
- **Nút "Hủy khởi động lại"**: sau khi đã lên lịch, còn ~20 giây để hủy (`shutdown /a`) và hoàn tác toàn bộ thay đổi.
- **Chế độ silent/CLI**: `ESET Uninstaller Support Tool.exe --auto` (không UI, tự lên lịch); kèm `--password=<mk>` để bật autologon không giám sát. Mã thoát: 0 = gỡ thành công, 1 = vẫn còn ESET, 2 = lỗi, 3 = không có ESET.
- **Sao lưu cấu hình mạng** (`netsh -c interface dump`) trước khi gỡ, lưu tại `%ProgramData%\ESETUninstallerSupport\`; khôi phục bằng `netsh -f <file>` (có nút "Khôi phục mạng").
- **Nhắc export thiết lập ESET** trước khi gỡ (không tự khôi phục khi cài lại).
- **Kiểm tra uninstaller** (PE/MZ, ≥50KB) trước khi chạy, và **xác minh đã xóa** mật khẩu LSA sau khi tắt autologon.
- **Tự copy về đĩa cục bộ** (`%ProgramData%\ESETUninstallerSupport\ESETUninstallerSupport.exe`) và trỏ trigger vào bản này → **chạy được từ network share / USB** (Safe Mode minimal không có mạng). Đây là copy trên **cùng máy**, không phải lan mạng.
- **Chế độ `--select`** (hoặc tick ô trên UI): chạy ESET Uninstaller ở **menu tương tác** để chọn sản phẩm cần gỡ, thay vì `/force` gỡ tất cả.
- **Tự thử lại 2 lần** trong Safe Mode nếu `/force` chưa gỡ sạch (KB nói đôi khi cần chạy 2 lần).
- **Recovery guard**: nếu lần arm trước không vào được Safe Mode (hủy/treo), lần boot bình thường kế tiếp tự dọn cờ safeboot + autologon còn sót.

## Chế độ & khái niệm quan trọng

- **Gỡ được cả khi bị khóa mật khẩu**: chạy trong Safe Mode, self-defense của ESET không hoạt động nên uninstaller gỡ được **kể cả khi settings bị bảo vệ bằng mật khẩu** (hợp lệ cho trường hợp mất mật khẩu admin). Nguồn: forum ESET.
- **`/nosafemode` (fallback)**: khi **không thể vào Safe Mode**, chạy `...exe --nosafemode` để gỡ ngay trong Windows bình thường. Bắt buộc **TẮT Self-Defense/HIPS** trong ESET trước (và nhập mật khẩu nếu được hỏi). Trên UI có nút "Thử gỡ ngay (không cần Safe Mode)".
- **Loop-guard**: đếm số lần vào Safe Mode (`Attempts`); quá **3 lần** chưa xong thì **dừng hẳn** (báo lỗi, dọn dẹp) thay vì reboot vô tận. Thực tế người dùng từng bị kẹt vòng lặp Safe Mode (msconfig không lưu được) — tool này tránh bằng `bcdedit` + guard.
- **Dọn leftover (tùy chọn)**: `--cleanleftovers` hoặc tick ô tương ứng → nếu uninstaller để sót, xóa service (`ekrn`/`epfw`/`ehdrv`...) + thư mục ESET. Mặc định **TẮT** vì có rủi ro; mọi thao tác đều ghi log.
- **Pre-flight**: trước khi vào Safe Mode, nhắc **Wi-Fi có thể không chạy** (chuẩn bị mạng dây/truy cập từ xa) và **sao lưu dữ liệu**.

## So sánh với cách chính thức

| Tình huống | Nên dùng |
|---|---|
| Endpoint **được quản lý** bởi ESET PROTECT | **ESET PROTECT** push-uninstall (KB7724, hỗ trợ `PASSWORD=`) |
| Máy **không quản lý** / mất mật khẩu / server quản lý đã chết / uninstall thường thất bại | **Công cụ này** (Safe Mode uninstaller) |

## Triển khai qua Network Share

Đặt `ESET Uninstaller Support Tool.exe` lên thư mục chia sẻ; người dùng/IT chạy trực tiếp. App tự copy về cục bộ nên relaunch trong Safe Mode vẫn hoạt động. Cho deploy hàng loạt: `...exe --auto [--select] [--password=<mk>]` (xem mã thoát ở trên).

Khi hoàn tất, `RemoveSafeBootAndRestart()` dọn **toàn bộ** trigger để tránh vòng lặp (khôi phục AlternateShell, xóa RunOnce, xóa cờ BCD) rồi restart.

### Tự động đăng nhập (tùy chọn)

Để Safe Mode chạy **hoàn toàn tự động** (không cần gõ mật khẩu), app hỏi mật khẩu Windows trước khi restart và bật autologon ([AutoLogon.cs](AutoLogon.cs)):

- Mật khẩu được lưu dưới dạng **LSA secret** (`DefaultPassword`) — mã hóa bởi OS, **không** ghi plaintext vào registry (giống Sysinternals Autologon).
- Đặt `AutoAdminLogon=1`, `DefaultUserName`, `DefaultDomainName` trong `Winlogon`.
- Autologon **luôn bị gỡ** trong bước dọn dẹp Safe Mode; nếu run bị crash, lần boot bình thường kế tiếp cũng tự gỡ (cờ `AutoLogonActive`). → mật khẩu chỉ tồn tại trong đúng 1 vòng.
- Bấm **"Bỏ qua"** ở hộp thoại nếu muốn đăng nhập thủ công.

---

## Lưu ý quan trọng

Nếu cần khôi phục thủ công (ví dụ app lỗi giữa chừng), chạy trong Safe Mode:

```
bcdedit /deletevalue {current} safeboot
bcdedit /deletevalue {current} safebootalternateshell
reg add "HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot" /v AlternateShell /t REG_SZ /d cmd.exe /f
```

rồi restart lại bình thường.
