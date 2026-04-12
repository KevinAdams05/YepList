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
    public class ListManagerForm : KryptonForm
    {
        private readonly TodoApiClient apiClient;

        private KryptonListBox lstLists = null!;
        private KryptonTextBox txtName = null!;
        private KryptonButton btnAdd = null!;
        private KryptonButton btnRename = null!;
        private KryptonButton btnDelete = null!;
        private KryptonButton btnClose = null!;

        private List<TodoList> lists;

        public ListManagerForm(TodoApiClient apiClient, List<TodoList> lists)
        {
            this.apiClient = apiClient;
            this.lists = lists.ToList();
            InitializeComponents();
            RefreshListBox();
        }

        private void InitializeComponents()
        {
            Text = "Manage Lists";
            Size = new Size(400, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            lstLists = new KryptonListBox
            {
                Location = new Point(12, 12),
                Size = new Size(250, 200)
            };
            lstLists.SelectedIndexChanged += (s, e) =>
            {
                if (lstLists.SelectedItem is TodoList selected)
                {
                    txtName.Text = selected.Name;
                }
            };

            txtName = new KryptonTextBox
            {
                Location = new Point(12, 222),
                Width = 250
            };

            btnAdd = new KryptonButton { Text = "Add", Location = new Point(275, 12), Width = 100 };
            btnRename = new KryptonButton { Text = "Rename", Location = new Point(275, 48), Width = 100 };
            btnDelete = new KryptonButton { Text = "Delete", Location = new Point(275, 84), Width = 100 };
            btnClose = new KryptonButton { Text = "Close", Location = new Point(275, 280), Width = 100, DialogResult = DialogResult.OK };

            btnAdd.Click += async (s, e) => await AddListAsync();
            btnRename.Click += async (s, e) => await RenameListAsync();
            btnDelete.Click += async (s, e) => await DeleteListAsync();

            CancelButton = btnClose;

            Controls.AddRange(new Control[] { lstLists, txtName, btnAdd, btnRename, btnDelete, btnClose });
        }

        private void RefreshListBox()
        {
            lstLists.Items.Clear();
            foreach (TodoList list in lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name))
            {
                lstLists.Items.Add(list);
            }
        }

        private async Task AddListAsync()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                TodoList newList = await apiClient.CreateListAsync(name);
                lists.Add(newList);
                RefreshListBox();
                txtName.Clear();
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            }
        }

        private async Task RenameListAsync()
        {
            if (lstLists.SelectedItem is not TodoList selected)
            {
                return;
            }

            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                TodoList updated = await apiClient.UpdateListAsync(selected.ListId, name, selected.SortOrder);
                selected.Name = updated.Name;
                RefreshListBox();
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            }
        }

        private async Task DeleteListAsync()
        {
            if (lstLists.SelectedItem is not TodoList selected)
            {
                return;
            }

            DialogResult result = KryptonMessageBox.Show(this,
                $"Delete list \"{selected.Name}\" and all its tasks?", "Confirm Delete",
                KryptonMessageBoxButtons.YesNo, KryptonMessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await apiClient.DeleteListAsync(selected.ListId);
                    lists.Remove(selected);
                    RefreshListBox();
                    txtName.Clear();
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
