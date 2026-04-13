using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ToDoList.Windows.Controls
{
    public class SidebarItem : Control
    {
        private static readonly Color NormalBg = Color.FromArgb(247, 247, 247);
        private static readonly Color HoverBg = Color.FromArgb(237, 237, 237);
        private static readonly Color SelectedBg = Color.FromArgb(232, 240, 254);
        private static readonly Color NormalFg = Color.FromArgb(60, 60, 60);
        private static readonly Color SelectedFg = Color.FromArgb(53, 122, 232);

        private bool isHovered;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsItemSelected { get; set; }

        public SidebarItem()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Font = new Font("Segoe UI", 10f);
            Cursor = Cursors.Hand;
            Height = 40;
            BackColor = Color.Transparent;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(2, 2, Width - 4, Height - 4);
            Color bgColor = IsItemSelected ? SelectedBg : (isHovered ? HoverBg : NormalBg);

            using GraphicsPath path = CreateRoundedRectangle(rect, 6);
            using SolidBrush bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, path);

            // Draw text
            Font textFont = IsItemSelected ? new Font(Font, FontStyle.Bold) : Font;
            Color textColor = IsItemSelected ? SelectedFg : NormalFg;

            using SolidBrush textBrush = new SolidBrush(textColor);
            RectangleF textRect = new RectangleF(16, 0, Width - 24, Height);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(Text, textFont, textBrush, textRect, sf);

            if (IsItemSelected)
            {
                textFont.Dispose();
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
