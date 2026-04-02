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
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label hintLabel;
        private readonly Button enableButton;
        private readonly Button disableButton;
        private readonly Button exitButton;
        private readonly Panel statusDot;

        private bool dragging;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public FloatingForm(AppState appState)
        {
            this.appState = appState;
            this.appState.StateChanged += AppStateOnStateChanged;

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
            ClientSize = new Size(288, 92);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Text = "SysHiberSwitch";

            Location = LoadWindowLocation();

            titleLabel = BuildLabel("\u7535\u6e90\u4fdd\u6301\u5524\u9192", new Point(16, 11), 9.6F, true, new Size(132, 18));

            statusDot = new Panel();
            statusDot.Size = new Size(10, 10);
            statusDot.Location = new Point(18, 39);

            statusLabel = BuildLabel(string.Empty, new Point(34, 33), 8.7F, true, new Size(110, 18));
            hintLabel = BuildLabel("\u5f00\u542f\u540e\u540c\u65f6\u963b\u6b62\u606f\u5c4f\u548c\u4f11\u7720", new Point(16, 58), 7.8F, false, new Size(180, 16));
            hintLabel.ForeColor = Color.FromArgb(102, 114, 128);

            enableButton = BuildButton("\u5f00\u542f", new Point(184, 18), 42, Color.FromArgb(46, 160, 67));
            enableButton.Click += EnableButtonOnClick;

            disableButton = BuildButton("\u5173\u95ed", new Point(230, 18), 42, Color.FromArgb(212, 84, 84));
            disableButton.Click += DisableButtonOnClick;

            exitButton = BuildButton("\u9000\u51fa", new Point(230, 52), 42, Color.FromArgb(90, 90, 98));
            exitButton.Click += ExitButtonOnClick;

            Controls.Add(titleLabel);
            Controls.Add(statusDot);
            Controls.Add(statusLabel);
            Controls.Add(hintLabel);
            Controls.Add(enableButton);
            Controls.Add(disableButton);
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

            UpdateUi();
            UpdateRoundedRegion();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            appState.StateChanged -= AppStateOnStateChanged;
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowLocation();
            appState.SetEnabled(false);
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

        private static Label BuildLabel(string text, Point location, float fontSize, bool bold, Size size)
        {
            var label = new Label();
            label.Text = text;
            label.Location = location;
            label.AutoSize = false;
            label.Size = size;
            label.ForeColor = Color.FromArgb(48, 58, 72);
            label.Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
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

        private void AppStateOnStateChanged(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void EnableButtonOnClick(object sender, EventArgs e)
        {
            appState.SetEnabled(true);
        }

        private void DisableButtonOnClick(object sender, EventArgs e)
        {
            appState.SetEnabled(false);
        }

        private void ExitButtonOnClick(object sender, EventArgs e)
        {
            appState.SetEnabled(false);
            Close();
        }

        private void UpdateUi()
        {
            if (appState.Enabled)
            {
                statusLabel.Text = "\u5df2\u542f\u7528";
                statusLabel.ForeColor = Color.FromArgb(46, 160, 67);
                statusDot.BackColor = Color.FromArgb(46, 160, 67);
                enableButton.Enabled = false;
                disableButton.Enabled = true;
                hintLabel.Text = "\u5df2\u963b\u6b62\u606f\u5c4f\u548c\u4f11\u7720";
            }
            else
            {
                statusLabel.Text = "\u5df2\u5173\u95ed";
                statusLabel.ForeColor = Color.FromArgb(212, 84, 84);
                statusDot.BackColor = Color.FromArgb(212, 84, 84);
                enableButton.Enabled = true;
                disableButton.Enabled = false;
                hintLabel.Text = "\u5f53\u524d\u7531\u7cfb\u7edf\u7535\u6e90\u8ba1\u5212\u63a5\u7ba1";
            }

            Invalidate();
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
