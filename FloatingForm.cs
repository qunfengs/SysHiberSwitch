using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SysHiberSwitch
{
    internal sealed class FloatingForm : Form
    {
        private readonly AppState appState;
        private readonly Label titleLabel;
        private readonly Label statusLabel;
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
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            MaximizeBox = false;
            MinimizeBox = false;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(232, 236, 242);
            Opacity = 0.72D;
            ClientSize = new Size(244, 72);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Text = "SysHiberSwitch";

            titleLabel = new Label();
            titleLabel.Text = "\u9632\u4f11\u7720";
            titleLabel.ForeColor = Color.FromArgb(48, 58, 72);
            titleLabel.Font = new Font(Font.FontFamily, 9.4F, FontStyle.Bold);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(16, 11);

            statusDot = new Panel();
            statusDot.Size = new Size(10, 10);
            statusDot.Location = new Point(18, 39);

            statusLabel = new Label();
            statusLabel.ForeColor = Color.FromArgb(72, 84, 98);
            statusLabel.Font = new Font(Font.FontFamily, 8.4F, FontStyle.Bold);
            statusLabel.AutoSize = false;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Location = new Point(34, 33);
            statusLabel.Size = new Size(70, 18);

            enableButton = BuildButton("\u5f00", new Point(118, 20), Color.FromArgb(46, 160, 67));
            enableButton.Click += EnableButtonOnClick;

            disableButton = BuildButton("\u5173", new Point(160, 20), Color.FromArgb(180, 70, 70));
            disableButton.Click += DisableButtonOnClick;

            exitButton = BuildButton("\u9000", new Point(202, 20), Color.FromArgb(90, 90, 98));
            exitButton.Click += ExitButtonOnClick;

            Controls.Add(titleLabel);
            Controls.Add(statusDot);
            Controls.Add(statusLabel);
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

        private static Button BuildButton(string text, Point location, Color backColor)
        {
            var button = new Button();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(32, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            return button;
        }

        private void AppStateOnStateChanged(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void EnableButtonOnClick(object sender, EventArgs e)
        {
            appState.Enable();
        }

        private void DisableButtonOnClick(object sender, EventArgs e)
        {
            appState.Disable();
        }

        private void ExitButtonOnClick(object sender, EventArgs e)
        {
            Close();
        }

        private void UpdateUi()
        {
            if (appState.Enabled)
            {
                statusLabel.Text = "\u5df2\u5f00\u542f";
                statusLabel.ForeColor = Color.FromArgb(82, 196, 102);
                statusDot.BackColor = Color.FromArgb(82, 196, 102);
                enableButton.Enabled = false;
                disableButton.Enabled = true;
            }
            else
            {
                statusLabel.Text = "\u5df2\u5173\u95ed";
                statusLabel.ForeColor = Color.FromArgb(255, 120, 117);
                statusDot.BackColor = Color.FromArgb(255, 120, 117);
                enableButton.Enabled = true;
                disableButton.Enabled = false;
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
            Opacity = 0.93D;
        }

        private void FadeOutIfNeeded(object sender, EventArgs e)
        {
            if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                return;
            }

            Opacity = 0.72D;
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
    }
}
