using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frmesetunins
{
    public partial class Form1 : Form
    {
        private Label  lblStatus;
        private Label  lblCountdown;
        private Button btnAction;
        private Timer  countdownTimer;
        private int    countdownSeconds = 10;

        private const string RunOnceKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string RunOnceName = "FrmEseUnins";

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblStatus = MakeLabel(new Point(0, 30), new Size(500, 60), 11, FontStyle.Regular,
                Color.FromArgb(0, 120, 215), "He thong dang o che do binh thuong.");
            Controls.Add(lblStatus);

            lblCountdown = MakeLabel(new Point(0, 100), new Size(500, 30), 10, FontStyle.Bold,
                Color.FromArgb(200, 0, 0), $"Dem nguoc: {countdownSeconds} giay");
            Controls.Add(lblCountdown);

            btnAction = new Button
            {
                Text      = "Restart to Safe Mode",
                Location  = new Point(170, 145),
                Size      = new Size(160, 35),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += (s, ev) => RestartToSafeMode();
            Controls.Add(btnAction);

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            base.OnFormClosing(e);
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            lblCountdown.Text = $"Dem nguoc: {countdownSeconds} giay";
            if (countdownSeconds <= 0)
            {
                countdownTimer.Stop();
                lblCountdown.Visible = false;
                RestartToSafeMode();
            }
        }

        private void RestartToSafeMode()
        {
            try
            {
                countdownTimer.Stop();
                lblStatus.Text       = "Dang khoi dong lai vao Safe Mode...";
                lblCountdown.Visible = false;
                btnAction.Enabled    = false;

                using (var key = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: true))
                    key?.SetValue(RunOnceName, $"\"{Application.ExecutablePath}\"");

                Program.RunSysCmd("bcdedit.exe", "/set {current} safeboot minimal");
                Program.RunSysCmd("shutdown.exe", "/r /t 5");
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text       = "Loi: " + ex.Message;
                lblCountdown.Visible = true;
                btnAction.Enabled    = true;
                countdownTimer.Start();
            }
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
