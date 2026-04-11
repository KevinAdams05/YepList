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
    public class MainForm : KryptonForm
    {
        private readonly TodoApiClient apiClient;

        // Controls
        private KryptonTreeView listTreeView = null!;
        private KryptonDataGridView taskGrid = null!;
        private KryptonButton btnNewList = null!;
        private KryptonButton btnManageCategories = null!;
        private KryptonButton btnAddTask = null!;
        private KryptonButton btnEditTask = null!;
        private KryptonButton btnDeleteTask = null!;
        private KryptonButton btnRefresh = null!;
        private KryptonLabel lblStatus = null!;
        private System.Windows.Forms.Timer syncTimer = null!;
        private KryptonSplitContainer splitContainer = null!;

        // State
        private List<TodoList> lists = new();
        private List<Category> categories = new();
        private List<TodoItem> currentItems = new();
        private long selectedListId = -1;

        public MainForm(TodoApiClient apiClient)
        {
            this.apiClient = apiClient;
            InitializeComponents();
            SetupSyncTimer();
        }

        private void InitializeComponents()
        {
            Text = "ToDoList";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 500);

            // ── Toolbar ─────────────────────────────────────
            var toolPanel = new KryptonPanel { Dock = DockStyle.Top, Height = 45 };

            btnAddTask = new KryptonButton { Text = "Add Task", Location = new Point(8, 8), Width = 90 };
            btnEditTask = new KryptonButton { Text = "Edit Task", Location = new Point(104, 8), Width = 90 };
            btnDeleteTask = new KryptonButton { Text = "Delete Task", Location = new Point(200, 8), Width = 100 };
            btnRefresh = new KryptonButton { Text = "Refresh", Location = new Point(316, 8), Width = 80 };

            btnAddTask.Click += async (s, e) => await AddTaskAsync();
            btnEditTask.Click += async (s, e) => await EditTaskAsync();
            btnDeleteTask.Click += async (s, e) => await DeleteTaskAsync();
            btnRefresh.Click += async (s, e) => await FullRefreshAsync();

            toolPanel.Controls.AddRange(new Control[] { btnAddTask, btnEditTask, btnDeleteTask, btnRefresh });

            // ── Split Container ─────────────────────────────
            splitContainer = new KryptonSplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 220,
                FixedPanel = FixedPanel.Panel1
            };

            // ── Left Panel (Lists) ──────────────────────────
            var leftPanel = new KryptonPanel { Dock = DockStyle.Fill };

            var lblLists = new KryptonLabel
            {
                Text = "Lists",
                Dock = DockStyle.Top,
                LabelStyle = LabelStyle.BoldControl
            };
            lblLists.StateCommon.ShortText.Font = new Font("Segoe UI", 11f, FontStyle.Bold);

            listTreeView = new KryptonTreeView
            {
                Dock = DockStyle.Fill,
                ShowLines = false
            };
            listTreeView.AfterSelect += async (s, e) => await OnListSelectedAsync();

            var leftButtonPanel = new KryptonPanel { Dock = DockStyle.Bottom, Height = 75 };

            btnNewList = new KryptonButton
            {
                Text = "New List",
                Location = new Point(8, 5),
                Width = 195
            };
            btnNewList.Click += async (s, e) => await ManageListsAsync();

            btnManageCategories = new KryptonButton
            {
                Text = "Manage Categories",
                Location = new Point(8, 38),
                Width = 195
            };
            btnManageCategories.Click += async (s, e) => await ManageCategoriesAsync();

            leftButtonPanel.Controls.AddRange(new Control[] { btnNewList, btnManageCategories });

            leftPanel.Controls.Add(listTreeView);
            leftPanel.Controls.Add(leftButtonPanel);
            leftPanel.Controls.Add(lblLists);

            splitContainer.Panel1.Controls.Add(leftPanel);

            // ── Right Panel (Tasks) ─────────────────────────
            taskGrid = new KryptonDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            // Columns
            var colCompleted = new DataGridViewCheckBoxColumn
            {
                Name = "IsCompleted",
                HeaderText = "",
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            var colTitle = new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "Task",
                ReadOnly = true,
                FillWeight = 50
            };
            var colCategory = new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                ReadOnly = true,
                FillWeight = 20
            };
            var colDueDate = new DataGridViewTextBoxColumn
            {
                Name = "DueDate",
                HeaderText = "Due Date",
                ReadOnly = true,
                FillWeight = 15
            };

            taskGrid.Columns.AddRange(new DataGridViewColumn[] { colCompleted, colTitle, colCategory, colDueDate });

            taskGrid.CellContentClick += async (s, e) => await OnCellClickAsync(e);
            taskGrid.CellDoubleClick += async (s, e) => await EditTaskAsync();

            splitContainer.Panel2.Controls.Add(taskGrid);

            // ── Status Bar ──────────────────────────────────
            var statusPanel = new KryptonPanel { Dock = DockStyle.Bottom, Height = 28 };
            lblStatus = new KryptonLabel
            {
                Text = "Connecting...",
                Dock = DockStyle.Fill
            };
            lblStatus.StateCommon.ShortText.Font = new Font("Segoe UI", 8.5f);
            statusPanel.Controls.Add(lblStatus);

            // ── Assemble ────────────────────────────────────
            Controls.Add(splitContainer);
            Controls.Add(toolPanel);
            Controls.Add(statusPanel);
        }

        private void SetupSyncTimer()
        {
            syncTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            syncTimer.Tick += async (s, e) => await SyncAsync();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await FullRefreshAsync();
            syncTimer.Start();
        }

        // ── Data Loading ────────────────────────────────────

        private async Task FullRefreshAsync()
        {
            try
            {
                apiClient.ResetSyncTime();
                lists = await apiClient.GetListsAsync();
                categories = await apiClient.GetCategoriesAsync();
                RefreshListTree();

                if (selectedListId > 0)
                {
                    currentItems = await apiClient.GetItemsByListAsync(selectedListId);
                    RefreshTaskGrid();
                }

                UpdateStatus("Connected");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private async Task SyncAsync()
        {
            try
            {
                var syncResult = await apiClient.SyncAsync();

                // Apply list changes
                foreach (var list in syncResult.Lists)
                {
                    var existing = lists.FirstOrDefault(l => l.ListId == list.ListId);
                    if (existing != null)
                    {
                        existing.Name = list.Name;
                        existing.SortOrder = list.SortOrder;
                        existing.ModifiedDate = list.ModifiedDate;
                    }
                    else
                    {
                        lists.Add(list);
                    }
                }
                lists.RemoveAll(l => syncResult.DeletedListIds.Contains(l.ListId));

                // Apply category changes
                foreach (var cat in syncResult.Categories)
                {
                    var existing = categories.FirstOrDefault(c => c.CategoryId == cat.CategoryId);
                    if (existing != null)
                    {
                        existing.Name = cat.Name;
                        existing.Color = cat.Color;
                        existing.ModifiedDate = cat.ModifiedDate;
                    }
                    else
                    {
                        categories.Add(cat);
                    }
                }
                categories.RemoveAll(c => syncResult.DeletedCategoryIds.Contains(c.CategoryId));

                // Apply item changes for current list
                if (selectedListId > 0)
                {
                    foreach (var item in syncResult.Items.Where(i => i.ListId == selectedListId))
                    {
                        var existing = currentItems.FirstOrDefault(i => i.ItemId == item.ItemId);
                        if (existing != null)
                        {
                            existing.Title = item.Title;
                            existing.Notes = item.Notes;
                            existing.CategoryId = item.CategoryId;
                            existing.IsCompleted = item.IsCompleted;
                            existing.DueDate = item.DueDate;
                            existing.SortOrder = item.SortOrder;
                            existing.ModifiedDate = item.ModifiedDate;
                        }
                        else
                        {
                            currentItems.Add(item);
                        }
                    }
                    currentItems.RemoveAll(i => syncResult.DeletedItemIds.Contains(i.ItemId));
                    RefreshTaskGrid();
                }

                // Check if selected list was deleted
                if (syncResult.DeletedListIds.Contains(selectedListId))
                {
                    selectedListId = -1;
                    currentItems.Clear();
                    RefreshTaskGrid();
                }

                RefreshListTree();
                UpdateStatus($"Synced at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Sync error: {ex.Message}");
            }
        }

        // ── UI Refresh ──────────────────────────────────────

        private void RefreshListTree()
        {
            listTreeView.BeginUpdate();
            listTreeView.Nodes.Clear();
            foreach (var list in lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name))
            {
                var node = new TreeNode(list.Name) { Tag = list.ListId };
                listTreeView.Nodes.Add(node);

                if (list.ListId == selectedListId)
                {
                    listTreeView.SelectedNode = node;
                }
            }
            listTreeView.EndUpdate();
        }

        private void RefreshTaskGrid()
        {
            taskGrid.Rows.Clear();
            foreach (var item in currentItems.OrderBy(i => i.IsCompleted).ThenBy(i => i.SortOrder).ThenBy(i => i.Title))
            {
                var categoryName = "";
                if (item.CategoryId.HasValue)
                {
                    var cat = categories.FirstOrDefault(c => c.CategoryId == item.CategoryId.Value);
                    categoryName = cat?.Name ?? "";
                }

                var dueDateText = item.DueDate?.ToString("yyyy-MM-dd") ?? "";
                var rowIndex = taskGrid.Rows.Add(item.IsCompleted, item.Title, categoryName, dueDateText);
                taskGrid.Rows[rowIndex].Tag = item;

                // Style completed tasks with strikethrough
                if (item.IsCompleted)
                {
                    taskGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                    taskGrid.Rows[rowIndex].DefaultCellStyle.Font = new Font(taskGrid.Font, FontStyle.Strikeout);
                }

                // Color the category cell by category color
                if (item.CategoryId.HasValue)
                {
                    var cat = categories.FirstOrDefault(c => c.CategoryId == item.CategoryId.Value);
                    if (cat?.Color != null)
                    {
                        try
                        {
                            var color = ColorTranslator.FromHtml(cat.Color);
                            taskGrid.Rows[rowIndex].Cells["Category"].Style.ForeColor = color;
                        }
                        catch
                        {
                            // Invalid color, ignore
                        }
                    }
                }
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = $"  {apiClient.BaseUrl}  |  {message}";
        }

        // ── Event Handlers ──────────────────────────────────

        private async Task OnListSelectedAsync()
        {
            if (listTreeView.SelectedNode?.Tag is long listId)
            {
                selectedListId = listId;
                try
                {
                    currentItems = await apiClient.GetItemsByListAsync(listId);
                    RefreshTaskGrid();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error loading tasks: {ex.Message}");
                }
            }
        }

        private async Task OnCellClickAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0)
            {
                return;
            }

            if (taskGrid.Rows[e.RowIndex].Tag is TodoItem item)
            {
                try
                {
                    var updated = await apiClient.ToggleCompleteAsync(item.ItemId, !item.IsCompleted);
                    item.IsCompleted = updated.IsCompleted;
                    item.ModifiedDate = updated.ModifiedDate;
                    RefreshTaskGrid();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error: {ex.Message}");
                }
            }
        }

        // ── Task CRUD ───────────────────────────────────────

        private async Task AddTaskAsync()
        {
            if (selectedListId < 0)
            {
                KryptonMessageBox.Show(this, "Please select a list first.", "No List Selected",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Information);
                return;
            }

            using var form = new TaskEditForm(categories, null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var newItem = await apiClient.CreateItemAsync(
                        selectedListId, form.TaskTitle, form.TaskNotes,
                        form.SelectedCategoryId, form.TaskDueDate, 0);
                    currentItems.Add(newItem);
                    RefreshTaskGrid();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error creating task: {ex.Message}");
                }
            }
        }

        private async Task EditTaskAsync()
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            using var form = new TaskEditForm(categories, item);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var updated = await apiClient.UpdateItemAsync(
                        item.ItemId, form.TaskTitle, form.TaskNotes,
                        form.SelectedCategoryId, item.IsCompleted, form.TaskDueDate, item.SortOrder);
                    item.Title = updated.Title;
                    item.Notes = updated.Notes;
                    item.CategoryId = updated.CategoryId;
                    item.DueDate = updated.DueDate;
                    item.ModifiedDate = updated.ModifiedDate;
                    RefreshTaskGrid();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error updating task: {ex.Message}");
                }
            }
        }

        private async Task DeleteTaskAsync()
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            var result = KryptonMessageBox.Show(this,
                $"Delete task \"{item.Title}\"?", "Confirm Delete",
                KryptonMessageBoxButtons.YesNo, KryptonMessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await apiClient.DeleteItemAsync(item.ItemId);
                    currentItems.Remove(item);
                    RefreshTaskGrid();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error deleting task: {ex.Message}");
                }
            }
        }

        private TodoItem? GetSelectedItem()
        {
            if (taskGrid.SelectedRows.Count == 0)
            {
                return null;
            }
            return taskGrid.SelectedRows[0].Tag as TodoItem;
        }

        // ── List / Category Management ──────────────────────

        private async Task ManageListsAsync()
        {
            using var form = new ListManagerForm(apiClient, lists);
            form.ShowDialog(this);
            lists = await apiClient.GetListsAsync();
            RefreshListTree();
        }

        private async Task ManageCategoriesAsync()
        {
            using var form = new CategoryManagerForm(apiClient, categories);
            form.ShowDialog(this);
            categories = await apiClient.GetCategoriesAsync();
            RefreshTaskGrid();
        }
    }
}
