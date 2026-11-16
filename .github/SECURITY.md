# Security Policy / Chính sách bảo mật

## Phạm vi / Scope

Đây là công cụ hỗ trợ gỡ ESET, chạy với quyền Administrator và thao tác với
BCD, registry (RunOnce, Winlogon, SafeBoot) và tiến trình hệ thống. This tool
runs elevated and touches BCD, the registry and system processes.

## Báo lỗi bảo mật / Reporting a vulnerability

Vui lòng **không** mở issue công khai cho lỗ hổng bảo mật. Thay vào đó, dùng
**GitHub Security Advisories** ("Report a vulnerability" ở tab Security của repo)
hoặc liên hệ riêng với chủ repo.

Please do **not** open a public issue for security vulnerabilities. Use GitHub's
**private vulnerability reporting** (Security tab → "Report a vulnerability")
instead.

Khi báo, nếu có thể hãy kèm: phiên bản tool, phiên bản Windows, các bước tái
hiện, và gói log chẩn đoán (nút "Gom log chẩn đoán" trong app).

## Nguyên tắc an toàn đã áp dụng / Safety measures in place

- Chỉ chạy uninstaller đã **xác minh chữ ký ESET**; log SHA-256.
- Mật khẩu autologon lưu bằng **LSA secret** (mã hóa), tự xóa sau một vòng.
- **Không** tự lan qua mạng; mọi lần chạy do người dùng chủ động.
- Tạo **System Restore point** trước khi gỡ; dọn dẹp mọi trigger khi xong.
