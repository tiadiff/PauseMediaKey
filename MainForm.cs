using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PauseMediaKey
{
    public class MainForm : Form
    {
        private ServiceConfig _config;
        private readonly System.Windows.Forms.Timer _pollTimer;

        private Panel _cardPanel = null!;
        private Label _titleLabel = null!;
        private Label _subtitleLabel = null!;
        private Label _statusBadge = null!;
        private Label _statusDesc = null!;
        private Label _serviceProcessLabel = null!;
        private Label _lastTriggerLabel = null!;
        private Button _toggleButton = null!;
        private CheckBox _chkStartWithWindows = null!;
        private Button _restartServiceBtn = null!;
        private Label _footerTip = null!;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainForm()
        {
            _config = ServiceConfig.Load();
            _pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _pollTimer.Tick += (s, e) => PollServiceStatus();

            InitializeComponent();
            ApplyTheme();

            EnsureServiceRunning();
            UpdateUI();

            _pollTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch
            {
                // Older Windows versions ignore this
            }
        }

        private void InitializeComponent()
        {
            Text = "PauseMediaKey";
            ClientSize = new Size(470, 360);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 25, 28);
            ForeColor = Color.FromArgb(240, 242, 245);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            // Title
            _titleLabel = new Label
            {
                Text = "PauseMediaKey",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(245, 246, 248),
                Location = new Point(24, 18),
                AutoSize = true
            };

            _subtitleLabel = new Label
            {
                Text = "Independent Background Service Controller",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(145, 152, 161),
                Location = new Point(25, 46),
                AutoSize = true
            };

            // Status Card Panel
            _cardPanel = new Panel
            {
                Location = new Point(24, 76),
                Size = new Size(422, 122),
                BackColor = Color.FromArgb(34, 37, 41)
            };
            _cardPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(48, 52, 58), 1);
                var rect = _cardPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);

                // Draw status indicator circle
                var dotColor = _config.IsEnabled ? Color.FromArgb(46, 160, 67) : Color.FromArgb(120, 126, 134);
                using var dotBrush = new SolidBrush(dotColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(dotBrush, 18, 19, 10, 10);
            };

            _statusBadge = new Label
            {
                Location = new Point(36, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point)
            };

            _statusDesc = new Label
            {
                Location = new Point(18, 42),
                Size = new Size(386, 34),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(170, 175, 183)
            };

            _serviceProcessLabel = new Label
            {
                Location = new Point(18, 78),
                Size = new Size(386, 18),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(135, 142, 151),
                Text = "Service: Initializing..."
            };

            _lastTriggerLabel = new Label
            {
                Location = new Point(18, 98),
                Size = new Size(386, 18),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(120, 126, 134),
                Text = "Ready - Waiting for keypress"
            };

            _cardPanel.Controls.Add(_statusBadge);
            _cardPanel.Controls.Add(_statusDesc);
            _cardPanel.Controls.Add(_serviceProcessLabel);
            _cardPanel.Controls.Add(_lastTriggerLabel);

            // Toggle Button
            _toggleButton = new Button
            {
                Location = new Point(24, 210),
                Size = new Size(422, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            _toggleButton.FlatAppearance.BorderSize = 0;
            _toggleButton.Click += (s, e) => ToggleState();

            // Checkbox: Start with Windows
            _chkStartWithWindows = new CheckBox
            {
                Text = "Start background service with Windows",
                Location = new Point(26, 266),
                AutoSize = true,
                Checked = _config.Autostart,
                ForeColor = Color.FromArgb(200, 205, 212),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point)
            };
            _chkStartWithWindows.CheckedChanged += (s, e) =>
            {
                _config.Autostart = _chkStartWithWindows.Checked;
                _config.Save();
                ServiceConfig.NotifyService();
            };

            // Restart Service Button
            _restartServiceBtn = new Button
            {
                Text = "Restart Service",
                Location = new Point(326, 262),
                Size = new Size(120, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.FromArgb(42, 45, 50),
                ForeColor = Color.FromArgb(180, 186, 194),
                Cursor = Cursors.Hand
            };
            _restartServiceBtn.FlatAppearance.BorderSize = 1;
            _restartServiceBtn.FlatAppearance.BorderColor = Color.FromArgb(58, 62, 69);
            _restartServiceBtn.Click += (s, e) => RestartService();

            // Footer tip
            _footerTip = new Label
            {
                Text = "You can close this window anytime. The service continues running independently.",
                Location = new Point(24, 312),
                Size = new Size(422, 32),
                ForeColor = Color.FromArgb(120, 127, 137),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Controls.Add(_titleLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_cardPanel);
            Controls.Add(_toggleButton);
            Controls.Add(_chkStartWithWindows);
            Controls.Add(_restartServiceBtn);
            Controls.Add(_footerTip);
        }

        private void ApplyTheme()
        {
            UpdateAppIcon();
        }

        private void UpdateAppIcon()
        {
            bool active = _config.IsEnabled;
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var bgBrush = new SolidBrush(Color.FromArgb(34, 37, 41));
                g.FillRectangle(bgBrush, 0, 0, 32, 32);

                var color = active ? Color.FromArgb(46, 160, 67) : Color.FromArgb(135, 142, 151);
                using var symbolBrush = new SolidBrush(color);

                g.FillRectangle(symbolBrush, 7, 7, 6, 18);
                g.FillRectangle(symbolBrush, 19, 7, 6, 18);
            }

            Icon = Icon.FromHandle(bmp.GetHicon());
        }

        private void UpdateUI()
        {
            bool active = _config.IsEnabled;

            _statusBadge.Text = active ? "SERVICE: ACTIVE" : "SERVICE: INACTIVE";
            _statusBadge.ForeColor = active ? Color.FromArgb(46, 160, 67) : Color.FromArgb(145, 152, 161);

            _statusDesc.Text = active
                ? "Key remapping is active. Pressing 'Pause / Interr' sends Play/Pause to Windows media players."
                : "Key remapping is disabled. The 'Pause / Interr' key performs its default Windows action.";

            if (active)
            {
                _toggleButton.Text = "Disable Remapping";
                _toggleButton.BackColor = Color.FromArgb(48, 52, 58);
                _toggleButton.ForeColor = Color.FromArgb(240, 242, 245);
                _toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 67, 75);
            }
            else
            {
                _toggleButton.Text = "Enable Remapping";
                _toggleButton.BackColor = Color.FromArgb(46, 160, 67);
                _toggleButton.ForeColor = Color.White;
                _toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(56, 178, 77);
            }

            if (_config.LastTriggered.HasValue)
            {
                _lastTriggerLabel.Text = $"Last event: Play/Pause sent at {_config.LastTriggered.Value:HH:mm:ss}";
            }

            _cardPanel.Invalidate();
            UpdateAppIcon();
        }

        private void ToggleState()
        {
            _config.IsEnabled = !_config.IsEnabled;
            _config.Save();
            ServiceConfig.NotifyService();
            UpdateUI();
        }

        private void PollServiceStatus()
        {
            var processes = Process.GetProcessesByName("PauseMediaService");
            if (processes.Length > 0)
            {
                var p = processes[0];
                _serviceProcessLabel.Text = $"Background Service: Running (PID: {p.Id})";
                _serviceProcessLabel.ForeColor = Color.FromArgb(135, 142, 151);
            }
            else
            {
                _serviceProcessLabel.Text = "Background Service: Stopped (Click Restart Service to launch)";
                _serviceProcessLabel.ForeColor = Color.FromArgb(220, 130, 60);
            }

            // Sync last triggered timestamp from disk
            var diskConfig = ServiceConfig.Load();
            if (diskConfig.LastTriggered != _config.LastTriggered)
            {
                _config.LastTriggered = diskConfig.LastTriggered;
                if (_config.LastTriggered.HasValue)
                {
                    _lastTriggerLabel.Text = $"Last event: Play/Pause sent at {_config.LastTriggered.Value:HH:mm:ss}";
                    _lastTriggerLabel.ForeColor = Color.FromArgb(46, 160, 67);
                }
            }
        }

        private void EnsureServiceRunning()
        {
            var processes = Process.GetProcessesByName("PauseMediaService");
            if (processes.Length == 0)
            {
                StartServiceProcess();
            }
        }

        private void StartServiceProcess()
        {
            try
            {
                string servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PauseMediaService.exe");
                if (File.Exists(servicePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = servicePath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
            }
            catch
            {
                // Ignore launch error
            }
        }

        private void RestartService()
        {
            foreach (var p in Process.GetProcessesByName("PauseMediaService"))
            {
                try { p.Kill(); } catch { }
            }
            System.Threading.Thread.Sleep(300);
            StartServiceProcess();
            PollServiceStatus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            // Note: Background service PauseMediaService remains running independently!
            base.OnFormClosing(e);
        }
    }
}
