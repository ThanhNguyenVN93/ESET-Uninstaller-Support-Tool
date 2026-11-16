# ESET Uninstaller Support Tool

**🇻🇳 Tiếng Việt** · [🇬🇧 English below](#-english)

Công cụ tự động gỡ ESET trong **Safe Mode** rồi tự khởi động lại về Windows bình thường — hạn chế tối đa thao tác tay. Dùng chính **ESET Uninstaller** chính thức (nhúng sẵn + tùy chọn tải bản mới nhất), **không** dịch ngược sản phẩm ESET.

## Tính năng

- 🔁 **Tự chạy lại trong Safe Mode** với 2 lớp độc lập: `RunOnce` (tiền tố `*`) + đăng ký làm **AlternateShell**.
- 🔐 **Tự động đăng nhập** (tùy chọn): mật khẩu lưu bằng **LSA secret** mã hóa, tự xóa sau 1 vòng.
- 🌐 **Chạy từ network share / USB**: app tự copy về đĩa cục bộ trước khi vào Safe Mode (Safe Mode không có mạng).
- ⬇️ **Tải ESET Uninstaller mới nhất** khi online, **xác minh chữ ký số ESET** + ghi SHA-256; offline thì dùng bản nhúng.
- 🧹 **Gỡ không giám sát** bằng `/force`, tự **thử lại 2 lần**; hoặc `--select` để chọn sản phẩm thủ công.
- 🛟 **An toàn**: nút **Hủy khởi động lại**, recovery guard tự dọn cờ còn sót, log chi tiết + màn hình kết quả.
- 🌏 **Song ngữ Việt/Anh**, chuyển đổi trực tiếp. 🐞 Nút **Báo lỗi** mở GitHub Issues.

## Luồng hoạt động

```
[Normal]  Đếm ngược 10s → tải/ xác minh uninstaller → tự copy về local
          → arm RunOnce + AlternateShell (+ autologon tùy chọn)
          → bcdedit safeboot minimal → (20s hủy được) → shutdown /r
[Safe]    Tự khởi động → phát hiện ESET (service/registry/thư mục)
          → sao lưu mạng → chạy uninstaller /force (tối đa 2 lần)
          → dọn mọi trigger → shutdown /r
[Normal]  RunOnce --result → hiển thị kết quả + log
```

## Yêu cầu & Build

- Windows 10/11, quyền **Administrator** (ép sẵn trong `app.manifest`).
- Đặt `uninstaller.exe` (ESET Uninstaller) vào thư mục gốc → build Release (VS 2019+/MSBuild). File được **nhúng** vào exe.

## Triển khai

Xem [deploy/README-deploy.md](deploy/README-deploy.md). Tóm tắt CLI:

```
ESET Uninstaller Support Tool.exe [--auto] [--select] [--password=<pwd>]
```

Mã thoát: `0`=gỡ xong · `1`=còn ESET · `2`=lỗi · `3`=không có ESET.

> ⚠️ Công cụ **không** tự lan qua mạng. Mọi lần chạy đều do người dùng/hệ thống quản trị chủ động khởi chạy.

---

## 🇬🇧 English

Automatically removes ESET in **Safe Mode**, then reboots back to normal Windows with minimal manual steps. It uses ESET's own official **ESET Uninstaller** (embedded + optional latest download); it does **not** reverse-engineer ESET products.

### Features

- 🔁 **Safe Mode relaunch** via two independent triggers: `RunOnce` (`*` prefix) + **AlternateShell**.
- 🔐 Optional **autologon** with the password kept as an encrypted **LSA secret**, wiped after one cycle.
- 🌐 **Runs from a network share / USB**: self-copies to local disk before Safe Mode (which has no network).
- ⬇️ Downloads the **latest ESET Uninstaller** when online, **verifies ESET's Authenticode signature** + logs SHA-256; falls back to the embedded copy offline.
- 🧹 **Unattended** removal with `/force` and **auto-retry up to 2×**; or `--select` for a manual product menu.
- 🛟 **Safety**: **Abort restart** button, recovery guard that clears leftover flags, detailed log + result screen.
- 🌏 **Bilingual VI/EN** with a live toggle. 🐞 **Report issue** button opens GitHub Issues.

### Requirements & Build

- Windows 10/11, **Administrator** (enforced by `app.manifest`).
- Put `uninstaller.exe` (ESET Uninstaller) in the project root → build Release. It is **embedded** into the exe.

### Deployment

See [deploy/README-deploy.md](deploy/README-deploy.md). CLI:

```
ESET Uninstaller Support Tool.exe [--auto] [--select] [--password=<pwd>]
```

Exit codes: `0`=removed · `1`=still present · `2`=error · `3`=no ESET.

> ⚠️ The tool **never** self-propagates over the network — every run is deliberately launched by a person or management system.

---

Xem thêm / See also: [HOW_IT_WORKS.md](HOW_IT_WORKS.md).
