using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Forms
{
    public class TaskEditForm : KryptonForm
    {
        private KryptonTextBox txtTitle = null!;
        private KryptonRichTextBox txtNotes = null!;
        private KryptonComboBox cboCategory = null!;
        private KryptonCheckBox chkHasDueDate = null!;
        private KryptonDateTimePicker dtpDueDate = null!;
        private KryptonButton btnSave = null!;
        private KryptonButton btnCancel = null!;

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
            Size = new Size(480, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 16;
            int controlLeft = 100;
            int controlWidth = 350;

            // Title
            var lblTitle = new KryptonLabel { Text = "Title:", Location = new Point(12, y + 2), AutoSize = true };
            txtTitle = new KryptonTextBox { Location = new Point(controlLeft, y), Width = controlWidth };
            y += 34;

            // Notes
            var lblNotes = new KryptonLabel { Text = "Notes:", Location = new Point(12, y + 2), AutoSize = true };
            txtNotes = new KryptonRichTextBox { Location = new Point(controlLeft, y), Width = controlWidth, Height = 120 };
            y += 130;

            // Category
            var lblCategory = new KryptonLabel { Text = "Category:", Location = new Point(12, y + 2), AutoSize = true };
            cboCategory = new KryptonComboBox
            {
                Location = new Point(controlLeft, y),
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            y += 34;

            // Due Date
            chkHasDueDate = new KryptonCheckBox { Text = "Due Date:", Location = new Point(12, y + 2) };
            dtpDueDate = new KryptonDateTimePicker
            {
                Location = new Point(controlLeft, y),
                Width = controlWidth,
                Enabled = false,
                Format = DateTimePickerFormat.Short
            };
            chkHasDueDate.CheckedChanged += (s, e) => dtpDueDate.Enabled = chkHasDueDate.Checked;
            y += 50;

            // Buttons
            btnSave = new KryptonButton { Text = "Save", Location = new Point(265, y), Width = 90, DialogResult = DialogResult.OK };
            btnCancel = new KryptonButton { Text = "Cancel", Location = new Point(362, y), Width = 90, DialogResult = DialogResult.Cancel };

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    KryptonMessageBox.Show(this, "Title is required.", "Validation",
                        KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            AcceptButton = btnSave;
            CancelButton = btnCancel;

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
            foreach (var cat in categories)
            {
                cboCategory.Items.Add(cat);
            }
            cboCategory.SelectedIndex = 0;
        }
    }
}
