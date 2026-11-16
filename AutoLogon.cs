using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frmesetunins
{
    /// <summary>
    /// Optional Windows autologon so the Safe Mode pass can run unattended.
    ///
    /// The password is stored as an LSA private secret ("DefaultPassword"),
    /// the same mechanism Sysinternals Autologon uses — it is encrypted by the
    /// OS and never written to the registry in plain text. Autologon is always
    /// torn down again during Safe Mode cleanup (and defensively on the next
    /// normal boot), so it is only active for the single round trip.
    /// </summary>
    internal static class AutoLogon
    {
        private const string WinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        private const string StateKey    = @"SOFTWARE\frmesetunins";

        private const string SecretKeyName = "DefaultPassword";
        private const string ActiveFlag    = "AutoLogonActive";
        private const string PriorAutoFlag = "PriorAutoAdminLogon";

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Enable autologon for the current interactive user.</summary>
        internal static void Enable(string password)
        {
            // Store (or clear, if empty) the password as an encrypted LSA secret.
            StoreLsaSecret(SecretKeyName, password ?? string.Empty);

            using (var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true))
            {
                if (key == null)
                    throw new InvalidOperationException("Khong mo duoc Winlogon key.");

                using (var state = Registry.LocalMachine.CreateSubKey(StateKey))
                {
                    if (state != null && state.GetValue(PriorAutoFlag) == null)
                        state.SetValue(PriorAutoFlag, (key.GetValue("AutoAdminLogon") as string) ?? "0");
                    state?.SetValue(ActiveFlag, "1");
                }

                key.SetValue("AutoAdminLogon",   "1");
                key.SetValue("DefaultUserName",  Environment.UserName);
                key.SetValue("DefaultDomainName", Environment.UserDomainName);
                // Safety net: after one automatic logon Winlogon decrements this
                // to 0 and clears AutoAdminLogon itself — so even if our own
                // teardown never runs, autologon self-disables after a single use.
                key.SetValue("AutoLogonCount", 1, RegistryValueKind.DWord);
                // A leftover plaintext password would take precedence — make sure
                // only the encrypted LSA secret is in play.
                try { key.DeleteValue("DefaultPassword", throwOnMissingValue: false); } catch { }
            }
        }

        /// <summary>Tear down autologon and wipe the stored secret. Safe to call twice.</summary>
        internal static void Disable()
        {
            try
            {
                StoreLsaSecret(SecretKeyName, null);           // null => delete secret
                if (SecretExists(SecretKeyName))
                    Program.Log("WARNING: DefaultPassword LSA secret still present after wipe.");
                else
                    Program.Log("Autologon disabled; password secret wiped.");
            }
            catch { }

            string prior = "0";
            using (var state = Registry.LocalMachine.OpenSubKey(StateKey, writable: true))
            {
                if (state != null)
                {
                    if (state.GetValue(PriorAutoFlag) is string s) prior = s;
                    try { state.DeleteValue(PriorAutoFlag, throwOnMissingValue: false); } catch { }
                    try { state.DeleteValue(ActiveFlag,    throwOnMissingValue: false); } catch { }
                }
            }

            using (var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true))
            {
                if (key == null) return;
                key.SetValue("AutoAdminLogon", string.IsNullOrEmpty(prior) ? "0" : prior);
                try { key.DeleteValue("AutoLogonCount",  throwOnMissingValue: false); } catch { }
                try { key.DeleteValue("DefaultPassword", throwOnMissingValue: false); } catch { }
            }
        }

        /// <summary>True if this tool left autologon enabled (e.g. a crashed run).</summary>
        internal static bool IsActive()
        {
            using (var state = Registry.LocalMachine.OpenSubKey(StateKey))
                return state?.GetValue(ActiveFlag) != null;
        }

        // ── LSA secret storage ────────────────────────────────────────────

        private static void StoreLsaSecret(string keyName, string secret)
        {
            LSA_OBJECT_ATTRIBUTES oa = new LSA_OBJECT_ATTRIBUTES();
            LSA_UNICODE_STRING system = new LSA_UNICODE_STRING(); // null => local machine

            IntPtr policy;
            uint status = LsaOpenPolicy(ref system, ref oa, POLICY_CREATE_SECRET, out policy);
            if (status != 0)
                throw new InvalidOperationException(
                    "LsaOpenPolicy loi: " + LsaNtStatusToWinError(status));

            LSA_UNICODE_STRING kName = InitLsaString(keyName);
            LSA_UNICODE_STRING data  = InitLsaString(secret); // null => delete secret
            try
            {
                uint r = LsaStorePrivateData(policy, ref kName, ref data);
                if (r != 0)
                    throw new InvalidOperationException(
                        "LsaStorePrivateData loi: " + LsaNtStatusToWinError(r));
            }
            finally
            {
                if (kName.Buffer != IntPtr.Zero) Marshal.FreeHGlobal(kName.Buffer);
                if (data.Buffer  != IntPtr.Zero) Marshal.FreeHGlobal(data.Buffer);
                LsaClose(policy);
            }
        }

        /// <summary>True if the named LSA secret currently holds a non-empty value.</summary>
        private static bool SecretExists(string keyName)
        {
            LSA_OBJECT_ATTRIBUTES oa = new LSA_OBJECT_ATTRIBUTES();
            LSA_UNICODE_STRING system = new LSA_UNICODE_STRING();
            IntPtr policy;
            if (LsaOpenPolicy(ref system, ref oa, POLICY_GET_PRIVATE_INFORMATION, out policy) != 0)
                return false;

            LSA_UNICODE_STRING kName = InitLsaString(keyName);
            IntPtr data = IntPtr.Zero;
            try
            {
                uint r = LsaRetrievePrivateData(policy, ref kName, out data);
                if (r != 0 || data == IntPtr.Zero) return false;
                LSA_UNICODE_STRING val = (LSA_UNICODE_STRING)Marshal.PtrToStructure(data, typeof(LSA_UNICODE_STRING));
                return val.Length > 0;
            }
            catch { return false; }
            finally
            {
                if (kName.Buffer != IntPtr.Zero) Marshal.FreeHGlobal(kName.Buffer);
                if (data != IntPtr.Zero) LsaFreeMemory(data);
                LsaClose(policy);
            }
        }

        private static LSA_UNICODE_STRING InitLsaString(string s)
        {
            var lus = new LSA_UNICODE_STRING();
            if (s == null)
            {
                lus.Buffer = IntPtr.Zero;
                lus.Length = 0;
                lus.MaximumLength = 0;
                return lus;
            }
            lus.Buffer        = Marshal.StringToHGlobalUni(s);
            lus.Length        = (ushort)(s.Length * 2);
            lus.MaximumLength = (ushort)((s.Length + 1) * 2);
            return lus;
        }

        // ── P/Invoke ──────────────────────────────────────────────────────

        private const uint POLICY_CREATE_SECRET          = 0x00000020;
        private const uint POLICY_GET_PRIVATE_INFORMATION = 0x00000004;

        [StructLayout(LayoutKind.Sequential)]
        private struct LSA_UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LSA_OBJECT_ATTRIBUTES
        {
            public int    Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint   Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern uint LsaOpenPolicy(
            ref LSA_UNICODE_STRING SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            uint DesiredAccess,
            out IntPtr PolicyHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern uint LsaStorePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            ref LSA_UNICODE_STRING PrivateData);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern uint LsaRetrievePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            out IntPtr PrivateData);

        [DllImport("advapi32.dll")]
        private static extern uint LsaFreeMemory(IntPtr Buffer);

        [DllImport("advapi32.dll")]
        private static extern uint LsaClose(IntPtr PolicyHandle);

        [DllImport("advapi32.dll")]
        private static extern int LsaNtStatusToWinError(uint status);
    }

    /// <summary>Tiny modal prompt for the current user's Windows password.</summary>
    internal static class PasswordPrompt
    {
        /// <returns>true = user confirmed (password may be empty); false = skipped.</returns>
        internal static bool TryGet(out string password)
        {
            password = null;
            using (var f = new Form())
            using (var lbl = new Label())
            using (var txt = new TextBox())
            using (var ok = new Button())
            using (var skip = new Button())
            {
                f.Text            = Loc.T("pwd_title");
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition   = FormStartPosition.CenterScreen;
                f.MinimizeBox     = false;
                f.MaximizeBox     = false;
                f.ClientSize      = new Size(420, 170);

                lbl.SetBounds(12, 12, 396, 52);
                lbl.Text = Loc.T("pwd_prompt");

                txt.SetBounds(12, 72, 396, 24);
                txt.UseSystemPasswordChar = true;

                ok.Text = Loc.T("pwd_ok");
                ok.SetBounds(248, 116, 75, 30);
                ok.DialogResult = DialogResult.OK;

                skip.Text = Loc.T("pwd_skip");
                skip.SetBounds(333, 116, 75, 30);
                skip.DialogResult = DialogResult.Cancel;

                f.Controls.Add(lbl);
                f.Controls.Add(txt);
                f.Controls.Add(ok);
                f.Controls.Add(skip);
                f.AcceptButton = ok;
                f.CancelButton = skip;

                if (f.ShowDialog() == DialogResult.OK)
                {
                    password = txt.Text;
                    return true;
                }
                return false;
            }
        }
    }
}
