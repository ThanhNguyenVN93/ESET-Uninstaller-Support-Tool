using System;
using System.Drawing;
using System.Windows.Forms;

namespace frmesetunins
{
    public partial class Form1 : Form
    {
        private Label    lblReminder;
        private Label    lblStatus;
        private Label    lblCountdown;
        private Button   btnAction;
        private Button   btnNoSafeMode;
        private Button   btnCancel;
        private Button   btnLang;
        private Button   btnReport;
        private Button   btnAbout;
        private Button   btnAbort;
        private CheckBox chkSelect;
        private CheckBox chkClean;
        private ProgressBar pbWork;
        private ToolTip  toolTip;

        private Timer  countdownTimer;
        private Timer  abortTimer;
        private int    countdownSeconds = 10;
        private int    abortSeconds     = 20;

        private enum Phase { Idle, Aborting, Done }
        private Phase phase = Phase.Idle;
        private bool  noEset;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblReminder = MakeLabel(new Point(16, 42), new Size(528, 52), 8.5f, FontStyle.Italic,
                Color.FromArgb(110, 110, 110), "");
            Controls.Add(lblReminder);

            lblStatus = MakeLabel(new Point(0, 100), new Size(560, 30), 11, FontStyle.Regular,
                Color.FromArgb(0, 120, 215), "");
            Controls.Add(lblStatus);

            lblCountdown = MakeLabel(new Point(0, 132), new Size(560, 26), 10, FontStyle.Bold,
                Color.FromArgb(200, 0, 0), "");
            Controls.Add(lblCountdown);

