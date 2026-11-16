using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace frmesetunins
{
    /// <summary>
    /// Integrity checks for an uninstaller binary before it is executed:
    /// a SHA-256 for the audit log, and an Authenticode verification that the
    /// file is validly signed by ESET. We cannot pin a fixed hash (ESET's
    /// "latest" build changes), so we trust the publisher's signature instead.
    /// </summary>
    internal static class Verify
    {
        internal static string Sha256(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = System.IO.File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
            }
            catch { return "(hash failed)"; }
        }

        /// <summary>
        /// True when the file carries a cryptographically valid Authenticode
        /// signature AND the signer's certificate subject mentions ESET.
        /// </summary>
        /// <summary>Read the Authenticode signer's certificate subject, if any.</summary>
        internal static bool TryGetSigner(string path, out string signer)
        {
            signer = "(none)";
            try
            {
                using (var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                {
                    signer = cert.Subject;
                    return true;
                }
            }
            catch { return false; }
        }

        /// <summary>True if the signer's certificate subject mentions ESET.</summary>
        internal static bool IsEsetSigner(string signer)
        {
            return signer != null && signer.IndexOf("ESET", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ── WinVerifyTrust ────────────────────────────────────────────────

        /// <summary>
        /// Raw WinVerifyTrust result: 0 (ERROR_SUCCESS) = the file's Authenticode
        /// signature is cryptographically valid and chains to a trusted root.
        /// A non-zero code can also mean the chain could not be built (e.g. an
        /// offline machine missing intermediate CAs), not only tampering.
        /// </summary>
        internal static uint WinVerifyCode(string path)
        {
            Guid action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE"); // GENERIC_VERIFY_V2

            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct       = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
                pcwszFilePath  = Marshal.StringToCoTaskMemUni(path),
                hFile          = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero,
            };

            IntPtr pFile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
            IntPtr pData = IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);

                var data = new WINTRUST_DATA
                {
                    cbStruct            = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA)),
                    dwUIChoice          = WTD_UI_NONE,
                    fdwRevocationChecks = WTD_REVOKE_NONE,
                    dwUnionChoice       = WTD_CHOICE_FILE,
                    pFile               = pFile,
                    dwStateAction       = WTD_STATEACTION_VERIFY,
                    dwProvFlags         = WTD_SAFER_FLAG,
                };

                pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA)));
                Marshal.StructureToPtr(data, pData, false);

                uint result = WinVerifyTrust(IntPtr.Zero, ref action, pData);

                // Always close the state to release the provider's resources.
                data.dwStateAction = WTD_STATEACTION_CLOSE;
                Marshal.StructureToPtr(data, pData, false);
                WinVerifyTrust(IntPtr.Zero, ref action, pData);

                return result; // 0 == ERROR_SUCCESS (trusted & valid)
            }
            catch { return 0x80004005; } // E_FAIL on marshalling error
            finally
            {
                if (fileInfo.pcwszFilePath != IntPtr.Zero) Marshal.FreeCoTaskMem(fileInfo.pcwszFilePath);
                if (pFile != IntPtr.Zero) Marshal.FreeHGlobal(pFile);
                if (pData != IntPtr.Zero) Marshal.FreeHGlobal(pData);
            }
        }

        private const uint WTD_UI_NONE          = 2;
        private const uint WTD_REVOKE_NONE       = 0;
        private const uint WTD_CHOICE_FILE       = 1;
        private const uint WTD_STATEACTION_VERIFY = 1;
        private const uint WTD_STATEACTION_CLOSE  = 2;
        private const uint WTD_SAFER_FLAG         = 0x100;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_FILE_INFO
        {
            public uint   cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint   cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint   dwUIChoice;
            public uint   fdwRevocationChecks;
            public uint   dwUnionChoice;
            public IntPtr pFile;
            public uint   dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint   dwProvFlags;
            public uint   dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, IntPtr pWVTData);
    }
}
