using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SysHiberSwitch
{
    internal sealed class FloatingForm : Form
    {
        private static readonly string StateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SysHiberSwitch");

        private static readonly string PositionFilePath = Path.Combine(StateDirectory, "window-position.txt");

        private readonly AppState appState;
        private readonly ApplicationIdleMonitor photoshopMonitor;
        private readonly ApplicationIdleMonitor cinema4DMonitor;
        private readonly AutoStartManager autoStartManager;

        private readonly Label titleLabel;
        private readonly Label awakeTitleLabel;
        private readonly Label awakeValueLabel;
        private readonly Label photoshopTitleLabel;
        private readonly Label photoshopValueLabel;
        private readonly Label cinema4DTitleLabel;
        private readonly Label cinema4DValueLabel;
        private readonly Label detailLabel;
        private readonly CheckBox autoStartCheckBox;
        private readonly Button exitButton;
        private readonly Panel awakeDot;
        private readonly Panel photoshopDot;
        private readonly Panel cinema4DDot;

        private bool syncingAutoStartCheckBox;
        private bool dragging;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public FloatingForm(
            AppState appState,
            ApplicationIdleMonitor photoshopMonitor,
            ApplicationIdleMonitor cinema4DMonitor,
            AutoStartManager autoStartManager)
        {
            this.appState = appState;
            this.photoshopMonitor = photoshopMonitor;
            this.cinema4DMonitor = cinema4DMonitor;
            this.autoStartManager = autoStartManager;

            this.appState.StateChanged += OnStateChanged;
            this.photoshopMonitor.StateChanged += OnStateChanged;
            this.cinema4DMonitor.StateChanged += OnStateChanged;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = true;
            MaximizeBox = false;
            MinimizeBox = false;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(232, 236, 242);
            Opacity = 0.76D;
            ClientSize = new Size(392, 182);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Text = "SysHiberSwitch";

            Location = LoadWindowLocation();

            titleLabel = BuildLabel("\u9632\u4f11\u7720\u76d1\u6d4b\u5668", new Point(16, 11), 9.6F, true, new Size(160, 18), Color.FromArgb(48, 58, 72));

            awakeDot = BuildDot(new Point(18, 42), Color.FromArgb(48, 58, 72));
            awakeTitleLabel = BuildLabel("\u9632\u4f11\u7720\uff1a", new Point(34, 35), 8.7F, true, new Size(66, 18), Color.FromArgb(48, 58, 72));
            awakeValueLabel = BuildLabel(string.Empty, new Point(100, 35), 8.7F, true, new Size(88, 18), Color.FromArgb(48, 58, 72));

            photoshopDot = BuildDot(new Point(18, 67), Color.FromArgb(48, 58, 72));
            photoshopTitleLabel = BuildLabel("Photoshop\uff1a", new Point(34, 60), 8.2F, true, new Size(78, 18), Color.FromArgb(48, 58, 72));
            photoshopValueLabel = BuildLabel(string.Empty, new Point(112, 60), 8.2F, true, new Size(116, 18), Color.FromArgb(48, 58, 72));

            cinema4DDot = BuildDot(new Point(18, 92), Color.FromArgb(48, 58, 72));
            cinema4DTitleLabel = BuildLabel("Cinema 4D\uff1a", new Point(34, 85), 8.2F, true, new Size(86, 18), Color.FromArgb(48, 58, 72));
            cinema4DValueLabel = BuildLabel(string.Empty, new Point(120, 85), 8.2F, true, new Size(116, 18), Color.FromArgb(48, 58, 72));

            detailLabel = BuildLabel(string.Empty, new Point(16, 115), 8F, false, new Size(302, 18), Color.FromArgb(48, 58, 72));

            autoStartCheckBox = new CheckBox();
            autoStartCheckBox.Text = "\u5f00\u673a\u542f\u52a8";
            autoStartCheckBox.Location = new Point(16, 142);
            autoStartCheckBox.Size = new Size(96, 20);
            autoStartCheckBox.FlatStyle = FlatStyle.Flat;
            autoStartCheckBox.ForeColor = Color.FromArgb(48, 58, 72);
            autoStartCheckBox.BackColor = Color.Transparent;
            autoStartCheckBox.CheckedChanged += AutoStartCheckBoxOnCheckedChanged;

            exitButton = BuildButton("\u9000\u51fa", new Point(324, 138), 52, Color.FromArgb(90, 90, 98));
            exitButton.Click += ExitButtonOnClick;

            Controls.Add(titleLabel);
            Controls.Add(awakeDot);
            Controls.Add(awakeTitleLabel);
            Controls.Add(awakeValueLabel);
            Controls.Add(photoshopDot);
            Controls.Add(photoshopTitleLabel);
            Controls.Add(photoshopValueLabel);
            Controls.Add(cinema4DDot);
            Controls.Add(cinema4DTitleLabel);
            Controls.Add(cinema4DValueLabel);
            Controls.Add(detailLabel);
            Controls.Add(autoStartCheckBox);
            Controls.Add(exitButton);

            MouseDown += StartDrag;
            MouseMove += DragWindow;
            MouseUp += StopDrag;
            MouseEnter += FadeIn;
            MouseLeave += FadeOutIfNeeded;
            Resize += OnResizeRefreshRegion;

            foreach (Control control in Controls)
            {
                control.MouseDown += StartDrag;
                control.MouseMove += DragWindow;
                control.MouseUp += StopDrag;
                control.MouseEnter += FadeIn;
                control.MouseLeave += FadeOutIfNeeded;
            }

            syncingAutoStartCheckBox = true;
            autoStartCheckBox.Checked = autoStartManager.GetEnabled();
            syncingAutoStartCheckBox = false;

            UpdateUi();
            UpdateRoundedRegion();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            appState.StateChanged -= OnStateChanged;
            photoshopMonitor.StateChanged -= OnStateChanged;
            cinema4DMonitor.StateChanged -= OnStateChanged;
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowLocation();
            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var borderPen = new Pen(Color.FromArgb(140, 188, 196, 206)))
            using (var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 16))
            {
                e.Graphics.DrawPath(borderPen, path);
            }
        }

        private static Label BuildLabel(string text, Point location, float fontSize, bool bold, Size size, Color color)
        {
            var label = new Label();
            label.Text = text;
            label.Location = location;
            label.AutoSize = false;
            label.Size = size;
            label.ForeColor = color;
            label.Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static Panel BuildDot(Point location, Color color)
        {
            var dot = new Panel();
            dot.Size = new Size(10, 10);
            dot.Location = location;
            dot.BackColor = color;
            return dot;
        }

        private static Button BuildButton(string text, Point location, int width, Color backColor)
        {
            var button = new Button();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(width, 26);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 8.1F, FontStyle.Bold, GraphicsUnit.Point, 0);
            return button;
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void AutoStartCheckBoxOnCheckedChanged(object sender, EventArgs e)
        {
            if (syncingAutoStartCheckBox)
            {
                return;
            }

            autoStartManager.SetEnabled(autoStartCheckBox.Checked, Application.ExecutablePath);
        }

        private void ExitButtonOnClick(object sender, EventArgs e)
        {
            Close();
        }

        private void UpdateUi()
        {
            awakeDot.BackColor = Color.FromArgb(48, 58, 72);
            photoshopDot.BackColor = Color.FromArgb(48, 58, 72);
            cinema4DDot.BackColor = Color.FromArgb(48, 58, 72);

            if (appState.KeepAwakeEnabled)
            {
                awakeValueLabel.Text = "\u5df2\u542f\u7528";
                awakeValueLabel.ForeColor = Color.FromArgb(46, 160, 67);
            }
            else
            {
                awakeValueLabel.Text = "\u5df2\u5173\u95ed";
                awakeValueLabel.ForeColor = Color.FromArgb(212, 84, 84);
            }

            ApplyApplicationStatus(photoshopMonitor, photoshopValueLabel);
            ApplyApplicationStatus(cinema4DMonitor, cinema4DValueLabel);
            detailLabel.Text = BuildDetailText();

            Invalidate();
        }

        private void ApplyApplicationStatus(ApplicationIdleMonitor monitor, Label valueLabel)
        {
            switch (monitor.State)
            {
                case ApplicationDetectionState.Active:
                    valueLabel.Text = "\u6d3b\u52a8\u4e2d";
                    valueLabel.ForeColor = Color.FromArgb(46, 160, 67);
                    break;
                case ApplicationDetectionState.IdleCountdown:
                    valueLabel.Text = "\u5012\u8ba1\u65f6";
                    valueLabel.ForeColor = Color.FromArgb(210, 138, 50);
                    break;
                case ApplicationDetectionState.IdleExpired:
                    valueLabel.Text = "\u7a7a\u95f2\u8d85\u65f6";
                    valueLabel.ForeColor = Color.FromArgb(212, 84, 84);
                    break;
                default:
                    valueLabel.Text = "\u672a\u542f\u52a8";
                    valueLabel.ForeColor = Color.FromArgb(212, 84, 84);
                    break;
            }
        }

        private string BuildDetailText()
        {
            if (photoshopMonitor.State == ApplicationDetectionState.Active || cinema4DMonitor.State == ApplicationDetectionState.Active)
            {
                return "\u6709\u8f6f\u4ef6\u6b63\u5728\u5de5\u4f5c\uff0c\u4fdd\u6301\u9632\u4f11\u7720";
            }

            if (photoshopMonitor.State == ApplicationDetectionState.IdleCountdown || cinema4DMonitor.State == ApplicationDetectionState.IdleCountdown)
            {
                var countdown = 0;

                if (photoshopMonitor.State == ApplicationDetectionState.IdleCountdown)
                {
                    countdown = Math.Max(countdown, photoshopMonitor.IdleCountdownSecondsRemaining);
                }

                if (cinema4DMonitor.State == ApplicationDetectionState.IdleCountdown)
                {
                    countdown = Math.Max(countdown, cinema4DMonitor.IdleCountdownSecondsRemaining);
                }

                return "\u7a7a\u95f2\u5012\u8ba1\u65f6\uff1a" + countdown + "\u79d2";
            }

            return "\u65e0\u4fdd\u62a4\u9700\u6c42\uff0c\u5df2\u5173\u95ed\u9632\u4f11\u7720";
        }

        private void StartDrag(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = Location;
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                return;
            }

            var cursorDelta = new Size(Cursor.Position.X - dragCursorPoint.X, Cursor.Position.Y - dragCursorPoint.Y);
            Location = Point.Add(dragFormPoint, cursorDelta);
        }

        private void StopDrag(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void FadeIn(object sender, EventArgs e)
        {
            Opacity = 0.94D;
        }

        private void FadeOutIfNeeded(object sender, EventArgs e)
        {
            if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                return;
            }

            Opacity = 0.76D;
        }

        private void OnResizeRefreshRegion(object sender, EventArgs e)
        {
            UpdateRoundedRegion();
        }

        private void UpdateRoundedRegion()
        {
            using (var path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), 16))
            {
                Region = new Region(path);
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private Point LoadWindowLocation()
        {
            var defaultBounds = Screen.PrimaryScreen.WorkingArea;
            var defaultLocation = new Point(
                defaultBounds.Left + (defaultBounds.Width - Width) / 2,
                defaultBounds.Top + (defaultBounds.Height - Height) / 2);

            try
            {
                if (!File.Exists(PositionFilePath))
                {
                    return defaultLocation;
                }

                var content = File.ReadAllText(PositionFilePath).Trim();
                var parts = content.Split(',');
                if (parts.Length != 2)
                {
                    return defaultLocation;
                }

                int x;
                int y;
                if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y))
                {
                    return defaultLocation;
                }

                return EnsureVisibleLocation(new Point(x, y), defaultLocation);
            }
            catch
            {
                return defaultLocation;
            }
        }

        private void SaveWindowLocation()
        {
            try
            {
                Directory.CreateDirectory(StateDirectory);
                File.WriteAllText(PositionFilePath, Location.X + "," + Location.Y);
            }
            catch
            {
            }
        }

        private Point EnsureVisibleLocation(Point location, Point fallback)
        {
            var windowBounds = new Rectangle(location, Size);

            foreach (var screen in Screen.AllScreens)
            {
                var visibleArea = screen.WorkingArea;
                if (visibleArea.IntersectsWith(windowBounds))
                {
                    var x = Math.Min(Math.Max(location.X, visibleArea.Left), visibleArea.Right - Width);
                    var y = Math.Min(Math.Max(location.Y, visibleArea.Top), visibleArea.Bottom - Height);
                    return new Point(x, y);
                }
            }

            return fallback;
        }
    }
}
