using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.ApiClient;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Forms
{
    public class CategoryManagerForm : KryptonForm
    {
        private readonly TodoApiClient apiClient;

        private KryptonDataGridView gridCategories = null!;
        private KryptonTextBox txtName = null!;
        private KryptonTextBox txtColor = null!;
        private KryptonButton btnPickColor = null!;
        private KryptonButton btnAdd = null!;
        private KryptonButton btnUpdate = null!;
        private KryptonButton btnDelete = null!;
        private KryptonButton btnClose = null!;
        private KryptonPanel colorPreview = null!;

        private List<Category> categories;

        public CategoryManagerForm(TodoApiClient apiClient, List<Category> categories)
        {
            this.apiClient = apiClient;
            this.categories = categories.ToList();
            InitializeComponents();
            RefreshGrid();
        }

        private void InitializeComponents()
        {
            Text = "Manage Categories";
            Size = new Size(520, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Grid
            gridCategories = new KryptonDataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(370, 220),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 60 };
            var colColor = new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "Color", FillWeight = 40 };
            gridCategories.Columns.AddRange(new DataGridViewColumn[] { colName, colColor });

            gridCategories.SelectionChanged += (s, e) =>
            {
                var cat = GetSelectedCategory();
                if (cat != null)
                {
                    txtName.Text = cat.Name;
                    txtColor.Text = cat.Color ?? "";
                    UpdateColorPreview();
                }
            };

            // Input fields
            var lblName = new KryptonLabel { Text = "Name:", Location = new Point(12, 248), AutoSize = true };
            txtName = new KryptonTextBox { Location = new Point(80, 246), Width = 302 };

            var lblColor = new KryptonLabel { Text = "Color:", Location = new Point(12, 282), AutoSize = true };
            txtColor = new KryptonTextBox { Location = new Point(80, 280), Width = 180 };
            txtColor.TextChanged += (s, e) => UpdateColorPreview();

            colorPreview = new KryptonPanel
            {
                Location = new Point(268, 280),
                Size = new Size(28, 24)
            };
            colorPreview.StateCommon.Color1 = Color.Gray;

            btnPickColor = new KryptonButton { Text = "...", Location = new Point(302, 280), Width = 32 };
            btnPickColor.Click += (s, e) =>
            {
                using var dlg = new ColorDialog();
                if (!string.IsNullOrEmpty(txtColor.Text))
                {
                    try
                    {
                        dlg.Color = ColorTranslator.FromHtml(txtColor.Text);
                    }
                    catch
                    {
                        // Ignore invalid color
                    }
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtColor.Text = ColorTranslator.ToHtml(dlg.Color);
                }
            };

            // Buttons
            btnAdd = new KryptonButton { Text = "Add", Location = new Point(400, 12), Width = 100 };
            btnUpdate = new KryptonButton { Text = "Update", Location = new Point(400, 48), Width = 100 };
            btnDelete = new KryptonButton { Text = "Delete", Location = new Point(400, 84), Width = 100 };
            btnClose = new KryptonButton { Text = "Close", Location = new Point(400, 340), Width = 100, DialogResult = DialogResult.OK };

            btnAdd.Click += async (s, e) => await AddCategoryAsync();
            btnUpdate.Click += async (s, e) => await UpdateCategoryAsync();
            btnDelete.Click += async (s, e) => await DeleteCategoryAsync();

            CancelButton = btnClose;

            Controls.AddRange(new Control[]
            {
                gridCategories, lblName, txtName, lblColor, txtColor,
                colorPreview, btnPickColor,
                btnAdd, btnUpdate, btnDelete, btnClose
            });
        }

        private void RefreshGrid()
        {
            gridCategories.Rows.Clear();
            foreach (var cat in categories.OrderBy(c => c.Name))
            {
                var rowIndex = gridCategories.Rows.Add(cat.Name, cat.Color ?? "");
                gridCategories.Rows[rowIndex].Tag = cat;

                if (cat.Color != null)
                {
                    try
                    {
                        var color = ColorTranslator.FromHtml(cat.Color);
                        gridCategories.Rows[rowIndex].Cells["Color"].Style.ForeColor = color;
                        gridCategories.Rows[rowIndex].Cells["Color"].Style.Font = new Font(gridCategories.Font, FontStyle.Bold);
                    }
                    catch
                    {
                        // Ignore invalid color
                    }
                }
            }
        }

        private void UpdateColorPreview()
        {
            try
            {
                colorPreview.StateCommon.Color1 = ColorTranslator.FromHtml(txtColor.Text);
            }
            catch
            {
                colorPreview.StateCommon.Color1 = Color.Gray;
            }
        }

        private Category? GetSelectedCategory()
        {
            if (gridCategories.SelectedRows.Count == 0)
            {
                return null;
            }
            return gridCategories.SelectedRows[0].Tag as Category;
        }

        private async Task AddCategoryAsync()
        {
            var name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var color = string.IsNullOrWhiteSpace(txtColor.Text) ? null : txtColor.Text.Trim();

            try
            {
                var newCat = await apiClient.CreateCategoryAsync(name, color);
                categories.Add(newCat);
                RefreshGrid();
                txtName.Clear();
                txtColor.Clear();
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            }
        }

        private async Task UpdateCategoryAsync()
        {
            var selected = GetSelectedCategory();
            if (selected == null)
            {
                return;
            }

            var name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var color = string.IsNullOrWhiteSpace(txtColor.Text) ? null : txtColor.Text.Trim();

            try
            {
                var updated = await apiClient.UpdateCategoryAsync(selected.CategoryId, name, color);
                selected.Name = updated.Name;
                selected.Color = updated.Color;
                RefreshGrid();
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            }
        }

        private async Task DeleteCategoryAsync()
        {
            var selected = GetSelectedCategory();
            if (selected == null)
            {
                return;
            }

            var result = KryptonMessageBox.Show(this,
                $"Delete category \"{selected.Name}\"? Tasks using this category will become uncategorized.",
                "Confirm Delete",
                KryptonMessageBoxButtons.YesNo, KryptonMessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await apiClient.DeleteCategoryAsync(selected.CategoryId);
                    categories.Remove(selected);
                    RefreshGrid();
                    txtName.Clear();
                    txtColor.Clear();
                }
                catch (Exception ex)
                {
                    KryptonMessageBox.Show(this, $"Error: {ex.Message}", "Error",
                        KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
                }
            }
        }
    }
}
