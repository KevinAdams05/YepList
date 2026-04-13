using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.ApiClient;
using ToDoList.Windows.Controls;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Forms
{
    public class MainForm : KryptonForm
    {
        private readonly TodoApiClient apiClient;

        // Colors
        private static readonly Color SidebarBg = Color.FromArgb(247, 247, 247);
        private static readonly Color SidebarSelectedBg = Color.FromArgb(232, 240, 254);
        private static readonly Color SidebarSelectedText = Color.FromArgb(53, 122, 232);
        private static readonly Color SidebarHoverBg = Color.FromArgb(237, 237, 237);
        private static readonly Color HeaderBg = Color.FromArgb(250, 250, 250);
        private static readonly Color ContentBg = Color.White;
        private static readonly Color BorderColor = Color.FromArgb(222, 222, 222);
        private static readonly Color SubtextColor = Color.FromArgb(120, 120, 120);

        // Controls
        private Panel sidebarPanel = null!;
        private Panel listPanel = null!;
        private Panel contentPanel = null!;
        private Panel headerPanel = null!;
        private Label lblHeaderTitle = null!;
        private Panel taskListPanel = null!;
        private Label lblStatus = null!;
        private System.Windows.Forms.Timer syncTimer = null!;

        // State
        private List<TodoList> lists = new();
        private List<Category> categories = new();
        private List<TodoItem> currentItems = new();
        private long selectedListId = -1;
        private TaskPanel? selectedTaskPanel;

        public MainForm(TodoApiClient apiClient)
        {
            this.apiClient = apiClient;
            InitializeComponents();
            SetupSyncTimer();
        }

        private void InitializeComponents()
        {
            Text = "YepList";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 500);
            BackColor = ContentBg;

            string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }

            // ── Sidebar ────────────────────────────────────────
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = SidebarBg
            };

            // Sidebar header
            Panel sidebarHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = SidebarBg,
                Padding = new Padding(16, 0, 16, 0)
            };

            Label lblSidebarTitle = new Label
            {
                Text = "Lists",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = true
            };
            sidebarHeader.Controls.Add(lblSidebarTitle);

            // List items (scrollable panel, items dock Top)
            listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = SidebarBg,
                Padding = new Padding(8, 4, 8, 4)
            };

            // Sidebar bottom buttons
            Panel sidebarBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = SidebarBg,
                Padding = new Padding(8, 8, 8, 8)
            };

            FlatButton btnNewList = new FlatButton
            {
                Text = "New List",
                Dock = DockStyle.Top,
                Height = 34,
                NormalBackColor = SidebarBg,
                HoverBackColor = SidebarHoverBg
            };
            btnNewList.Margin = new Padding(0, 0, 0, 4);
            btnNewList.Click += async (s, e) => await ManageListsAsync();

            FlatButton btnManageCategories = new FlatButton
            {
                Text = "Manage Categories",
                Dock = DockStyle.Top,
                Height = 34,
                NormalBackColor = SidebarBg,
                HoverBackColor = SidebarHoverBg
            };
            btnManageCategories.Click += async (s, e) => await ManageCategoriesAsync();

            sidebarBottom.Controls.Add(btnManageCategories);
            sidebarBottom.Controls.Add(btnNewList);

            sidebarPanel.Controls.Add(listPanel);
            sidebarPanel.Controls.Add(sidebarBottom);
            sidebarPanel.Controls.Add(sidebarHeader);

            // Sidebar border
            Panel sidebarBorder = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = BorderColor
            };

            // ── Content area ───────────────────────────────────
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ContentBg
            };

            // Header bar
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = HeaderBg,
                Padding = new Padding(20, 0, 16, 0)
            };

            Panel headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BorderColor
            };
            headerPanel.Controls.Add(headerBorder);

            lblHeaderTitle = new Label
            {
                Text = "Tasks",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = true
            };
            headerPanel.Controls.Add(lblHeaderTitle);

            // Header buttons (right-aligned)
            FlowLayoutPanel headerButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = HeaderBg
            };

            AccentButton btnAddTask = new AccentButton { Text = "Add Task", Width = 100 };
            btnAddTask.Click += async (s, e) => await AddTaskAsync();
            headerButtons.Controls.Add(btnAddTask);

            FlatButton btnEditTask = new FlatButton { Text = "Edit", Width = 70, Margin = new Padding(4, 0, 0, 0) };
            btnEditTask.Click += async (s, e) => await EditTaskAsync();
            headerButtons.Controls.Add(btnEditTask);

            FlatButton btnDeleteTask = new FlatButton { Text = "Delete", Width = 70, Margin = new Padding(4, 0, 0, 0) };
            btnDeleteTask.Click += async (s, e) => await DeleteTaskAsync();
            headerButtons.Controls.Add(btnDeleteTask);

            FlatButton btnRefresh = new FlatButton { Text = "Refresh", Width = 76, Margin = new Padding(4, 0, 0, 0) };
            btnRefresh.Click += async (s, e) => await FullRefreshAsync();
            headerButtons.Controls.Add(btnRefresh);

            FlatButton btnAbout = new FlatButton { Text = "About", Width = 66, Margin = new Padding(4, 0, 0, 0) };
            btnAbout.Click += (s, e) => { using AboutForm form = new AboutForm(); form.ShowDialog(this); };
            headerButtons.Controls.Add(btnAbout);

            headerPanel.Controls.Add(headerButtons);

            // Task list (scrollable panel, items dock Top)
            Panel taskListContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ContentBg,
                Padding = new Padding(12, 8, 12, 8)
            };

            taskListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ContentBg
            };
            taskListContainer.Controls.Add(taskListPanel);

            // Status bar
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = HeaderBg
            };

            Panel statusBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            statusPanel.Controls.Add(statusBorder);

            lblStatus = new Label
            {
                Text = "Connecting...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = SubtextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                UseCompatibleTextRendering = true
            };
            statusPanel.Controls.Add(lblStatus);

            contentPanel.Controls.Add(taskListContainer);
            contentPanel.Controls.Add(headerPanel);
            contentPanel.Controls.Add(statusPanel);

            // ── Assemble ───────────────────────────────────────
            Controls.Add(contentPanel);
            Controls.Add(sidebarBorder);
            Controls.Add(sidebarPanel);
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

        // ── Data Loading ────────────────────────────────────────

        private async Task FullRefreshAsync()
        {
            try
            {
                apiClient.ResetSyncTime();
                lists = await apiClient.GetListsAsync();
                categories = await apiClient.GetCategoriesAsync();
                RefreshSidebarLists();

                // Auto-select first list if none selected
                if (selectedListId < 0 && lists.Count > 0)
                {
                    selectedListId = lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name).First().ListId;
                    RefreshSidebarLists();
                }

                if (selectedListId > 0)
                {
                    currentItems = await apiClient.GetItemsByListAsync(selectedListId);
                    RefreshTaskList();

                    TodoList? selectedList = lists.FirstOrDefault(l => l.ListId == selectedListId);
                    if (selectedList != null)
                    {
                        lblHeaderTitle.Text = selectedList.Name;
                    }
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
                SyncResponse syncResult = await apiClient.SyncAsync();

                foreach (TodoList list in syncResult.Lists)
                {
                    TodoList? existing = lists.FirstOrDefault(l => l.ListId == list.ListId);
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

                foreach (Category cat in syncResult.Categories)
                {
                    Category? existing = categories.FirstOrDefault(c => c.CategoryId == cat.CategoryId);
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

                if (selectedListId > 0)
                {
                    foreach (TodoItem item in syncResult.Items.Where(i => i.ListId == selectedListId))
                    {
                        TodoItem? existing = currentItems.FirstOrDefault(i => i.ItemId == item.ItemId);
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
                    RefreshTaskList();
                }

                if (syncResult.DeletedListIds.Contains(selectedListId))
                {
                    selectedListId = -1;
                    currentItems.Clear();
                    RefreshTaskList();
                }

                RefreshSidebarLists();
                UpdateStatus($"Synced at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Sync error: {ex.Message}");
            }
        }

        // ── UI Refresh ──────────────────────────────────────────

        private void RefreshSidebarLists()
        {
            listPanel.SuspendLayout();
            listPanel.Controls.Clear();

            // Add in reverse order because Dock.Top stacks bottom-up
            List<TodoList> ordered = lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name).ToList();
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                TodoList list = ordered[i];
                bool isSelected = list.ListId == selectedListId;

                SidebarItem row = new SidebarItem
                {
                    Text = list.Name,
                    Dock = DockStyle.Top,
                    Height = 40,
                    Tag = list.ListId,
                    IsItemSelected = isSelected
                };

                row.Click += (s, e) =>
                {
                    long listId = (long)((Control)s!).Tag;
                    _ = OnListSelectedAsync(listId);
                };

                listPanel.Controls.Add(row);
            }

            listPanel.ResumeLayout();
        }

        private void RefreshTaskList()
        {
            taskListPanel.SuspendLayout();
            taskListPanel.Controls.Clear();
            selectedTaskPanel = null;

            // Add in reverse order because Dock.Top stacks bottom-up
            List<TodoItem> ordered = currentItems
                .OrderBy(i => i.IsCompleted)
                .ThenBy(i => i.SortOrder)
                .ThenBy(i => i.Title)
                .ToList();

            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                TodoItem item = ordered[i];
                string categoryName = "";
                Color? categoryColor = null;

                if (item.CategoryId.HasValue)
                {
                    Category? cat = categories.FirstOrDefault(c => c.CategoryId == item.CategoryId.Value);
                    categoryName = cat?.Name ?? "";
                    if (cat?.Color != null)
                    {
                        try
                        {
                            categoryColor = ColorTranslator.FromHtml(cat.Color);
                        }
                        catch
                        {
                            // Invalid color
                        }
                    }
                }

                TaskPanel panel = new TaskPanel(item, categoryName, categoryColor)
                {
                    Dock = DockStyle.Top
                };

                panel.CompletionToggled += async (s, e) =>
                {
                    if (s is TaskPanel tp)
                    {
                        await ToggleCompleteAsync(tp.Item);
                    }
                };
                panel.TaskDoubleClicked += async (s, e) =>
                {
                    if (s is TaskPanel tp)
                    {
                        selectedTaskPanel = tp;
                        await EditTaskAsync();
                    }
                };
                panel.Click += (s, e) =>
                {
                    if (selectedTaskPanel != null)
                    {
                        selectedTaskPanel.IsSelected = false;
                    }

                    TaskPanel tp = (TaskPanel)s!;
                    tp.IsSelected = true;
                    selectedTaskPanel = tp;
                };

                taskListPanel.Controls.Add(panel);
            }

            taskListPanel.ResumeLayout();
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = $"  {apiClient.BaseUrl}  |  {message}";
        }

        // ── Event Handlers ──────────────────────────────────────

        private async Task OnListSelectedAsync(long listId)
        {
            selectedListId = listId;
            RefreshSidebarLists();

            TodoList? selectedList = lists.FirstOrDefault(l => l.ListId == listId);
            if (selectedList != null)
            {
                lblHeaderTitle.Text = selectedList.Name;
            }

            try
            {
                currentItems = await apiClient.GetItemsByListAsync(listId);
                RefreshTaskList();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading tasks: {ex.Message}");
            }
        }

        private async Task ToggleCompleteAsync(TodoItem item)
        {
            try
            {
                TodoItem updated = await apiClient.ToggleCompleteAsync(item.ItemId, !item.IsCompleted);
                item.IsCompleted = updated.IsCompleted;
                item.ModifiedDate = updated.ModifiedDate;
                RefreshTaskList();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        // ── Task CRUD ───────────────────────────────────────────

        private async Task AddTaskAsync()
        {
            if (selectedListId < 0)
            {
                KryptonMessageBox.Show(this, "Please select a list first.", "No List Selected",
                    KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Information);

                return;
            }

            using TaskEditForm form = new TaskEditForm(categories, null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    TodoItem newItem = await apiClient.CreateItemAsync(
                        selectedListId, form.TaskTitle, form.TaskNotes,
                        form.SelectedCategoryId, form.TaskDueDate, 0);
                    currentItems.Add(newItem);
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error creating task: {ex.Message}");
                }
            }
        }

        private async Task EditTaskAsync()
        {
            TodoItem? item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            using TaskEditForm form = new TaskEditForm(categories, item);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    TodoItem updated = await apiClient.UpdateItemAsync(
                        item.ItemId, form.TaskTitle, form.TaskNotes,
                        form.SelectedCategoryId, item.IsCompleted, form.TaskDueDate, item.SortOrder);
                    item.Title = updated.Title;
                    item.Notes = updated.Notes;
                    item.CategoryId = updated.CategoryId;
                    item.DueDate = updated.DueDate;
                    item.ModifiedDate = updated.ModifiedDate;
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error updating task: {ex.Message}");
                }
            }
        }

        private async Task DeleteTaskAsync()
        {
            TodoItem? item = GetSelectedItem();
            if (item == null)
            {
                return;
            }

            DialogResult result = KryptonMessageBox.Show(this,
                $"Delete task \"{item.Title}\"?", "Confirm Delete",
                KryptonMessageBoxButtons.YesNo, KryptonMessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await apiClient.DeleteItemAsync(item.ItemId);
                    currentItems.Remove(item);
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error deleting task: {ex.Message}");
                }
            }
        }

        private TodoItem? GetSelectedItem()
        {
            return selectedTaskPanel?.Item;
        }

        // ── List / Category Management ──────────────────────────

        private async Task ManageListsAsync()
        {
            using ListManagerForm form = new ListManagerForm(apiClient, lists);
            form.ShowDialog(this);
            lists = await apiClient.GetListsAsync();
            RefreshSidebarLists();
        }

        private async Task ManageCategoriesAsync()
        {
            using CategoryManagerForm form = new CategoryManagerForm(apiClient, categories);
            form.ShowDialog(this);
            categories = await apiClient.GetCategoriesAsync();
            RefreshTaskList();
        }
    }
}
