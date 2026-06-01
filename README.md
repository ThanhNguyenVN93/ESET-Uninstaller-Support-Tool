# ESET Safe Mode Uninstaller

Tự động gỡ cài đặt ESET trong Safe Mode, sau đó tự boot lại về Windows bình thường — không cần thao tác tay.

## Luồng hoạt động

```
[Normal Mode]  App chạy → đếm ngược 10s (hoặc click button)
                → Đăng ký RunOnce để tự chạy lại khi vào Safe Mode
                → bcdedit /set {current} safeboot minimal
                → shutdown /r /t 5

[Safe Mode]    Windows thực thi RunOnce → app tự khởi động
                → Kiểm tra ESET còn tồn tại không
                → Nếu còn: chạy uninstaller.exe, chờ hoàn tất
                → bcdedit /deletevalue {current} safeboot
                → shutdown /r /t 5  →  boot về Windows bình thường
```

## Yêu cầu

- Windows 10/11 (x64)
- Chạy với quyền **Administrator** (khai báo trong `app.manifest`)
- File `uninstaller.exe` phải được đặt vào thư mục gốc project trước khi build

## Build

1. Đặt `uninstaller.exe` vào thư mục gốc project
2. Mở bằng Visual Studio 2019+ hoặc MSBuild
3. Build → Release

`uninstaller.exe` được nhúng vào exe khi build — không cần đi kèm file riêng khi phân phối.

## Ghi chú kỹ thuật

| Cơ chế | Chi tiết |
|--------|---------|
| Auto-run Safe Mode | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce` |
| Boot flag | `bcdedit /set {current} safeboot minimal` |
| Revert | `bcdedit /deletevalue {current} safeboot` |
| Extract uninstaller | `%TEMP%\frmesetunins_temp\uninstaller.exe` (tự dọn sau khi chạy) |
| ESET detection | `%ProgramFiles%\ESET`, `%ProgramFiles(x86)%\ESET`, `%ProgramData%\ESET` |
