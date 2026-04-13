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
        private Panel colorPreview = null!;
        private Controls.FlatButton btnPickColor = null!;
        private Controls.FlatButton btnAdd = null!;
        private Controls.FlatButton btnUpdate = null!;
        private Controls.FlatButton btnDelete = null!;
        private Controls.AccentButton btnClose = null!;

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
            Size = new Size(560, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Grid
            gridCategories = new KryptonDataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(390, 240),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 60 };
            DataGridViewTextBoxColumn colColor = new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "Color", FillWeight = 40 };
            gridCategories.Columns.AddRange(new DataGridViewColumn[] { colName, colColor });

            gridCategories.SelectionChanged += (s, e) =>
            {
                Category? cat = GetSelectedCategory();
                if (cat != null)
                {
                    txtName.Text = cat.Name;
                    txtColor.Text = cat.Color ?? "";
                    UpdateColorPreview();
                }
            };

            // Input fields
            KryptonLabel lblName = new KryptonLabel { Text = "Name", Location = new Point(20, 278), AutoSize = true };
            lblName.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            txtName = new KryptonTextBox { Location = new Point(90, 276), Width = 320 };
            txtName.StateCommon.Border.Rounding = 4;

            KryptonLabel lblColor = new KryptonLabel { Text = "Color", Location = new Point(20, 318), AutoSize = true };
            lblColor.StateCommon.ShortText.Font = new Font("Segoe UI", 9.5f);
            txtColor = new KryptonTextBox { Location = new Point(90, 316), Width = 200 };
            txtColor.StateCommon.Border.Rounding = 4;
            txtColor.TextChanged += (s, e) => UpdateColorPreview();

            colorPreview = new Panel
            {
                Location = new Point(298, 316),
                Size = new Size(28, 26),
                BackColor = Color.Gray
            };

            btnPickColor = new Controls.FlatButton { Text = "...", Width = 36, Height = 26, Location = new Point(332, 316) };
            btnPickColor.Click += (s, e) =>
            {
                using ColorDialog dlg = new ColorDialog();
                if (!string.IsNullOrEmpty(txtColor.Text))
                {
                    try
                    {
                        dlg.Color = ColorTranslator.FromHtml(txtColor.Text);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtColor.Text = ColorTranslator.ToHtml(dlg.Color);
                }
            };

            // Buttons
            btnAdd = new Controls.FlatButton { Text = "Add", Width = 110, Location = new Point(430, 20) };
            btnAdd.Click += async (s, e) => await AddCategoryAsync();

            btnUpdate = new Controls.FlatButton { Text = "Update", Width = 110, Location = new Point(430, 60) };
            btnUpdate.Click += async (s, e) => await UpdateCategoryAsync();

            btnDelete = new Controls.FlatButton { Text = "Delete", Width = 110, Location = new Point(430, 100) };
            btnDelete.Click += async (s, e) => await DeleteCategoryAsync();

            btnClose = new Controls.AccentButton { Text = "Close", Width = 110, Location = new Point(430, 376) };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; };

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
            foreach (Category cat in categories.OrderBy(c => c.Name))
            {
                int rowIndex = gridCategories.Rows.Add(cat.Name, cat.Color ?? "");
                gridCategories.Rows[rowIndex].Tag = cat;

                if (cat.Color != null)
                {
                    try
                    {
                        Color color = ColorTranslator.FromHtml(cat.Color);
                        gridCategories.Rows[rowIndex].Cells["Color"].Style.ForeColor = color;
                        gridCategories.Rows[rowIndex].Cells["Color"].Style.Font = new Font(gridCategories.Font, FontStyle.Bold);
                    }
                    catch
                    {
                        // Invalid color
                    }
                }
            }
        }

        private void UpdateColorPreview()
        {
            try
            {
                colorPreview.BackColor = ColorTranslator.FromHtml(txtColor.Text);
            }
            catch
            {
                colorPreview.BackColor = Color.Gray;
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
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            string? color = string.IsNullOrWhiteSpace(txtColor.Text) ? null : txtColor.Text.Trim();

            try
            {
                Category newCat = await apiClient.CreateCategoryAsync(name, color);
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
            Category? selected = GetSelectedCategory();
            if (selected == null)
            {
                return;
            }

            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            string? color = string.IsNullOrWhiteSpace(txtColor.Text) ? null : txtColor.Text.Trim();

            try
            {
                Category updated = await apiClient.UpdateCategoryAsync(selected.CategoryId, name, color);
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
            Category? selected = GetSelectedCategory();
            if (selected == null)
            {
                return;
            }

            DialogResult result = KryptonMessageBox.Show(this,
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
