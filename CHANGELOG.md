# Changelog

Tất cả thay đổi đáng chú ý của dự án. Định dạng theo [Keep a Changelog](https://keepachangelog.com/),
phiên bản theo [SemVer](https://semver.org/lang/vi/).

## [Unreleased]

### Added — Vòng hoàn thiện 2
- **Overall timeout**: bỏ qua yêu cầu "quá hạn" (>6 giờ) + loop-guard sẵn có.
- **Kiểm tra dung lượng đĩa** (<100MB) trước khi extract uninstaller.
- **Log rotation** (xoay khi >1MB) để log không phình vô hạn.
- **Xác minh chữ ký bản nhúng** lúc khởi động (nền, ghi log).
- **Thanh tiến trình (marquee)** khi chạy lâu; **tooltip** cho các tùy chọn.
- **Gom log chẩn đoán 1-click** → file ZIP (log + ESET log + network + môi trường).
- **Unit test** (MSTest) + chạy trong CI; **.editorconfig**, **SECURITY.md**,
  mẫu Issue/PR của GitHub.

### Added — Độ tin cậy & hoàn thiện
- Bắt mọi lỗi không mong muốn (global exception handler) → ghi log + hộp thoại thân thiện thay vì crash.
- Chống chạy nhiều bản (single-instance) ở chế độ giao diện.
- Kiểm tra quyền Administrator + thông báo rõ ràng.
- Thông tin phiên bản/công ty/bản quyền trong assembly; hiện phiên bản trên tiêu đề; nút **Giới thiệu**.
- Tham số `--help` hiển thị hướng dẫn dòng lệnh.
- **High-DPI aware** (manifest) để UI sắc nét trên màn 4K/scaled.
- Tạo **System Restore point** trước khi gỡ (nếu System Protection bật).
- Nút **Mở thư mục log**; **ghi nhớ ngôn ngữ** đã chọn.

### Added — Tính năng gỡ
- Fix gốc: RunOnce `*` + AlternateShell để tự chạy lại trong Safe Mode.
- Tải ESET Uninstaller mới nhất + **xác minh chữ ký ESET** + SHA-256; fallback bản nhúng.
- `/force` không giám sát, tự thử lại 2 lần; `--select` chọn sản phẩm thủ công.
- `--nosafemode` gỡ ngay ở Normal Mode; **loop-guard** dừng sau 3 lần.
- Tự copy về đĩa cục bộ (chạy từ network share/USB); chế độ silent `--auto` + mã thoát.
- Autologon qua LSA secret (tự xóa); sao lưu/khôi phục mạng; dọn leftover (opt-in).
- Song ngữ Việt/Anh; nút Hủy/Abort; màn hình kết quả + báo lỗi GitHub.

### CI/CD
- Upload artifact mỗi build; tạo GitHub Release khi push tag `v*`.

[Unreleased]: https://github.com/ThanhNguyenVN93/ESET-Uninstaller-Support-Tool/commits
