using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ToDoList.Windows.Controls
{
    public class LargeCheckBox : Control
    {
        private static readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private static readonly Color CheckedBg = Color.FromArgb(53, 122, 232);
        private static readonly Color HoverBorder = Color.FromArgb(53, 122, 232);

        private bool isChecked;
        private bool isHovered;

        public event EventHandler? CheckedChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    Invalidate();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public LargeCheckBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(24, 24);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int boxSize = Math.Min(Width, Height) - 4;
            int x = (Width - boxSize) / 2;
            int y = (Height - boxSize) / 2;
            Rectangle boxRect = new Rectangle(x, y, boxSize, boxSize);

            using GraphicsPath path = CreateRoundedRectangle(boxRect, 4);

            if (isChecked)
            {
                // Filled blue background
                using SolidBrush bgBrush = new SolidBrush(CheckedBg);
                g.FillPath(bgBrush, path);

                // White checkmark
                using Pen checkPen = new Pen(Color.White, 2.5f);
                checkPen.StartCap = LineCap.Round;
                checkPen.EndCap = LineCap.Round;
                checkPen.LineJoin = LineJoin.Round;

                float cx = x + boxSize * 0.22f;
                float cy = y + boxSize * 0.5f;
                float mx = x + boxSize * 0.42f;
                float my = y + boxSize * 0.72f;
                float ex = x + boxSize * 0.78f;
                float ey = y + boxSize * 0.28f;

                g.DrawLine(checkPen, cx, cy, mx, my);
                g.DrawLine(checkPen, mx, my, ex, ey);
            }
            else
            {
                // Empty box with border
                Color border = isHovered ? HoverBorder : BorderColor;
                using Pen borderPen = new Pen(border, 2f);
                g.DrawPath(borderPen, path);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
