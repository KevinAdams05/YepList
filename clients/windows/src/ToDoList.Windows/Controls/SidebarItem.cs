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
        private bool isHovered;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsItemSelected { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsDefault { get; set; }

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
            Color bgColor = IsItemSelected ? AppTheme.SidebarSelectedBg
                : (isHovered ? AppTheme.SidebarHoverBg : AppTheme.SidebarBg);

            using GraphicsPath path = CreateRoundedRectangle(rect, 6);
            using SolidBrush bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, path);

            // Draw text
            Font textFont = IsItemSelected ? new Font(Font, FontStyle.Bold) : Font;
            Color textColor = IsItemSelected ? AppTheme.SidebarSelectedText : AppTheme.TitleColor;

            float starWidth = IsDefault ? 20f : 0f;
            using SolidBrush textBrush = new SolidBrush(textColor);
            RectangleF textRect = new RectangleF(16, 0, Width - 24 - starWidth, Height);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(Text, textFont, textBrush, textRect, sf);

            if (IsDefault)
            {
                using Font starFont = new Font("Segoe UI", 11f);
                using SolidBrush starBrush = new SolidBrush(AppTheme.SubtextColor);
                RectangleF starRect = new RectangleF(Width - 32, 0, 24, Height);
                StringFormat starSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("\u2605", starFont, starBrush, starRect, starSf);
            }

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
