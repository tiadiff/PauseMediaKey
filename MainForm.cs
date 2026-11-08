using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PauseMediaKey
{
    public class MainForm : Form
    {
        private readonly KeyboardHook _hook;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;

        private Panel _cardPanel = null!;
        private Label _titleLabel = null!;
        private Label _subtitleLabel = null!;
        private Label _statusBadge = null!;
        private Label _statusDesc = null!;
        private Label _lastTriggerLabel = null!;
        private Button _toggleButton = null!;
        private CheckBox _chkMinimizeToTray = null!;
        private CheckBox _chkStartWithWindows = null!;
        private Label _footerTip = null!;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainForm()
        {
            _hook = new KeyboardHook();
            _notifyIcon = new NotifyIcon();
            _trayMenu = new ContextMenuStrip();

            InitializeComponent();
            ApplyTheme();
            SetupTray();

            _hook.MediaKeyTriggered += OnMediaKeyTriggered;
            _hook.StatusChanged += OnHookStatusChanged;

            // Start hook by default
            _hook.Start();
            UpdateUI();
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
            ClientSize = new Size(460, 360);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
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
                Location = new Point(24, 20),
                AutoSize = true
            };

            _subtitleLabel = new Label
            {
                Text = "Remap 'Pause / Interr' to Windows Play/Pause Media Control",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(145, 152, 161),
                Location = new Point(25, 48),
                AutoSize = true
            };

            // Status Card Panel
            _cardPanel = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(412, 110),
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
                var dotColor = _hook.IsActive ? Color.FromArgb(46, 160, 67) : Color.FromArgb(120, 126, 134);
                using var dotBrush = new SolidBrush(dotColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(dotBrush, 18, 20, 10, 10);
            };

            _statusBadge = new Label
            {
                Location = new Point(36, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point)
            };

            _statusDesc = new Label
            {
                Location = new Point(18, 44),
                Size = new Size(376, 36),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(170, 175, 183)
            };

            _lastTriggerLabel = new Label
            {
                Location = new Point(18, 84),
                Size = new Size(376, 20),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(125, 132, 142),
                Text = "Ready - Waiting for keypress"
            };

            _cardPanel.Controls.Add(_statusBadge);
            _cardPanel.Controls.Add(_statusDesc);
            _cardPanel.Controls.Add(_lastTriggerLabel);

            // Toggle Button
            _toggleButton = new Button
            {
                Location = new Point(24, 204),
                Size = new Size(412, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            _toggleButton.FlatAppearance.BorderSize = 0;
            _toggleButton.Click += (s, e) => _hook.Toggle();

            // Checkbox: Minimize to Tray
            _chkMinimizeToTray = new CheckBox
            {
                Text = "Close / minimize to system tray",
                Location = new Point(26, 260),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(200, 205, 212),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point)
            };

            // Checkbox: Start with Windows
            _chkStartWithWindows = new CheckBox
            {
                Text = "Start automatically with Windows",
                Location = new Point(26, 286),
                AutoSize = true,
                Checked = IsStartupShortcutEnabled(),
                ForeColor = Color.FromArgb(200, 205, 212),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular, GraphicsUnit.Point)
            };
            _chkStartWithWindows.CheckedChanged += (s, e) =>
            {
                SetStartupShortcut(_chkStartWithWindows.Checked);
            };

            // Footer tip
            _footerTip = new Label
            {
                Text = "Works with Opera GX, Spotify, Chrome, YouTube & Windows Media.",
                Location = new Point(24, 324),
                Size = new Size(412, 20),
                ForeColor = Color.FromArgb(115, 122, 131),
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Controls.Add(_titleLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_cardPanel);
            Controls.Add(_toggleButton);
            Controls.Add(_chkMinimizeToTray);
            Controls.Add(_chkStartWithWindows);
            Controls.Add(_footerTip);
        }

        private void ApplyTheme()
        {
            UpdateAppIcon();
        }

        private void SetupTray()
        {
            _trayMenu.Renderer = new DarkMenuRenderer();
            _trayMenu.Items.Add("Open PauseMediaKey", null, (s, e) => RestoreFromTray());
            _trayMenu.Items.Add("Toggle (Active / Inactive)", null, (s, e) => _hook.Toggle());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = _trayMenu;
            _notifyIcon.Text = "PauseMediaKey";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void UpdateAppIcon()
        {
            bool active = _hook != null && _hook.IsActive;
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Rounded background
                using var bgBrush = new SolidBrush(Color.FromArgb(34, 37, 41));
                g.FillRectangle(bgBrush, 0, 0, 32, 32);

                // Play / Pause symbol
                var color = active ? Color.FromArgb(46, 160, 67) : Color.FromArgb(135, 142, 151);
                using var symbolBrush = new SolidBrush(color);

                // Draw two vertical pause bars
                g.FillRectangle(symbolBrush, 7, 7, 6, 18);
                g.FillRectangle(symbolBrush, 19, 7, 6, 18);
            }

            var iconHandle = bmp.GetHicon();
            var icon = Icon.FromHandle(iconHandle);
            Icon = icon;
            _notifyIcon.Icon = icon;
        }

        private void UpdateUI()
        {
            bool active = _hook.IsActive;

            _statusBadge.Text = active ? "ACTIVE" : "INACTIVE";
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

            _cardPanel.Invalidate();
            UpdateAppIcon();

            _notifyIcon.Text = active ? "PauseMediaKey (Active)" : "PauseMediaKey (Inactive)";
        }

        private void OnHookStatusChanged(bool active)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnHookStatusChanged(active)));
                return;
            }
            UpdateUI();
        }

        private void OnMediaKeyTriggered()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnMediaKeyTriggered));
                return;
            }

            _lastTriggerLabel.Text = $"Last triggered: Play/Pause sent at {DateTime.Now:HH:mm:ss}";
            _lastTriggerLabel.ForeColor = Color.FromArgb(46, 160, 67);

            // Reset color after 1.5 seconds
            var timer = new System.Windows.Forms.Timer { Interval = 1500 };
            timer.Tick += (s, e) =>
            {
                _lastTriggerLabel.ForeColor = Color.FromArgb(125, 132, 142);
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && _chkMinimizeToTray.Checked)
            {
                e.Cancel = true;
                Hide();
                _notifyIcon.ShowBalloonTip(1500, "PauseMediaKey", "Minimized to tray. Double-click icon to open.", ToolTipIcon.Info);
                return;
            }

            _hook.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && _chkMinimizeToTray.Checked)
            {
                Hide();
            }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
        }

        private void ExitApplication()
        {
            _hook.Dispose();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private static string GetStartupShortcutPath()
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, "PauseMediaKey.lnk");
        }

        private static bool IsStartupShortcutEnabled()
        {
            return File.Exists(GetStartupShortcutPath());
        }

        private static void SetStartupShortcut(bool enable)
        {
            var shortcutPath = GetStartupShortcutPath();
            try
            {
                if (enable)
                {
                    var shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic? shell = Activator.CreateInstance(shellType);
                        if (shell != null)
                        {
                            var shortcut = shell.CreateShortcut(shortcutPath);
                            shortcut.TargetPath = Application.ExecutablePath;
                            shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            shortcut.Description = "PauseMediaKey Background Utility";
                            shortcut.Save();
                        }
                    }
                }
                else
                {
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                }
            }
            catch
            {
                // Ignore permissions or environment issues
            }
        }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(235, 238, 242);
            base.OnRenderItemText(e);
        }
    }

    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(34, 37, 41);
        public override Color MenuBorder => Color.FromArgb(48, 52, 58);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(48, 52, 58);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(48, 52, 58);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(48, 52, 58);
        public override Color SeparatorDark => Color.FromArgb(48, 52, 58);
        public override Color SeparatorLight => Color.Transparent;
        public override Color ImageMarginGradientBegin => Color.FromArgb(34, 37, 41);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(34, 37, 41);
        public override Color ImageMarginGradientEnd => Color.FromArgb(34, 37, 41);
    }
}
