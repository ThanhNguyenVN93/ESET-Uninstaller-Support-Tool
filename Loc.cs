using System.Collections.Generic;
using System.Globalization;

namespace frmesetunins
{
    /// <summary>
    /// Minimal two-language (Vietnamese / English) string table. The initial
    /// language follows the Windows UI culture; the user can switch it live.
    /// </summary>
    internal static class Loc
    {
        internal enum Lang { Vi, En }

        internal static Lang Current;

        static Loc()
        {
            Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi"
                ? Lang.Vi : Lang.En;
        }

        internal static void Toggle()
        {
            Current = Current == Lang.Vi ? Lang.En : Lang.Vi;
        }

        /// <summary>Look up a key for the current language (falls back to the key).</summary>
        internal static string T(string key)
        {
            var table = Current == Lang.Vi ? Vi : En;
            string v;
            return table.TryGetValue(key, out v) ? v : key;
        }

        private static readonly Dictionary<string, string> Vi = new Dictionary<string, string>
        {
            { "app_title",        "Công cụ hỗ trợ gỡ ESET" },
            { "status_normal",    "Hệ thống đang ở chế độ bình thường." },
            { "countdown",        "Đếm ngược: {0} giây" },
            { "btn_restart",      "Khởi động lại vào Safe Mode" },
            { "btn_cancel",       "Hủy bỏ" },
            { "btn_lang",         "English" },
            { "btn_report",       "Báo lỗi / Góp ý" },
            { "chk_select",       "Chọn sản phẩm ESET thủ công (trong Safe Mode)" },
            { "status_downloading","Đang tải ESET Uninstaller mới nhất..." },
            { "status_preparing", "Đang chuẩn bị khởi động lại vào Safe Mode..." },
            { "err_prefix",       "Lỗi: " },
            { "autologon_title",  "Tự động đăng nhập" },
            { "autologon_fail",   "Không bật được tự động đăng nhập, bạn sẽ cần đăng nhập thủ công trong Safe Mode.\n" },
            { "pwd_title",        "Tự động đăng nhập Safe Mode" },
            { "pwd_prompt",       "Nhập mật khẩu Windows để máy TỰ ĐỘNG đăng nhập và chạy trong Safe Mode (không cần thao tác tay).\nĐể trống nếu tài khoản không có mật khẩu. Bấm 'Bỏ qua' nếu muốn tự đăng nhập thủ công." },
            { "pwd_ok",           "OK" },
            { "pwd_skip",         "Bỏ qua" },
            { "export_reminder",  "Lưu ý: hãy xuất (export) thiết lập ESET trước khi gỡ — thiết lập sẽ KHÔNG tự khôi phục khi cài lại." },
            { "abort_text",       "Máy sẽ khởi động lại vào Safe Mode sau {0} giây." },
            { "abort_btn",        "Hủy khởi động lại" },
            { "aborted",          "Đã hủy. Mọi thay đổi đã được hoàn tác, máy không khởi động lại." },
            { "result_title",     "Kết quả gỡ cài đặt ESET" },
            { "result_success",   "✓ Đã gỡ cài đặt ESET thành công." },
            { "result_warning",   "⚠ Đã chạy gỡ cài đặt nhưng vẫn còn phát hiện ESET.\nCó thể cần chạy lại công cụ một lần nữa." },
            { "result_none",      "Không phát hiện ESET trên máy. Không có gì để gỡ." },
            { "result_error",     "✕ Có lỗi xảy ra khi gỡ cài đặt trong Safe Mode.\nXem log để biết chi tiết." },
            { "result_unknown",   "Không rõ kết quả lần chạy gần nhất." },
            { "result_openlog",   "Mở log chi tiết" },
            { "result_close",     "Đóng" },
            { "result_nolog",     "(Không có log.)" },
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "app_title",        "ESET Uninstaller Support Tool" },
            { "status_normal",    "The system is in normal mode." },
            { "countdown",        "Countdown: {0}s" },
            { "btn_restart",      "Restart to Safe Mode" },
            { "btn_cancel",       "Cancel" },
            { "btn_lang",         "Tiếng Việt" },
            { "btn_report",       "Report issue / Feedback" },
            { "chk_select",       "Pick ESET product manually (in Safe Mode)" },
            { "status_downloading","Downloading the latest ESET Uninstaller..." },
            { "status_preparing", "Preparing to restart into Safe Mode..." },
            { "err_prefix",       "Error: " },
            { "autologon_title",  "Autologon" },
            { "autologon_fail",   "Could not enable autologon; you will need to log in manually in Safe Mode.\n" },
            { "pwd_title",        "Safe Mode autologon" },
            { "pwd_prompt",       "Enter your Windows password so the PC logs in AUTOMATICALLY and runs in Safe Mode (no manual steps).\nLeave blank if the account has no password. Click 'Skip' to log in manually." },
            { "pwd_ok",           "OK" },
            { "pwd_skip",         "Skip" },
            { "export_reminder",  "Note: export your ESET settings before removal — they are NOT restored automatically on reinstall." },
            { "abort_text",       "The PC will restart into Safe Mode in {0}s." },
            { "abort_btn",        "Abort restart" },
            { "aborted",          "Aborted. All changes were reverted; the PC will not restart." },
            { "result_title",     "ESET uninstall result" },
            { "result_success",   "✓ ESET was uninstalled successfully." },
            { "result_warning",   "⚠ The uninstaller ran but ESET is still detected.\nYou may need to run the tool again." },
            { "result_none",      "No ESET detected on this PC. Nothing to remove." },
            { "result_error",     "✕ An error occurred while uninstalling in Safe Mode.\nSee the log for details." },
            { "result_unknown",   "The outcome of the last run is unknown." },
            { "result_openlog",   "Open detailed log" },
            { "result_close",     "Close" },
            { "result_nolog",     "(No log.)" },
        };
    }
}
