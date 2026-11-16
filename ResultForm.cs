using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace frmesetunins
{
    /// <summary>
    /// Shown on the first normal boot after a Safe Mode run (launched via a
    /// RunOnce "--result"). Reports the outcome recorded during Safe Mode and
    /// offers to open the detailed log or report an issue.
    /// </summary>
    internal sealed class ResultForm : Form
    {
        public ResultForm()
        {
            string result = Program.GetAndClearLastResult() ?? "unknown";

            string headline;
            Color  color;
            if (result.StartsWith("success", StringComparison.OrdinalIgnoreCase))
            {
                headline = Loc.T("result_success");
                color    = Color.FromArgb(0, 150, 60);
            }
            else if (result.StartsWith("warning", StringComparison.OrdinalIgnoreCase))
            {
                headline = Loc.T("result_warning");
                color    = Color.FromArgb(200, 120, 0);
            }
            else if (result.StartsWith("none", StringComparison.OrdinalIgnoreCase))
            {
                headline = Loc.T("result_none");
                color    = Color.FromArgb(0, 120, 215);
            }
            else if (result.StartsWith("error", StringComparison.OrdinalIgnoreCase))
            {
                headline = Loc.T("result_error");
                color    = Color.FromArgb(200, 0, 0);
            }
            else
            {
                headline = Loc.T("result_unknown");
                color    = Color.FromArgb(90, 90, 90);
            }

            Text            = Loc.T("result_title");
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox     = false;
            MaximizeBox     = false;
            ClientSize      = new Size(540, 380);

            var lblHead = new Label
            {
                AutoSize  = false,
                Location  = new Point(16, 16),
                Size      = new Size(508, 60),
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = color,
                Text      = headline,
            };
            Controls.Add(lblHead);

            var txtLog = new TextBox
            {
                Location   = new Point(16, 86),
                Size       = new Size(508, 180),
                Multiline  = true,
                ReadOnly   = true,
                ScrollBars = ScrollBars.Vertical,
                Font       = new Font("Consolas", 9),
                Text       = ReadLogTail(),
            };
            Controls.Add(txtLog);

            // Row 1: open logs + restore network.
            var btnOpen = new Button
            {
                Text     = Loc.T("result_openlog"),
                Location = new Point(16, 278),
                Size     = new Size(150, 32),
            };
            btnOpen.Click += (s, e) =>
            {
                try { Process.Start("notepad.exe", Program.LogFilePath); } catch { }
            };
            Controls.Add(btnOpen);

            string esetLog = Program.LatestEsetLogPath();
            var btnEsetLog = new Button
            {
                Text     = Loc.T("result_openesetlog"),
                Location = new Point(176, 278),
                Size     = new Size(150, 32),
                Enabled  = esetLog != null,
            };
            btnEsetLog.Click += (s, e) =>
            {
                try { if (esetLog != null) Process.Start("notepad.exe", esetLog); } catch { }
            };
            Controls.Add(btnEsetLog);

            var btnRestoreNet = new Button
            {
                Text     = Loc.T("result_restorenet"),
                Location = new Point(336, 278),
                Size     = new Size(188, 32),
                Enabled  = Program.LatestNetworkBackupPath() != null,
            };
            btnRestoreNet.Click += (s, e) =>
            {
                bool ok = Program.RestoreNetwork();
                MessageBox.Show(Loc.T(ok ? "restored_ok" : "restored_fail"),
                    Loc.T("result_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(btnRestoreNet);

            // Row 2: report + close.
            var btnReport = new Button
            {
                Text     = Loc.T("btn_report"),
                Location = new Point(16, 320),
                Size     = new Size(200, 32),
            };
            btnReport.Click += (s, e) => Program.OpenIssues();
            Controls.Add(btnReport);

            var btnClose = new Button
            {
                Text         = Loc.T("result_close"),
                Location     = new Point(444, 320),
                Size         = new Size(80, 32),
                DialogResult = DialogResult.OK,
            };
            Controls.Add(btnClose);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private static string ReadLogTail()
        {
            try
            {
                if (!File.Exists(Program.LogFilePath)) return Loc.T("result_nolog");
                string[] lines = File.ReadAllLines(Program.LogFilePath);
                int take = Math.Min(lines.Length, 40);
                return string.Join(Environment.NewLine, lines, lines.Length - take, take);
            }
            catch (Exception ex)
            {
                return "(" + ex.Message + ")";
            }
        }
    }
}
