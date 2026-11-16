using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace frmesetunins
{
    internal static class Updater
    {
        private const string ReleasesApi =
            "https://api.github.com/repos/ThanhNguyenVN93/ESET-Uninstaller-Support-Tool/releases/latest";

        internal sealed class ReleaseInfo
        {
            internal string TagName;
            internal Version Version;
            internal string DownloadUrl;
            internal string HtmlUrl;
        }

        internal static ReleaseInfo CheckForUpdate()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var req = (HttpWebRequest)WebRequest.Create(ReleasesApi);
                req.Timeout = 10000;
                req.ReadWriteTimeout = 10000;
                req.UserAgent = "ESETUninstallerSupport/" + Program.AppVersion;
                req.Accept = "application/vnd.github+json";

                string json;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    json = sr.ReadToEnd();

                string tag = ExtractJsonString(json, "tag_name");
                string htmlUrl = ExtractJsonString(json, "html_url");
                if (tag == null) return null;

                string versionStr = tag.TrimStart('v', 'V');
                Version remote;
                if (!Version.TryParse(versionStr, out remote)) return null;

                var current = new Version(Program.AppVersion);
                if (remote <= current) return null;

                string downloadUrl = ExtractFirstAssetUrl(json);

                return new ReleaseInfo
                {
                    TagName = tag,
                    Version = remote,
                    DownloadUrl = downloadUrl,
                    HtmlUrl = htmlUrl
                };
            }
            catch (Exception ex)
            {
                Program.Log("Update check failed: " + ex.Message);
                return null;
            }
        }

        internal static bool DownloadAndStage(ReleaseInfo info, out string stagedPath)
        {
            stagedPath = null;
            if (info == null || info.DownloadUrl == null) return false;

            string dir = Program.LocalDir();
            string tmp = Path.Combine(dir, "update.tmp");
            string dest = Path.Combine(dir, "update_" + info.TagName + ".exe");

            try
            {
                Directory.CreateDirectory(dir);

                var req = (HttpWebRequest)WebRequest.Create(info.DownloadUrl);
                req.Timeout = 30000;
                req.ReadWriteTimeout = 30000;
                req.UserAgent = "ESETUninstallerSupport/" + Program.AppVersion;

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                    rs.CopyTo(fs);

                if (new FileInfo(tmp).Length < 50 * 1024)
                {
                    try { File.Delete(tmp); } catch { }
                    Program.Log("Update download too small, rejected.");
                    return false;
                }

                if (File.Exists(dest)) File.Delete(dest);
                File.Move(tmp, dest);
                stagedPath = dest;
                Program.Log("Update staged: " + dest + " (" + new FileInfo(dest).Length + " bytes)");
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                Program.Log("Update download failed: " + ex.Message);
                return false;
            }
        }

        internal static void ApplyUpdate(string stagedExe)
        {
            string currentExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string bak = currentExe + ".bak";
            string cmd = Path.Combine(Path.GetTempPath(), "eset_update.cmd");

            string script = "@echo off\r\n"
                + "timeout /t 2 /nobreak >nul\r\n"
                + "if exist \"" + bak + "\" del /f \"" + bak + "\"\r\n"
                + "move /y \"" + currentExe + "\" \"" + bak + "\"\r\n"
                + "copy /y \"" + stagedExe + "\" \"" + currentExe + "\"\r\n"
                + "del /f \"" + stagedExe + "\"\r\n"
                + "start \"\" \"" + currentExe + "\"\r\n"
                + "del /f \"%~f0\"\r\n";

            File.WriteAllText(cmd, script, System.Text.Encoding.Default);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"" + cmd + "\"",
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };
            System.Diagnostics.Process.Start(psi);
        }

        private static string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"";
            var m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string ExtractFirstAssetUrl(string json)
        {
            var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
