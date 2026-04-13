using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Forms
{
    public class TaskEditForm : Form
    {
        private KryptonTextBox txtTitle = null!;
        private KryptonRichTextBox txtNotes = null!;
        private KryptonComboBox cboCategory = null!;
        private KryptonCheckBox chkHasDueDate = null!;
        private KryptonDateTimePicker dtpDueDate = null!;
        private Controls.AccentButton btnSave = null!;
        private Controls.FlatButton btnCancel = null!;

        private readonly List<Category> categories;

        public string TaskTitle => txtTitle.Text.Trim();
        public string? TaskNotes => string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
        public long? SelectedCategoryId
        {
            get
            {
                if (cboCategory.SelectedIndex <= 0)
                {
                    return null;
                }
                if (cboCategory.Items[cboCategory.SelectedIndex] is Category cat)
                {
                    return cat.CategoryId;
                }

                return null;
            }
        }
        public DateTime? TaskDueDate => chkHasDueDate.Checked ? dtpDueDate.Value.Date : null;

        public TaskEditForm(List<Category> categories, TodoItem? existingItem)
        {
            this.categories = categories;
            InitializeComponents();
            PopulateCategories();
            AppTheme.StyleForm(this);

            if (existingItem != null)
            {
                Text = "Edit Task";
                txtTitle.Text = existingItem.Title;
                txtNotes.Text = existingItem.Notes ?? "";

                if (existingItem.CategoryId.HasValue)
                {
                    for (int i = 1; i < cboCategory.Items.Count; i++)
                    {
                        if (cboCategory.Items[i] is Category cat && cat.CategoryId == existingItem.CategoryId.Value)
                        {
                            cboCategory.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (existingItem.DueDate.HasValue)
                {
                    chkHasDueDate.Checked = true;
                    dtpDueDate.Value = existingItem.DueDate.Value;
                    dtpDueDate.Enabled = true;
                }
            }
        }

        private void InitializeComponents()
        {
            Text = "New Task";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.ContentBg;
            Padding = new Padding(24, 20, 24, 20);

            int y = 24;
            int controlLeft = 120;
            int controlWidth = 330;

            // Title
            KryptonLabel lblTitle = new KryptonLabel { Text = "Title", Location = new Point(24, y + 4), AutoSize = true };
            lblTitle.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            txtTitle = new KryptonTextBox { Location = new Point(controlLeft, y), Width = controlWidth };
            txtTitle.StateCommon.Border.Rounding = 4;
            y += 40;

            // Notes
            KryptonLabel lblNotes = new KryptonLabel { Text = "Notes", Location = new Point(24, y + 4), AutoSize = true };
            lblNotes.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            txtNotes = new KryptonRichTextBox { Location = new Point(controlLeft, y), Width = controlWidth, Height = 130 };
            y += 146;

            // Category
            KryptonLabel lblCategory = new KryptonLabel { Text = "Category", Location = new Point(24, y + 4), AutoSize = true };
            lblCategory.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            cboCategory = new KryptonComboBox
            {
                Location = new Point(controlLeft, y),
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            y += 40;

            // Due Date
            chkHasDueDate = new KryptonCheckBox { Text = "Due Date", Location = new Point(24, y + 4) };
            chkHasDueDate.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            dtpDueDate = new KryptonDateTimePicker
            {
                Location = new Point(controlLeft, y),
                Width = controlWidth,
                Enabled = false,
                Format = DateTimePickerFormat.Short
            };
            chkHasDueDate.CheckedChanged += (s, e) => dtpDueDate.Enabled = chkHasDueDate.Checked;
            y += 56;

            // Buttons
            btnSave = new Controls.AccentButton
            {
                Text = "Save",
                Width = 100,
                Location = new Point(265, y)
            };

            btnCancel = new Controls.FlatButton
            {
                Text = "Cancel",
                Width = 80,
                Location = new Point(375, y)
            };

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show(this, "Title is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                DialogResult = DialogResult.OK;
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
            };

            Controls.AddRange(new Control[]
            {
                lblTitle, txtTitle,
                lblNotes, txtNotes,
                lblCategory, cboCategory,
                chkHasDueDate, dtpDueDate,
                btnSave, btnCancel
            });
        }

        private void PopulateCategories()
        {
            cboCategory.Items.Clear();
            cboCategory.Items.Add("(None)");
            foreach (Category cat in categories)
            {
                cboCategory.Items.Add(cat);
            }
            cboCategory.SelectedIndex = 0;
        }
    }
}
