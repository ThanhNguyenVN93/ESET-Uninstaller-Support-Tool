using System;
using System.Drawing;
using System.Windows.Forms;

namespace frmesetunins
{
    public partial class Form1 : Form
    {
        private Label  lblReminder;
        private Label  lblStatus;
        private Label  lblCountdown;
        private Button btnAction;
        private Button btnCancel;
        private Button btnLang;
        private Button   btnReport;
        private Button   btnAbort;
        private CheckBox chkSelect;

        private Timer  countdownTimer;
        private Timer  abortTimer;
        private int    countdownSeconds = 10;
        private int    abortSeconds     = 20;

        private enum Phase { Idle, Aborting, Done }
        private Phase phase = Phase.Idle;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblReminder = MakeLabel(new Point(12, 40), new Size(496, 34), 8.5f, FontStyle.Italic,
                Color.FromArgb(120, 120, 120), "");
            Controls.Add(lblReminder);

            lblStatus = MakeLabel(new Point(0, 78), new Size(520, 34), 11, FontStyle.Regular,
                Color.FromArgb(0, 120, 215), "");
            Controls.Add(lblStatus);

            lblCountdown = MakeLabel(new Point(0, 114), new Size(520, 28), 10, FontStyle.Bold,
                Color.FromArgb(200, 0, 0), "");
            Controls.Add(lblCountdown);

            btnAction = new Button
            {
                Location  = new Point(96, 150),
                Size      = new Size(210, 38),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += (s, ev) => RestartToSafeMode();
            Controls.Add(btnAction);

            btnCancel = new Button
            {
                Location  = new Point(316, 150),
                Size      = new Size(100, 38),
                Font      = new Font("Segoe UI", 10, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnCancel.Click += (s, ev) => CancelAndExit();
            Controls.Add(btnCancel);

            btnAbort = new Button
            {
                Location  = new Point(150, 150),
                Size      = new Size(220, 38),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Visible   = false,
            };
            btnAbort.FlatAppearance.BorderSize = 0;
            btnAbort.Click += (s, ev) => AbortRestart();
            Controls.Add(btnAbort);

            chkSelect = new CheckBox
            {
                Location = new Point(14, 196),
                AutoSize = true,
                Font     = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            };
            Controls.Add(chkSelect);

            btnReport = new Button
            {
                Location  = new Point(12, 216),
                Size      = new Size(200, 28),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnReport.Click += (s, ev) => Program.OpenIssues();
            Controls.Add(btnReport);

            btnLang = new Button
            {
                Location  = new Point(426, 10),
                Size      = new Size(82, 26),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnLang.Click += (s, ev) => { Loc.Toggle(); ApplyLanguage(); };
            Controls.Add(btnLang);

            ApplyLanguage();

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        // Re-applies every visible string for the current language.
        private void ApplyLanguage()
        {
            Text            = Loc.T("app_title");
            btnAction.Text  = Loc.T("btn_restart");
            btnCancel.Text  = Loc.T("btn_cancel");
            btnLang.Text    = Loc.T("btn_lang");
            btnReport.Text  = Loc.T("btn_report");
            btnAbort.Text   = Loc.T("abort_btn");
            chkSelect.Text  = Loc.T("chk_select");
            lblReminder.Text = Loc.T("export_reminder");

            if (phase == Phase.Idle)
            {
                lblStatus.Text    = Loc.T("status_normal");
                lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);
            }
            else if (phase == Phase.Aborting)
            {
                lblStatus.Text = string.Format(Loc.T("abort_text"), abortSeconds);
            }
            else // Done (aborted)
            {
                lblStatus.Text = Loc.T("aborted");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            abortTimer?.Stop();
            abortTimer?.Dispose();
            base.OnFormClosing(e);
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);
            if (countdownSeconds <= 0)
            {
                countdownTimer.Stop();
                lblCountdown.Visible = false;
                RestartToSafeMode();
            }
        }

        private void CancelAndExit()
        {
            // Nothing has been changed yet (triggers are only set once
            // RestartToSafeMode runs), so cancelling just stops and closes.
            countdownTimer.Stop();
            Close();
        }

        private void RestartToSafeMode()
        {
            try
            {
                countdownTimer.Stop();
                lblCountdown.Visible = false;
                btnAction.Enabled    = false;
                btnCancel.Enabled    = false;
                chkSelect.Enabled    = false;

                // Copy ourselves to local disk and point the triggers there, so
                // the relaunch works even when run from a network share (Safe
                // Mode minimal has no network access).
                string exe = Program.StageSelfLocally();

                // Download the latest ESET Uninstaller now, while networking is
                // available (Safe Mode minimal has none). Best-effort.
                lblStatus.Text = Loc.T("status_downloading");
                Application.DoEvents();
                Program.TryStageLatestUninstaller(out string _);

                lblStatus.Text = Loc.T("status_preparing");
                Application.DoEvents();

                // Arm both relaunch triggers + the Safe Mode boot flags.
                Program.ArmSafeMode(exe, chkSelect.Checked);

                // Optional: enable autologon so the Safe Mode pass is unattended.
                if (PasswordPrompt.TryGet(out string pwd))
                {
                    try { AutoLogon.Enable(pwd); }
                    catch (Exception ax)
                    {
                        MessageBox.Show(Loc.T("autologon_fail") + ax.Message,
                            Loc.T("autologon_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                EnterAbortPhase();
            }
            catch (Exception ex)
            {
                lblStatus.Text       = Loc.T("err_prefix") + ex.Message;
                lblCountdown.Visible = true;
                btnAction.Enabled    = true;
                btnCancel.Enabled    = true;
                countdownTimer.Start();
            }
        }

        // After arming, give the user a window to abort before the restart fires.
        private void EnterAbortPhase()
        {
            phase = Phase.Aborting;
            lblReminder.Visible  = false;
            lblCountdown.Visible = false;
            btnAction.Visible    = false;
            btnCancel.Visible    = false;
            chkSelect.Visible    = false;
            btnAbort.Visible     = true;
            lblStatus.ForeColor  = Color.FromArgb(200, 0, 0);
            lblStatus.Text       = string.Format(Loc.T("abort_text"), abortSeconds);

            Program.RunSysCmd("shutdown.exe", "/r /t " + abortSeconds);

            abortTimer = new Timer { Interval = 1000 };
            abortTimer.Tick += (s, e) =>
            {
                abortSeconds--;
                if (abortSeconds <= 0)
                {
                    abortTimer.Stop();
                    Close(); // let the scheduled restart proceed
                    return;
                }
                lblStatus.Text = string.Format(Loc.T("abort_text"), abortSeconds);
            };
            abortTimer.Start();
        }

        private void AbortRestart()
        {
            abortTimer?.Stop();
            try { Program.RunSysCmd("shutdown.exe", "/a"); } catch { }
            try { Program.UndoAllBootTriggers(); } catch { }

            phase = Phase.Done;
            btnAbort.Enabled    = false;
            btnAbort.Visible    = false;
            lblStatus.ForeColor = Color.FromArgb(0, 150, 60);
            lblStatus.Text      = Loc.T("aborted");
        }

        private static Label MakeLabel(Point loc, Size size, float fontSize,
            FontStyle style, Color fore, string text)
        {
            return new Label
            {
                AutoSize  = false,
                Location  = loc,
                Size      = size,
                Font      = new Font("Segoe UI", fontSize, style),
                ForeColor = fore,
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = text,
            };
        }
    }
}
