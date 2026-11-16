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

        // Override the culture default with a previously saved choice.
        internal static void LoadSaved()
        {
            string s = Program.GetState("Lang");
            if (s == "vi") Current = Lang.Vi;
            else if (s == "en") Current = Lang.En;
        }

        internal static void Toggle()
        {
            Current = Current == Lang.Vi ? Lang.En : Lang.Vi;
            try { Program.SetState("Lang", Current == Lang.Vi ? "vi" : "en"); } catch { }
        }

        /// <summary>Look up a key for the current language (falls back to the key).</summary>
        internal static string T(string key)
        {
            var table = Current == Lang.Vi ? Vi : En;
            string v;
            return table.TryGetValue(key, out v) ? v : key;
        }

        // Exposed for tests: the key set of each language table.
        internal static System.Collections.Generic.ICollection<string> Keys(Lang lang)
        {
            return (lang == Lang.Vi ? Vi : En).Keys;
        }

        private static readonly Dictionary<string, string> Vi = new Dictionary<string, string>
        {
            { "app_title",        "Công cụ hỗ trợ gỡ ESET" },
            { "status_normal",    "Hệ thống đang ở chế độ bình thường." },
            { "no_eset_normal",   "Không phát hiện ESET trên máy — không cần gỡ. Bạn vẫn có thể chạy thủ công nếu muốn." },
            { "countdown",        "Đếm ngược: {0} giây" },
            { "btn_restart",      "Khởi động lại vào Safe Mode" },
            { "btn_cancel",       "Hủy bỏ" },
            { "btn_lang",         "English" },
            { "btn_report",       "Báo lỗi / Góp ý" },
            { "chk_select",       "Chọn sản phẩm ESET thủ công (trong Safe Mode)" },
            { "chk_clean",        "Dọn file/registry ESET còn sót (nâng cao)" },
            { "btn_nosafemode",   "Thử gỡ ngay (không cần Safe Mode)" },
            { "info_password",    "Mẹo: chạy trong Safe Mode gỡ được ESET kể cả khi cài đặt bị khóa bằng mật khẩu." },
            { "preflight",        "Trước khi vào Safe Mode:\n• Wi-Fi có thể KHÔNG hoạt động — chuẩn bị mạng dây/truy cập từ xa.\n• Sao lưu dữ liệu quan trọng.\n\nTiếp tục khởi động lại vào Safe Mode?" },
            { "preflight_title",  "Kiểm tra trước khi chạy" },
            { "nosafemode_confirm","Chế độ này gỡ ESET NGAY trong Windows bình thường.\nHãy TẮT 'Self-Defense / HIPS' trong ESET trước (và nhập mật khẩu nếu được hỏi), nếu không thao tác có thể bị chặn.\n\nTiếp tục?" },
            { "nosafemode_title", "Gỡ không cần Safe Mode" },
            { "working",          "Đang gỡ ESET, vui lòng đợi..." },
            { "result_openesetlog","Mở log ESET" },
            { "result_restorenet","Khôi phục mạng" },
            { "restored_ok",      "Đã khôi phục cấu hình mạng." },
            { "restored_fail",    "Không tìm thấy bản sao lưu mạng để khôi phục." },
            { "btn_about",        "Giới thiệu" },
            { "result_openfolder","Mở thư mục log" },
            { "crash_title",      "Lỗi không mong muốn" },
            { "crash",            "Đã xảy ra lỗi không mong muốn. Chi tiết đã ghi vào log:" },
            { "need_admin",       "Công cụ cần quyền Administrator để chạy." },
            { "already_running",  "Công cụ đang chạy rồi." },
            { "help_title",       "Hướng dẫn dòng lệnh" },
            { "help_text",        "ESET Uninstaller Support Tool\n\nTham số dòng lệnh:\n  --auto            Không giao diện, tự vào Safe Mode\n  --select          Chọn sản phẩm ESET thủ công\n  --cleanleftovers  Dọn file/service ESET còn sót\n  --password=<mk>   Tự động đăng nhập (mật khẩu Windows)\n  --nosafemode      Gỡ ngay trong Windows bình thường (không reboot)\n  --help            Hiện trợ giúp này\n\nMã thoát: 0=xong, 1=còn ESET, 2=lỗi, 3=không có ESET" },
            { "about_text",       "ESET Uninstaller Support Tool\nPhiên bản {0}\n\nTự động gỡ ESET trong Safe Mode.\nGiấy phép MIT.\n\n{1}" },
            { "tip_restart",      "Khởi động lại vào Safe Mode rồi tự gỡ ESET (khuyên dùng)." },
            { "tip_nosafemode",   "Gỡ ngay trong Windows bình thường. Cần tắt Self-Defense/HIPS trước." },
            { "tip_select",       "Hiện menu để tự chọn sản phẩm ESET cần gỡ (thay vì gỡ tất cả)." },
            { "tip_clean",        "Nếu còn sót, xóa service + thư mục ESET. Có rủi ro — chỉ bật khi cần." },
            { "result_diag",      "Gom log chẩn đoán" },
            { "diag_done",        "Đã tạo gói chẩn đoán:\n{0}" },
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
            { "no_eset_normal",   "No ESET detected on this PC — nothing to remove. You can still run it manually." },
            { "countdown",        "Countdown: {0}s" },
            { "btn_restart",      "Restart to Safe Mode" },
            { "btn_cancel",       "Cancel" },
            { "btn_lang",         "Tiếng Việt" },
            { "btn_report",       "Report issue / Feedback" },
            { "chk_select",       "Pick ESET product manually (in Safe Mode)" },
            { "chk_clean",        "Clean leftover ESET files/registry (advanced)" },
            { "btn_nosafemode",   "Try removal now (no Safe Mode)" },
            { "info_password",    "Tip: running in Safe Mode removes ESET even if its settings are password-protected." },
            { "preflight",        "Before entering Safe Mode:\n• Wi-Fi may NOT work — prepare wired/remote access.\n• Back up important data.\n\nContinue restarting into Safe Mode?" },
            { "preflight_title",  "Pre-flight check" },
            { "nosafemode_confirm","This removes ESET RIGHT NOW in normal Windows.\nFirst DISABLE 'Self-Defense / HIPS' in ESET (and enter the password if asked), otherwise it may be blocked.\n\nContinue?" },
            { "nosafemode_title", "Remove without Safe Mode" },
            { "working",          "Uninstalling ESET, please wait..." },
            { "result_openesetlog","Open ESET log" },
            { "result_restorenet","Restore network" },
            { "restored_ok",      "Network configuration restored." },
            { "restored_fail",    "No network backup found to restore." },
            { "btn_about",        "About" },
            { "result_openfolder","Open log folder" },
            { "crash_title",      "Unexpected error" },
            { "crash",            "An unexpected error occurred. Details were written to the log:" },
            { "need_admin",       "This tool must run as Administrator." },
            { "already_running",  "The tool is already running." },
            { "help_title",       "Command-line help" },
            { "help_text",        "ESET Uninstaller Support Tool\n\nCommand-line flags:\n  --auto            No UI; arm + reboot into Safe Mode\n  --select          Pick the ESET product manually\n  --cleanleftovers  Remove leftover ESET files/services\n  --password=<pwd>  Unattended autologon (Windows password)\n  --nosafemode      Uninstall now in normal Windows (no reboot)\n  --help            Show this help\n\nExit codes: 0=done, 1=still present, 2=error, 3=no ESET" },
            { "about_text",       "ESET Uninstaller Support Tool\nVersion {0}\n\nAutomatically removes ESET in Safe Mode.\nMIT License.\n\n{1}" },
            { "tip_restart",      "Restart into Safe Mode and remove ESET automatically (recommended)." },
            { "tip_nosafemode",   "Remove now in normal Windows. Disable Self-Defense/HIPS first." },
            { "tip_select",       "Show a menu to pick which ESET product to remove (instead of all)." },
            { "tip_clean",        "If leftovers remain, delete ESET services + folders. Risky — enable only if needed." },
            { "result_diag",      "Collect diagnostics" },
            { "diag_done",        "Diagnostics bundle created:\n{0}" },
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
