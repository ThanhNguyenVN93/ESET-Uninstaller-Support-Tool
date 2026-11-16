# Triển khai / Deployment

Ba cách triển khai đã được kiểm chứng, dùng chế độ CLI `--auto`. All three use the `--auto` CLI mode.

| Cách / Method | Khi nào / When | Script |
|---|---|---|
| **Network share** | Người dùng/IT tự chạy trên 1 máy. User runs on one PC. | [Deploy-Share.cmd](Deploy-Share.cmd) |
| **PsExec** | IT đẩy tới nhiều máy đã được phép. Push to many authorized PCs. | [Deploy-PsExec.cmd](Deploy-PsExec.cmd) |
| **GPO / Intune / SCCM** | Quản trị domain. Domain-managed rollout. | Chạy `...exe --auto` như một package/startup script. |

## Tham số dòng lệnh / CLI flags

```
ESET Uninstaller Support Tool.exe [--auto] [--select] [--password=<pwd>]
```

- `--auto` — không UI, tự lên lịch vào Safe Mode. No UI; arm + reboot automatically.
- `--select` — chạy ESET Uninstaller ở menu tương tác (chọn sản phẩm). Interactive product menu.
- `--password=<pwd>` — bật autologon không giám sát (mật khẩu Windows của máy). Enables unattended autologon.

## Mã thoát / Exit codes

| Code | Ý nghĩa / Meaning |
|---|---|
| `0` | Đã gỡ ESET thành công / ESET removed |
| `1` | Vẫn còn ESET sau khi gỡ / ESET still present |
| `2` | Lỗi / Error |
| `3` | Không phát hiện ESET / No ESET found |

> ⚠️ **An toàn / Safety:** chỉ chạy trên máy bạn được phép quản trị. Công cụ **không** tự lan qua mạng — mọi lần chạy đều do con người/hệ thống quản trị chủ động khởi chạy. The tool never self-propagates; every run is deliberately launched.
