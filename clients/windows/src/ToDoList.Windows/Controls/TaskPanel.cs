using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Controls
{
    public class TaskPanel : Panel
    {
        private readonly LargeCheckBox checkBox;
        private readonly Label lblTitle;
        private readonly Label lblCategory;
        private readonly Label lblDueDate;

        private bool isHovered;
        private bool isSelected;

        public TodoItem Item { get; }
        public event EventHandler? CompletionToggled;
        public event EventHandler? TaskDoubleClicked;

        // Colors
        private static readonly Color HoverColor = Color.FromArgb(245, 245, 245);
        private static readonly Color SelectedColor = Color.FromArgb(232, 240, 254);
        private static readonly Color SelectedBorderColor = Color.FromArgb(66, 133, 244);
        private static readonly Color CardBorderColor = Color.FromArgb(228, 228, 228);
        private static readonly Color CompletedTextColor = Color.FromArgb(158, 158, 158);
        private static readonly Color TitleColor = Color.FromArgb(32, 32, 32);
        private static readonly Color SubtextColor = Color.FromArgb(120, 120, 120);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                Invalidate();
            }
        }

        public TaskPanel(TodoItem item, string categoryName, Color? categoryColor)
        {
            Item = item;

            Height = 56;
            Padding = new Padding(12, 8, 12, 8);
            Margin = new Padding(4, 2, 4, 2);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            // Checkbox
            checkBox = new LargeCheckBox
            {
                Checked = item.IsCompleted,
                Location = new Point(12, 12),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            checkBox.CheckedChanged += (s, e) => CompletionToggled?.Invoke(this, EventArgs.Empty);

            // Title
            lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI", 10f, item.IsCompleted ? FontStyle.Strikeout : FontStyle.Regular),
                ForeColor = item.IsCompleted ? CompletedTextColor : TitleColor,
                Location = new Point(48, 8),
                AutoSize = false,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                BackColor = Color.Transparent
            };

            // Category badge
            lblCategory = new Label
            {
                Text = categoryName,
                Font = new Font("Segoe UI", 8f),
                ForeColor = categoryColor ?? SubtextColor,
                Location = new Point(48, 30),
                AutoSize = true,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                BackColor = Color.Transparent
            };

            // Due date
            lblDueDate = new Label
            {
                Text = item.DueDate?.ToString("MMM d, yyyy") ?? "",
                Font = new Font("Segoe UI", 8f),
                ForeColor = GetDueDateColor(item),
                AutoSize = true,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                BackColor = Color.Transparent
            };

            Controls.Add(checkBox);
            Controls.Add(lblTitle);
            Controls.Add(lblCategory);
            Controls.Add(lblDueDate);

            // Hover events for all child controls
            foreach (Control child in Controls)
            {
                if (child != checkBox)
                {
                    child.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
                    child.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
                    child.Click += (s, e) => OnClick(EventArgs.Empty);
                    child.DoubleClick += (s, e) => TaskDoubleClicked?.Invoke(this, EventArgs.Empty);
                }
            }

            MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
            DoubleClick += (s, e) => TaskDoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lblTitle == null || lblDueDate == null)
            {
                return;
            }

            int titleWidth = Width - 200;
            if (titleWidth < 100)
            {
                titleWidth = 100;
            }

            lblTitle.Width = titleWidth;
            lblDueDate.Location = new Point(Width - lblDueDate.Width - 16, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(4, 2, Width - 8, Height - 4);
            Color bgColor = isSelected ? SelectedColor : (isHovered ? HoverColor : Color.White);
            Color borderColor = isSelected ? SelectedBorderColor : CardBorderColor;

            using GraphicsPath path = CreateRoundedRectangle(rect, 8);
            using SolidBrush bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, path);

            using Pen borderPen = new Pen(borderColor, isSelected ? 2f : 1f);
            g.DrawPath(borderPen, path);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private static Color GetDueDateColor(TodoItem item)
        {
            if (item.IsCompleted || !item.DueDate.HasValue)
            {
                return SubtextColor;
            }

            if (item.DueDate.Value.Date < DateTime.Today)
            {
                return Color.FromArgb(211, 47, 47); // Red for overdue
            }

            if (item.DueDate.Value.Date == DateTime.Today)
            {
                return Color.FromArgb(245, 124, 0); // Orange for today
            }

            return SubtextColor;
        }
    }
}
