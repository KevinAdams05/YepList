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
    public class ListManagerForm : Form
    {
        private readonly TodoApiClient apiClient;

        private KryptonListBox lstLists = null!;
        private KryptonTextBox txtName = null!;
        private Controls.FlatButton btnAdd = null!;
        private Controls.FlatButton btnRename = null!;
        private Controls.FlatButton btnDelete = null!;
        private Controls.AccentButton btnClose = null!;

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
            Size = new Size(440, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            lstLists = new KryptonListBox
            {
                Location = new Point(20, 20),
                Size = new Size(270, 220)
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
                Location = new Point(20, 252),
                Width = 270
            };
            txtName.StateCommon.Border.Rounding = 4;

            btnAdd = new Controls.FlatButton { Text = "Add", Width = 110, Location = new Point(308, 20) };
            btnAdd.Click += async (s, e) => await AddListAsync();

            btnRename = new Controls.FlatButton { Text = "Rename", Width = 110, Location = new Point(308, 60) };
            btnRename.Click += async (s, e) => await RenameListAsync();

            btnDelete = new Controls.FlatButton { Text = "Delete", Width = 110, Location = new Point(308, 100) };
            btnDelete.Click += async (s, e) => await DeleteListAsync();

            btnClose = new Controls.AccentButton { Text = "Close", Width = 110, Location = new Point(308, 316) };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; };

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
                MessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, $"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteListAsync()
        {
            if (lstLists.SelectedItem is not TodoList selected)
            {
                return;
            }

            DialogResult result = MessageBox.Show(this,
                $"Delete list \"{selected.Name}\" and all its tasks?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                    MessageBox.Show(this, $"Error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