            btnAction = new Button
            {
                Location  = new Point(120, 162),
                Size      = new Size(320, 40),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += (s, ev) => RestartToSafeMode();
            Controls.Add(btnAction);

            btnNoSafeMode = new Button
            {
                Location  = new Point(120, 208),
                Size      = new Size(320, 32),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnNoSafeMode.Click += (s, ev) => RunNoSafeMode();
            Controls.Add(btnNoSafeMode);

            chkSelect = new CheckBox { Location = new Point(16, 248), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            Controls.Add(chkSelect);

            chkClean = new CheckBox { Location = new Point(16, 270), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), Checked = true };
            Controls.Add(chkClean);

            btnAbort = new Button
            {
                Location  = new Point(120, 162),
                Size      = new Size(320, 40),
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

            btnReport = new Button
            {
                Location  = new Point(16, 300),
                Size      = new Size(200, 30),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnReport.Click += (s, ev) => Program.OpenIssues();
            Controls.Add(btnReport);

            btnAbout = new Button
            {
                Location  = new Point(228, 300),
                Size      = new Size(104, 30),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAbout.Click += (s, ev) => ShowAbout();
            Controls.Add(btnAbout);

            btnCancel = new Button
            {
                Location  = new Point(424, 300),
                Size      = new Size(120, 30),
                Font      = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnCancel.Click += (s, ev) => CancelAndExit();
            Controls.Add(btnCancel);

            btnLang = new Button
            {
                Location  = new Point(466, 10),
                Size      = new Size(82, 26),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnLang.Click += (s, ev) => { Loc.Toggle(); ApplyLanguage(); };
            Controls.Add(btnLang);

            pbWork = new ProgressBar
            {
                Location = new Point(120, 134),
                Size     = new Size(320, 14),
                Style    = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible  = false,
            };
            Controls.Add(pbWork);

            toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 100 };

            ApplyLanguage();

            // Background integrity check of the embedded uninstaller (best-effort).
            var vt = new System.Threading.Thread(() =>
            {
                string info;
                bool ok = Program.ValidateEmbeddedUninstaller(out info);
                Program.Log("Embedded uninstaller signed=" + ok + " " + info);
            });
            vt.IsBackground = true;
            vt.Start();

            // If ESET isn't installed, don't auto-count down into a pointless
            // Safe Mode trip — tell the user and let them act manually.
            noEset = !Program.EsetInstalled();
            ApplyLanguage();

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
            if (!noEset) countdownTimer.Start();
            else lblCountdown.Visible = false;
        }

        private void ApplyLanguage()
        {
            Text              = Loc.T("app_title") + "  v" + Program.AppVersion;
            btnAction.Text    = Loc.T("btn_restart");
            btnNoSafeMode.Text = Loc.T("btn_nosafemode");
            btnCancel.Text    = Loc.T("btn_cancel");
            btnLang.Text      = Loc.T("btn_lang");
            btnReport.Text    = Loc.T("btn_report");
            btnAbout.Text     = Loc.T("btn_about");
            btnAbort.Text     = Loc.T("abort_btn");
            chkSelect.Text    = Loc.T("chk_select");
            chkClean.Text     = Loc.T("chk_clean");
            lblReminder.Text  = Loc.T("export_reminder") + "\n" + Loc.T("info_password");

            if (toolTip != null)
            {
                toolTip.SetToolTip(btnAction,     Loc.T("tip_restart"));
                toolTip.SetToolTip(btnNoSafeMode, Loc.T("tip_nosafemode"));
                toolTip.SetToolTip(chkSelect,     Loc.T("tip_select"));
                toolTip.SetToolTip(chkClean,      Loc.T("tip_clean"));
            }

            if (phase == Phase.Idle)
            {
                if (noEset)
                {
                    lblStatus.Text      = Loc.T("no_eset_normal");
                    lblStatus.ForeColor = Color.FromArgb(0, 150, 60);
                }
                else
                {
                    lblStatus.Text    = Loc.T("status_normal");
                    lblCountdown.Text = string.Format(Loc.T("countdown"), countdownSeconds);
                }
            }
            else if (phase == Phase.Aborting)
            {
                lblStatus.Text = string.Format(Loc.T("abort_text"), abortSeconds);
            }
            else
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
            countdownTimer.Stop();
            Close();
        }

        private void SetIdleControls(bool enabled)
        {
            btnAction.Enabled     = enabled;
            btnNoSafeMode.Enabled = enabled;
            btnCancel.Enabled     = enabled;
            chkSelect.Enabled     = enabled;
            chkClean.Enabled      = enabled;
        }

        private void RestartToSafeMode()
        {
            countdownTimer.Stop();

            // Pre-flight checklist (Wi-Fi off in Safe Mode, back up data).
            if (MessageBox.Show(Loc.T("preflight"), Loc.T("preflight_title"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
            {
                lblStatus.Text = Loc.T("status_normal");
                lblCountdown.Visible = true;
                SetIdleControls(true);
                return;
            }

            try
            {
                lblCountdown.Visible = false;
                SetIdleControls(false);
                pbWork.Visible = true;

                // Copy ourselves to local disk and point triggers there, so the
                // relaunch works even from a network share (Safe Mode has no net).
                string exe = Program.StageSelfLocally();

                // Safety net before anything destructive (needs System Protection).
                Program.CreateRestorePoint("Before ESET removal (ESET Uninstaller Support Tool)");

                lblStatus.Text = Loc.T("status_downloading");
                Application.DoEvents();
                Program.TryStageLatestUninstaller(out string _);

                lblStatus.Text = Loc.T("status_preparing");
                Application.DoEvents();

                Program.ArmSafeMode(exe, chkSelect.Checked, chkClean.Checked);

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
                pbWork.Visible       = false;
                lblStatus.Text       = Loc.T("err_prefix") + ex.Message;
                lblCountdown.Visible = true;
                SetIdleControls(true);
            }
        }

        // Fallback: run the uninstaller now in normal mode (/nosafemode).
        private void RunNoSafeMode()
        {
            countdownTimer.Stop();
            if (MessageBox.Show(Loc.T("nosafemode_confirm"), Loc.T("nosafemode_title"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                lblCountdown.Visible = true;
                return;
            }

            bool clean = chkClean.Checked;
            lblCountdown.Visible = false;
            SetIdleControls(false);
            pbWork.Visible = true;
            lblStatus.Text = Loc.T("working");

            var t = new System.Threading.Thread(() =>
            {
                Program.RunNormalModeUninstall(clean);
                try { BeginInvoke(new Action(ShowResultAndClose)); } catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                string.Format(Loc.T("about_text"), Program.AppVersion, Program.IssuesUrl),
                Loc.T("btn_about"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowResultAndClose()
        {
            using (var rf = new ResultForm()) rf.ShowDialog(this);
            Close();
        }

        private void EnterAbortPhase()
        {
            phase = Phase.Aborting;
            pbWork.Visible        = false;
            lblReminder.Visible   = false;
            lblCountdown.Visible  = false;
            btnAction.Visible     = false;
            btnNoSafeMode.Visible = false;
            btnCancel.Visible     = false;
            chkSelect.Visible     = false;
            chkClean.Visible      = false;
            btnAbort.Visible      = true;
            lblStatus.ForeColor   = Color.FromArgb(200, 0, 0);
            lblStatus.Text        = string.Format(Loc.T("abort_text"), abortSeconds);

            Program.RunSysCmd("shutdown.exe", "/r /t " + abortSeconds);

            abortTimer = new Timer { Interval = 1000 };
            abortTimer.Tick += (s, e) =>
            {
                abortSeconds--;
                if (abortSeconds <= 0)
                {
                    abortTimer.Stop();
                    Close();
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
