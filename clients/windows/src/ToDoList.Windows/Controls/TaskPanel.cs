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
        private Point? dragStartPoint;

        public TodoItem Item { get; }
        public event EventHandler? CompletionToggled;
        public event EventHandler? TaskDoubleClicked;
        public event EventHandler? DragInitiated;

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

            bool hasDueDate = item.DueDate.HasValue;
            Height = hasDueDate ? 62 : 50;
            Padding = new Padding(12, 8, 12, 8);
            Margin = new Padding(4, 2, 4, 2);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            // Checkbox — vertically centered
            checkBox = new LargeCheckBox
            {
                Checked = item.IsCompleted,
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            checkBox.CheckedChanged += (s, e) => CompletionToggled?.Invoke(this, EventArgs.Empty);

            // Title
            lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI", 14f, item.IsCompleted ? FontStyle.Strikeout : FontStyle.Regular),
                ForeColor = item.IsCompleted ? AppTheme.CompletedTextColor : AppTheme.TitleColor,
                AutoSize = false,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                BackColor = Color.Transparent
            };

            // Due date — below title on the left
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

            // Category badge — far right, vertically centered
            lblCategory = new Label
            {
                Text = categoryName,
                Font = new Font("Segoe UI", 9f),
                ForeColor = categoryColor ?? AppTheme.SubtextColor,
                AutoSize = true,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                BackColor = Color.Transparent
            };

            Controls.Add(checkBox);
            Controls.Add(lblTitle);
            Controls.Add(lblCategory);
            Controls.Add(lblDueDate);

            // Hover and drag events for all child controls
            foreach (Control child in Controls)
            {
                if (child != checkBox)
                {
                    child.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
                    child.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
                    child.Click += (s, e) => OnClick(EventArgs.Empty);
                    child.DoubleClick += (s, e) => TaskDoubleClicked?.Invoke(this, EventArgs.Empty);
                    child.MouseDown += OnDragMouseDown;
                    child.MouseMove += OnDragMouseMove;
                    child.MouseUp += OnDragMouseUp;
                }
            }

            MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
            MouseDown += OnDragMouseDown;
            MouseMove += OnDragMouseMove;
            MouseUp += OnDragMouseUp;
            DoubleClick += (s, e) => TaskDoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lblTitle == null || lblCategory == null || checkBox == null || lblDueDate == null)
            {
                return;
            }

            bool hasDueDate = !string.IsNullOrEmpty(lblDueDate.Text);

            // Checkbox — vertically centered
            checkBox.Location = new Point(12, (Height - checkBox.Height) / 2);

            // Category on far right, vertically centered
            int catWidth = lblCategory.PreferredWidth;
            lblCategory.Location = new Point(Width - catWidth - 20, (Height - lblCategory.Height) / 2);

            // Title + due date block centered vertically
            int textLeft = 50;
            int blockHeight = hasDueDate ? lblTitle.Height + lblDueDate.Height + 2 : lblTitle.Height;
            int blockTop = (Height - blockHeight) / 2;

            lblTitle.Location = new Point(textLeft, blockTop);
            lblDueDate.Location = new Point(textLeft, blockTop + lblTitle.Height + 2);

            // Title fills space between checkbox and category
            int titleWidth = Width - textLeft - catWidth - 32;
            if (titleWidth < 100)
            {
                titleWidth = 100;
            }

            lblTitle.Width = titleWidth;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(4, 2, Width - 8, Height - 4);
            Color bgColor = isSelected ? AppTheme.CardSelectedColor
                : (isHovered ? AppTheme.CardHoverColor : AppTheme.CardBg);
            Color borderColor = isSelected ? AppTheme.CardSelectedBorderColor : AppTheme.CardBorderColor;

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

        private void OnDragMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = (sender is Control c) ? c.PointToScreen(e.Location) : PointToScreen(e.Location);
            }
        }

        private void OnDragMouseMove(object? sender, MouseEventArgs e)
        {
            if (dragStartPoint == null || e.Button != MouseButtons.Left)
            {
                return;
            }

            Point screenPos = (sender is Control c) ? c.PointToScreen(e.Location) : PointToScreen(e.Location);
            if (Math.Abs(screenPos.X - dragStartPoint.Value.X) > SystemInformation.DragSize.Width ||
                Math.Abs(screenPos.Y - dragStartPoint.Value.Y) > SystemInformation.DragSize.Height)
            {
                dragStartPoint = null;
                DragInitiated?.Invoke(this, EventArgs.Empty);
                DoDragDrop(this, DragDropEffects.Move);
            }
        }

        private void OnDragMouseUp(object? sender, MouseEventArgs e)
        {
            dragStartPoint = null;
        }

        private static Color GetDueDateColor(TodoItem item)
        {
            if (item.IsCompleted || !item.DueDate.HasValue)
            {
                return AppTheme.SubtextColor;
            }

            if (item.DueDate.Value.Date < DateTime.Today)
            {
                return AppTheme.OverdueColor;
            }

            if (item.DueDate.Value.Date == DateTime.Today)
            {
                return AppTheme.DueTodayColor;
            }

            return AppTheme.SubtextColor;
        }
    }
}
