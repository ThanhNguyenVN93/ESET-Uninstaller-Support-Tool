using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frmesetunins
{
    internal static class Program
    {
        // Relaunch triggers / state (shared with Form1).
        internal const string RunOnceKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        // '*' prefix is REQUIRED so the entry is honored while booted in Safe Mode.
        internal const string RunOnceName = "*FrmEseUnins";

        private const string SafeBootKey    = @"SYSTEM\CurrentControlSet\Control\SafeBoot";
        private const string AppStateKey    = @"SOFTWARE\frmesetunins";
        private const string OrigShellValue = "OrigAlternateShell";

        private const string SafeModeMutex  = @"Global\frmesetunins_safemode";

        private const string ResultRunOnce   = "FrmEseUninsResult";
        private const string LastResultVal   = "LastResult";
        private const string UninstallModeVal = "UninstallMode";
        private const string ArmedVal        = "Armed";

        internal const string EsetUninstallerUrl =
            "https://download.eset.com/com/eset/tools/installers/eset_apps_remover/latest/uninstaller.exe";

        internal const string IssuesUrl =
            "https://github.com/ThanhNguyenVN93/ESET-Uninstaller-Support-Tool/issues/new";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool wantResult = args != null && Array.IndexOf(args, "--result") >= 0;

            // Defensive: if a previous arm never reached Safe Mode (aborted,
            // crashed, or the restart failed), the safeboot flag / autologon may
            // still be set. Clean everything up now that we're in normal mode.
            if (SystemInformation.BootMode == BootMode.Normal &&
                (IsArmedLeftover() || AutoLogon.IsActive()))
            {
                try { UndoAllBootTriggers(); } catch { }
            }

            // Post-run summary: after the whole cycle returns to normal mode, a
            // RunOnce relaunches us with --result to show how the uninstall went.
            if (wantResult && SystemInformation.BootMode == BootMode.Normal)
            {
                Application.Run(new ResultForm());
                return;
            }

            if (SystemInformation.BootMode != BootMode.Normal)
            {
                // Two independent triggers can launch us in Safe Mode: the
                // RunOnce entry (runs in userinit) and the AlternateShell
                // fallback (runs as the Safe Mode shell). Guarantee that only
                // one instance performs the uninstall. Any extra instance stays
                // alive until the owner reboots, so that a quick exit of the
                // shell instance doesn't trigger a logoff that kills the worker.
                bool createdNew;
                using (var guard = new Mutex(true, SafeModeMutex, out createdNew))
                {
                    if (!createdNew)
                    {
                        try { guard.WaitOne(); } catch (AbandonedMutexException) { }
                        return;
                    }

                    HandleSafeMode();
                }
                return;
            }

            // Silent/CLI mode for mass deployment: arm the triggers and reboot
            // with no UI. Optional --password=VALUE enables unattended autologon.
            if (args != null && Array.IndexOf(args, "--auto") >= 0)
            {
                RunAutoSilent(args);
                return;
            }

            Application.Run(new Form1());
        }

        // ── Silent mode ───────────────────────────────────────────────────

        private static void RunAutoSilent(string[] args)
        {
            try
            {
                string exe = StageSelfLocally();
                TryStageLatestUninstaller(out string _);
                bool interactive = Array.IndexOf(args, "--select") >= 0;
                ArmSafeMode(exe, interactive);

                string pwd = GetArgValue(args, "--password=");
                if (pwd != null)
                {
                    try { AutoLogon.Enable(pwd); }
                    catch (Exception ex) { Log("Silent autologon failed: " + ex.Message); }
                }

                RunSysCmd("shutdown.exe", "/r /t 5");
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Log("Silent arm failed: " + ex);
                Environment.ExitCode = 2;
            }
        }

        private static string GetArgValue(string[] args, string prefix)
        {
            if (args != null)
                foreach (string a in args)
                    if (a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return a.Substring(prefix.Length);
            return null;
        }

        // Arms every Safe Mode relaunch trigger (does NOT schedule the restart).
        // interactive=true runs the ESET uninstaller in '--select' (menu) mode
        // instead of '/force'.
        internal static void ArmSafeMode(string exe, bool interactive)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: true))
                key?.SetValue(RunOnceName, "\"" + exe + "\"");

            EnableAlternateShellFallback(exe);

            SetState(UninstallModeVal, interactive ? "select" : "force");
            SetState(ArmedVal, "1");

            RunSysCmd("bcdedit.exe", "/set {current} safeboot minimal");
            RunSysCmd("bcdedit.exe", "/set {current} safebootalternateshell yes");
        }

        // Reverts every trigger set by ArmSafeMode + autologon (no restart).
        // Used by the "abort restart" button and by Safe Mode cleanup.
        internal static void UndoAllBootTriggers()
        {
            DisableAlternateShellFallback();
            try { AutoLogon.Disable(); } catch { }

            using (var key = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: true))
            {
                try { key?.DeleteValue(RunOnceName, throwOnMissingValue: false); } catch { }
            }

            ClearState(ArmedVal);

            RunSysCmd("bcdedit.exe", "/deletevalue {current} safebootalternateshell");
            RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
        }

        internal static void OpenIssues()
        {
            try { Process.Start(new ProcessStartInfo(IssuesUrl) { UseShellExecute = true }); }
            catch { }
        }

        // ── Safe Mode entry point ─────────────────────────────────────────

        private static void HandleSafeMode()
        {
            // Runs fully unattended: no blocking dialogs (there may be nobody to
            // click them when autologon is used). Results go to the log file, and
            // we ALWAYS undo the boot triggers and restart — even on error — so
            // the machine can never get stuck in Safe Mode.
            try
            {
                Log("=== Safe Mode run started ===");
                ClearState(ArmedVal); // we reached Safe Mode; no leftover to clean

                bool interactive = GetState(UninstallModeVal) == "select";

                if (EsetInstalled())
                {
                    BackupNetworkSettings();
                    Log("Before uninstall: " + EsetStateSnapshot());

                    // The KB notes a second pass is sometimes needed. Auto-retry
                    // once in unattended (/force) mode; a single pass for the
                    // interactive menu so the user isn't prompted twice.
                    int maxPasses = interactive ? 1 : 2;
                    for (int pass = 1; pass <= maxPasses && EsetInstalled(); pass++)
                    {
                        Log("Uninstall pass " + pass + " (" + (interactive ? "select" : "force") + ").");
                        RunEmbeddedUninstaller(interactive);
                    }

                    Log("After uninstall: " + EsetStateSnapshot());

                    if (EsetInstalled())
                    {
                        Log("WARNING: ESET still detected after uninstall; may need another pass.");
                        SetLastResult("warning");
                        Environment.ExitCode = 1;
                    }
                    else
                    {
                        Log("ESET removed successfully.");
                        SetLastResult("success");
                        Environment.ExitCode = 0;
                    }
                }
                else
                {
                    Log("No ESET detected; nothing to remove.");
                    SetLastResult("none");
                    Environment.ExitCode = 3;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR during Safe Mode handling: " + ex);
                SetLastResult("error:" + ex.Message);
                Environment.ExitCode = 2;
            }
            finally
            {
                try { RemoveSafeBootAndRestart(); }
                catch (Exception ex2) { Log("ERROR during cleanup: " + ex2); }
            }
        }

        private static bool EsetInstalled()
        {
            // 1) Services: the ESET kernel service is the most reliable signal.
            foreach (string svc in new[] { "ekrn", "epfw", "ehdrv" })
                using (var k = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + svc))
                    if (k != null) return true;

            // 2) Uninstall registry (64-bit + 32-bit views): catches installs
            //    regardless of folder location.
            if (UninstallListHasEset(RegistryView.Registry64) ||
                UninstallListHasEset(RegistryView.Registry32))
                return true;

            // 3) Folders as a last resort.
            string pf         = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86       = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            foreach (string p in new[]
            {
                Path.Combine(pf,         "ESET"),
                Path.Combine(pf86,       "ESET"),
                Path.Combine(commonData, "ESET"),
            })
                if (Directory.Exists(p)) return true;

            return false;
        }

        // Human-readable snapshot of ESET's footprint, logged before and after
        // the uninstall so a run can be diagnosed from the log alone.
        private static string EsetStateSnapshot()
        {
            var sb = new StringBuilder();

            sb.Append("services=[");
            foreach (string svc in new[] { "ekrn", "epfw", "ehdrv" })
                using (var k = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + svc))
                    if (k != null) sb.Append(svc).Append(' ');
            sb.Append("] folders=[");

            string pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string cd   = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            foreach (string p in new[]
            {
                Path.Combine(pf, "ESET"), Path.Combine(pf86, "ESET"), Path.Combine(cd, "ESET"),
            })
                if (Directory.Exists(p)) sb.Append(p).Append(' ');
            sb.Append("] uninstallEntry=")
              .Append(UninstallListHasEset(RegistryView.Registry64) ||
                      UninstallListHasEset(RegistryView.Registry32));

            return sb.ToString();
        }

        private static bool UninstallListHasEset(RegistryView view)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var uninstall = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (uninstall == null) return false;
                    foreach (string sub in uninstall.GetSubKeyNames())
                        using (var app = uninstall.OpenSubKey(sub))
                        {
                            string name      = app?.GetValue("DisplayName")   as string;
                            string publisher = app?.GetValue("Publisher")     as string;
                            if ((name      != null && name.IndexOf("ESET", StringComparison.OrdinalIgnoreCase)      >= 0) ||
                                (publisher != null && publisher.IndexOf("ESET", StringComparison.OrdinalIgnoreCase) >= 0))
                                return true;
                        }
                }
            }
            catch { }
            return false;
        }

        // 5 minutes: generous for a full ESET removal, but bounded so a hung
        // uninstaller can never leave the machine stuck in Safe Mode forever.
        private const int UninstallTimeoutMs = 5 * 60 * 1000;
        // Interactive menu needs a human: allow much longer before giving up.
        private const int UninstallInteractiveTimeoutMs = 30 * 60 * 1000;

        private static void RunEmbeddedUninstaller(bool interactive)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "frmesetunins_temp");
            Directory.CreateDirectory(tempDir);
            // The KB command examples use the name ESETUninstaller.exe.
            string dest = Path.Combine(tempDir, "ESETUninstaller.exe");
            try
            {
                // Prefer the fresh copy staged (downloaded) in normal mode; fall
                // back to the embedded resource when none is available (e.g. no
                // network was reachable before the reboot).
                string staged = StagedUninstallerPath;
                if (File.Exists(staged) && new FileInfo(staged).Length >= 50 * 1024)
                {
                    File.Copy(staged, dest, overwrite: true);
                    Log("Using staged (downloaded) uninstaller.");
                }
                else
                {
                    using (var s = Assembly.GetExecutingAssembly()
                                           .GetManifestResourceStream("frmesetunins.uninstaller.exe"))
                    {
                        if (s == null)
                            throw new InvalidOperationException("Khong tim thay uninstaller trong resources.");
                        if (s.Length < 50 * 1024)
                            throw new InvalidOperationException("Uninstaller nhung khong hop le (qua nho).");
                        using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                            s.CopyTo(fs);
                    }
                    Log("Using embedded uninstaller.");
                }

                // Record exactly what we are about to execute (traceability).
                string sigSubject;
                bool   signedByEset = Verify.IsSignedByEset(dest, out sigSubject);
                Log("Uninstaller sha256=" + Verify.Sha256(dest) +
                    " signedByEset=" + signedByEset + " signer=" + sigSubject);

                // '/force' removes all ESET apps/components WITHOUT prompting —
                // needed for unattended runs. Interactive ('select') mode shows
                // the tool's own menu so the user picks which product to remove.
                string arguments = interactive ? "" : "/force";
                int    timeout   = interactive ? UninstallInteractiveTimeoutMs : UninstallTimeoutMs;

                using (var proc = Process.Start(new ProcessStartInfo(dest)
                {
                    Arguments        = arguments,
                    UseShellExecute  = false,
                    CreateNoWindow   = false,
                    WorkingDirectory = tempDir,
                }))
                {
                    if (!proc.WaitForExit(timeout))
                    {
                        try { proc.Kill(); } catch { }
                        throw new TimeoutException(
                            "ESET Uninstaller chay qua " + (timeout / 60000) + " phut va da bi dung.");
                    }
                    Log("Uninstaller exit code: " + proc.ExitCode);
                }
            }
            finally
            {
                SaveUninstallerLog(tempDir);
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        // ESETUninstaller writes ~ESETUninstaller.log beside itself; preserve a
        // copy before the temp folder is deleted so the run can be diagnosed.
        private static void SaveUninstallerLog(string tempDir)
        {
            try
            {
                string src = Path.Combine(tempDir, "~ESETUninstaller.log");
                if (File.Exists(src))
                {
                    string dst = Path.Combine(Path.GetTempPath(),
                        "frmesetunins_ESETUninstaller_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                    File.Copy(src, dst, overwrite: true);
                    Log("Saved uninstaller log to: " + dst);
                }
            }
            catch { }
        }

        private static void RemoveSafeBootAndRestart()
        {
            // Undo every relaunch trigger before leaving Safe Mode, otherwise a
            // leftover RunOnce/AlternateShell could relaunch us in normal mode
            // and bounce the machine straight back into Safe Mode (a loop).
            UndoAllBootTriggers();

            // Show a result summary on the next NORMAL boot (no '*' prefix, so it
            // only runs in normal mode). Best-effort.
            using (var key = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: true))
            {
                try { key?.SetValue(ResultRunOnce, "\"" + Application.ExecutablePath + "\" --result"); } catch { }
            }

            RunSysCmd("shutdown.exe", "/r /t 5");
        }

        // Best-effort backup of network configuration before touching ESET's
        // filter drivers, so the user can restore with: netsh -f <file>
        private static void BackupNetworkSettings()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "frmesetunins");
                Directory.CreateDirectory(dir);
                string outFile = Path.Combine(dir,
                    "NetworkSettings_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

                var psi = new ProcessStartInfo(Path.Combine(SysDir(), "netsh.exe"), "-c interface dump")
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true,
                };
                using (var p = Process.Start(psi))
                {
                    string dump = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    File.WriteAllText(outFile, dump);
                }
                Log("Backed up network settings to: " + outFile);
            }
            catch (Exception ex) { Log("Network backup failed: " + ex.Message); }
        }

        // ── AlternateShell fallback ───────────────────────────────────────
        // Registers this app as the Safe Mode shell, so it launches even if
        // RunOnce processing is suppressed. The original shell value is backed
        // up so it can be restored during Safe Mode cleanup.

        internal static void EnableAlternateShellFallback(string exePath)
        {
            using (var sb = Registry.LocalMachine.OpenSubKey(SafeBootKey, writable: true))
            {
                if (sb == null) return;

                string original = sb.GetValue("AlternateShell") as string;
                if (string.IsNullOrEmpty(original)) original = "cmd.exe";

                // Don't overwrite a backup left by an earlier, unfinished run.
                using (var state = Registry.LocalMachine.CreateSubKey(AppStateKey))
                {
                    if (state != null && state.GetValue(OrigShellValue) == null)
                        state.SetValue(OrigShellValue, original);
                }

                sb.SetValue("AlternateShell", "\"" + exePath + "\"");
            }
        }

        internal static void DisableAlternateShellFallback()
        {
            string original = "cmd.exe";
            using (var state = Registry.LocalMachine.OpenSubKey(AppStateKey, writable: true))
            {
                if (state != null)
                {
                    if (state.GetValue(OrigShellValue) is string s && !string.IsNullOrEmpty(s))
                        original = s;
                    try { state.DeleteValue(OrigShellValue, throwOnMissingValue: false); } catch { }
                }
            }

            using (var sb = Registry.LocalMachine.OpenSubKey(SafeBootKey, writable: true))
                sb?.SetValue("AlternateShell", original);
        }

        // ── Uninstaller staging (download latest in normal mode) ──────────

        private static string LocalDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "frmesetunins");
        }

        internal static string StagedUninstallerPath
        {
            get { return Path.Combine(LocalDir(), "uninstaller.exe"); }
        }

        private static string SelfLocalPath
        {
            get { return Path.Combine(LocalDir(), "ESETUninstallerSupport.exe"); }
        }

        /// <summary>
        /// Copy this executable to a LOCAL path and return it. Essential when the
        /// tool is launched from a network share (or any removable/synced folder):
        /// Safe Mode minimal has no networking, so the relaunch triggers must point
        /// at a local copy or the app would never start in Safe Mode. This copies
        /// to the same machine's disk — it is NOT network self-propagation.
        /// </summary>
        internal static string StageSelfLocally()
        {
            string target = SelfLocalPath;
            try
            {
                string cur = Application.ExecutablePath;
                if (string.Equals(cur, target, StringComparison.OrdinalIgnoreCase))
                    return target; // already running from the local copy

                Directory.CreateDirectory(LocalDir());
                File.Copy(cur, target, overwrite: true);
                Log("Staged self to local path: " + target);
                return target;
            }
            catch (Exception ex)
            {
                Log("Self-stage failed, using current path: " + ex.Message);
                return Application.ExecutablePath;
            }
        }

        /// <summary>
        /// Try to download the latest ESET Uninstaller (there is only one, shared
        /// across all product versions) so Safe Mode uses a current build. Must be
        /// called in normal mode where networking is available — Safe Mode minimal
        /// has none, so the embedded copy is used as fallback.
        /// </summary>
        internal static bool TryStageLatestUninstaller(out string message)
        {
            string path = StagedUninstallerPath;
            string tmp  = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

                var req = (HttpWebRequest)WebRequest.Create(EsetUninstallerUrl);
                req.Timeout          = 20000;
                req.ReadWriteTimeout = 20000;
                req.UserAgent        = "frmesetunins";

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs   = resp.GetResponseStream())
                using (var fs   = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                    rs.CopyTo(fs);

                // Validate: a real PE executable of a sane size.
                if (new FileInfo(tmp).Length < 50 * 1024 || !HasMzHeader(tmp))
                {
                    try { File.Delete(tmp); } catch { }
                    message = "Tep tai ve khong hop le; dung ban tich hop.";
                    Log("Downloaded uninstaller failed validation.");
                    return false;
                }

                // Only trust a download that is validly Authenticode-signed by
                // ESET — otherwise we would run an unverified executable.
                string signer;
                if (!Verify.IsSignedByEset(tmp, out signer))
                {
                    try { File.Delete(tmp); } catch { }
                    message = "Ban tai ve khong co chu ky ESET hop le; dung ban tich hop.";
                    Log("Downloaded uninstaller REJECTED (not validly ESET-signed). Signer=" + signer);
                    return false;
                }

                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                Log("Staged latest uninstaller (" + new FileInfo(path).Length + " bytes). Signer=" +
                    signer + " sha256=" + Verify.Sha256(path));
                message = "Da tai ESET Uninstaller moi nhat.";
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                Log("Download latest uninstaller failed: " + ex.Message);
                message = "Khong tai duoc ban moi; dung ban tich hop.";
                return false;
            }
        }

        private static bool HasMzHeader(string file)
        {
            try
            {
                using (var fs = File.OpenRead(file))
                    return fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
            }
            catch { return false; }
        }

        // ── Last-result state (for the post-run summary screen) ────────────

        internal static void SetState(string name, string value)
        {
            try
            {
                using (var k = Registry.LocalMachine.CreateSubKey(AppStateKey))
                    k?.SetValue(name, value ?? "");
            }
            catch { }
        }

        internal static string GetState(string name)
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(AppStateKey))
                    return k?.GetValue(name) as string;
            }
            catch { return null; }
        }

        internal static void ClearState(string name)
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(AppStateKey, writable: true))
                    k?.DeleteValue(name, throwOnMissingValue: false);
            }
            catch { }
        }

        internal static bool IsArmedLeftover() { return GetState(ArmedVal) == "1"; }

        internal static void SetLastResult(string value)
        {
            SetState(LastResultVal, value);
        }

        internal static string GetAndClearLastResult()
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(AppStateKey, writable: true))
                {
                    string v = k?.GetValue(LastResultVal) as string;
                    try { k?.DeleteValue(LastResultVal, throwOnMissingValue: false); } catch { }
                    return v;
                }
            }
            catch { return null; }
        }

        // ── Shared helpers ────────────────────────────────────────────────

        private static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "frmesetunins.log");

        internal static string LogFilePath => LogPath;

        internal static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("s") + "  " + msg + Environment.NewLine); }
            catch { }
        }

        private static string SysDir()
        {
            string winDir = Environment.GetEnvironmentVariable("SystemRoot");
            return Environment.Is64BitProcess
                ? Path.Combine(winDir, "System32")
                : Path.Combine(winDir, "Sysnative");
        }

        internal static void RunSysCmd(string exe, string args)
        {
            using (var p = Process.Start(new ProcessStartInfo(Path.Combine(SysDir(), exe))
            {
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true,
            }))
                p.WaitForExit();
        }
    }
}
