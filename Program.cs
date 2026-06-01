using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace frmesetunins
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (SystemInformation.BootMode != BootMode.Normal)
            {
                HandleSafeMode();
                return;
            }

            Application.Run(new Form1());
        }

        // ── Safe Mode entry point ─────────────────────────────────────────

        private static void HandleSafeMode()
        {
            try
            {
                if (EsetInstalled())
                    RunEmbeddedUninstaller();
                else
                    MessageBox.Show(
                        "ESET da duoc go cai dat thanh cong!\nMay tinh se khoi dong lai binh thuong.",
                        "Hoan Tat", MessageBoxButtons.OK, MessageBoxIcon.Information);

                RemoveSafeBootAndRestart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi xu ly Safe Mode:\n" + ex.Message,
                    "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool EsetInstalled()
        {
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

        private static void RunEmbeddedUninstaller()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "frmesetunins_temp");
            Directory.CreateDirectory(tempDir);
            string dest = Path.Combine(tempDir, "uninstaller.exe");
            try
            {
                using (var s = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceStream("frmesetunins.uninstaller.exe"))
                {
                    if (s == null) throw new InvalidOperationException("Khong tim thay uninstaller trong resources.");
                    using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                        s.CopyTo(fs);
                }
                using (var proc = Process.Start(new ProcessStartInfo(dest)
                {
                    UseShellExecute  = true,
                    WorkingDirectory = tempDir,
                }))
                    proc.WaitForExit();
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        private static void RemoveSafeBootAndRestart()
        {
            RunSysCmd("bcdedit.exe", "/deletevalue {current} safeboot");
            RunSysCmd("shutdown.exe", "/r /t 5");
        }

        // ── Shared helper ─────────────────────────────────────────────────

        internal static void RunSysCmd(string exe, string args)
        {
            string winDir = Environment.GetEnvironmentVariable("SystemRoot");
            string sysDir = Environment.Is64BitProcess
                ? Path.Combine(winDir, "System32")
                : Path.Combine(winDir, "Sysnative");

            using (var p = Process.Start(new ProcessStartInfo(Path.Combine(sysDir, exe))
            {
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true,
            }))
                p.WaitForExit();
        }
    }
}
