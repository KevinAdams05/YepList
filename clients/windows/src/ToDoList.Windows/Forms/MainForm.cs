using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToDoList.Windows.ApiClient;
using ToDoList.Windows.Controls;
using ToDoList.Windows.Debug;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.Forms
{
    public class MainForm : Form
    {
        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private static void BeginUpdate(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        private static void EndUpdate(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            control.Invalidate(true);
        }

        private readonly TodoApiClient apiClient;
        private readonly AppSettings settings;

        // Controls
        private Panel sidebarPanel = null!;
        private Panel listPanel = null!;
        private Panel contentPanel = null!;
        private Panel headerPanel = null!;
        private Label lblHeaderTitle = null!;
        private Control[] headerButtons = null!;
        private Panel taskListPanel = null!;
        private Panel dragIndicator = null!;
        private TextBox txtQuickAdd = null!;
        private Label lblStatus = null!;
        private System.Windows.Forms.Timer syncTimer = null!;

        // State
        private List<TodoList> lists = new();
        private List<Category> categories = new();
        private List<TodoItem> currentItems = new();
        private long selectedListId = -1;
        private List<TaskPanel> selectedTaskPanels = new();

        public MainForm(TodoApiClient apiClient, AppSettings settings)
        {
            this.apiClient = apiClient;
            this.settings = settings;
            InitializeComponents();
            AppTheme.StyleForm(this);
            SetupSyncTimer();
        }

        private void InitializeComponents()
        {
            Text = "YepList";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 500);
            BackColor = AppTheme.ContentBg;

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
                BackColor = AppTheme.SidebarBg
            };

            // Sidebar logo
            PictureBox logoBox = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 48,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(40, 10, 40, 4)
            };

            string logoPath = Path.Combine(AppContext.BaseDirectory, AppTheme.LogoFileName);
            if (File.Exists(logoPath))
            {
                logoBox.Image = Image.FromFile(logoPath);
            }

            // Sidebar header
            Panel sidebarHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(16, 0, 16, 0)
            };

            Label lblSidebarTitle = new Label
            {
                Text = "Lists",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = AppTheme.TitleColor,
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
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(8, 4, 8, 4)
            };

            // Sidebar bottom buttons
            Panel sidebarBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(8, 8, 8, 8)
            };

            FlatButton btnNewList = new FlatButton
            {
                Text = "New List",
                Dock = DockStyle.Top,
                Height = 34,
                NormalBackColor = AppTheme.SidebarBg,
                HoverBackColor = AppTheme.SidebarHoverBg
            };
            btnNewList.Margin = new Padding(0, 0, 0, 4);
            btnNewList.Click += async (s, e) => await ManageListsAsync();

            FlatButton btnManageCategories = new FlatButton
            {
                Text = "Manage Categories",
                Dock = DockStyle.Top,
                Height = 34,
                NormalBackColor = AppTheme.SidebarBg,
                HoverBackColor = AppTheme.SidebarHoverBg
            };
            btnManageCategories.Click += async (s, e) => await ManageCategoriesAsync();

            sidebarBottom.Controls.Add(btnManageCategories);
            sidebarBottom.Controls.Add(btnNewList);

            sidebarPanel.Controls.Add(listPanel);
            sidebarPanel.Controls.Add(sidebarBottom);
            sidebarPanel.Controls.Add(sidebarHeader);
            sidebarPanel.Controls.Add(logoBox);

            // Sidebar border
            Panel sidebarBorder = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = AppTheme.BorderColor
            };

            // ── Content area ───────────────────────────────────
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.ContentBg
            };

            // Header bar
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = AppTheme.HeaderBg,
                Padding = new Padding(20, 0, 16, 0)
            };

            Panel headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = AppTheme.BorderColor
            };
            headerPanel.Controls.Add(headerBorder);

            int btnHeight = 34;
            int btnY = (56 - btnHeight) / 2;

            lblHeaderTitle = new Label
            {
                Text = "Tasks",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = AppTheme.TitleColor,
                Location = new Point(20, 0),
                AutoSize = true,
                Height = 56,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = true
            };
            headerPanel.Controls.Add(lblHeaderTitle);

            // Header buttons — absolute positioned, vertically centered
            AccentButton btnAddTask = new AccentButton { Text = "Add Task", Width = 100, Height = btnHeight };
            btnAddTask.Click += async (s, e) => await AddTaskAsync();

            FlatButton btnEditTask = new FlatButton { Text = "Edit", Width = 70, Height = btnHeight };
            btnEditTask.Click += async (s, e) => await EditTaskAsync();

            FlatButton btnDeleteTask = new FlatButton { Text = "Delete", Width = 70, Height = btnHeight };
            btnDeleteTask.Click += async (s, e) => await DeleteTaskAsync();

            FlatButton btnRefresh = new FlatButton { Text = "Refresh", Width = 76, Height = btnHeight };
            btnRefresh.Click += async (s, e) => await FullRefreshAsync();

            FlatButton btnSettings = new FlatButton { Text = "Settings", Width = 80, Height = btnHeight };
            btnSettings.Click += (s, e) =>
            {
                using SettingsForm form = new SettingsForm(settings);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    settings.ServerUrl = form.ServerUrl;
                    settings.Save();
                }
            };

            FlatButton btnAbout = new FlatButton { Text = "About", Width = 66, Height = btnHeight };
            btnAbout.Click += (s, e) => { using AboutForm form = new AboutForm(); form.ShowDialog(this); };

            headerButtons = new Control[] { btnAddTask, btnEditTask, btnDeleteTask, btnRefresh, btnSettings, btnAbout };
            foreach (Control btn in headerButtons)
            {
                btn.Top = btnY;
                headerPanel.Controls.Add(btn);
            }

            headerPanel.Resize += (s, e) => LayoutHeaderButtons();

            // Task list (scrollable panel, items dock Top)
            Panel taskListContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.ContentBg,
                Padding = new Padding(12, 8, 12, 8)
            };

            taskListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = AppTheme.ContentBg,
                AllowDrop = true
            };
            // Enable double buffering to prevent flicker during refreshes
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, taskListPanel, new object[] { true });

            taskListPanel.DragEnter += TaskListPanel_DragEnter;
            taskListPanel.DragOver += TaskListPanel_DragOver;
            taskListPanel.DragDrop += TaskListPanel_DragDrop;
            taskListPanel.DragLeave += TaskListPanel_DragLeave;

            // Drag indicator line
            dragIndicator = new Panel
            {
                Height = 3,
                BackColor = AppTheme.AccentBg,
                Visible = false
            };
            taskListPanel.Controls.Add(dragIndicator);

            taskListContainer.Controls.Add(taskListPanel);

            // Status bar
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = AppTheme.HeaderBg
            };

            Panel statusBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = AppTheme.BorderColor
            };
            statusPanel.Controls.Add(statusBorder);

            lblStatus = new Label
            {
                Text = "Connecting...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.SubtextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                UseCompatibleTextRendering = true
            };
            statusPanel.Controls.Add(lblStatus);

            // Quick-add bar
            Panel quickAddPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = AppTheme.HeaderBg,
                Padding = new Padding(16, 8, 16, 8)
            };

            Panel quickAddBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = AppTheme.BorderColor
            };
            quickAddPanel.Controls.Add(quickAddBorder);

            txtQuickAdd = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f),
                PlaceholderText = "Add a new task...",
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.HeaderBg,
                ForeColor = AppTheme.TitleColor
            };
            txtQuickAdd.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtQuickAdd.Text))
                {
                    e.SuppressKeyPress = true;
                    await QuickAddTaskAsync(txtQuickAdd.Text.Trim());
                    txtQuickAdd.Clear();
                }
            };
            quickAddPanel.Controls.Add(txtQuickAdd);

            contentPanel.Controls.Add(taskListContainer);
            contentPanel.Controls.Add(headerPanel);
            contentPanel.Controls.Add(quickAddPanel);
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
            LayoutHeaderButtons();
            await FullRefreshAsync();
            syncTimer.Start();

            // Listen for Windows theme changes
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            base.OnFormClosed(e);
        }

        private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
            {
                if (AppTheme.IsCurrentlyDark() != AppTheme.IsDark)
                {
                    Application.Restart();
                    Environment.Exit(0);
                }
            }
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

                // Auto-select default list, or first list if none selected
                if (selectedListId < 0 && lists.Count > 0)
                {
                    if (settings.DefaultListId.HasValue &&
                        lists.Any(l => l.ListId == settings.DefaultListId.Value))
                    {
                        selectedListId = settings.DefaultListId.Value;
                    }
                    else
                    {
                        selectedListId = lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name).First().ListId;
                    }
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
                RemoteLogger.Error("MainForm", "FullRefresh failed", ex);
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
                RemoteLogger.Error("MainForm", "Sync failed", ex);
                UpdateStatus($"Sync error: {ex.Message}");
            }
        }

        private void LayoutHeaderButtons()
        {
            if (headerButtons == null || headerPanel == null)
            {
                return;
            }

            int gap = 4;
            int x = headerPanel.Width - headerPanel.Padding.Right;
            for (int i = headerButtons.Length - 1; i >= 0; i--)
            {
                x -= headerButtons[i].Width;
                headerButtons[i].Left = x;
                x -= gap;
            }
        }

        // ── UI Refresh ──────────────────────────────────────────

        private void RefreshSidebarLists()
        {
            BeginUpdate(listPanel);
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
                    IsItemSelected = isSelected,
                    IsDefault = settings.DefaultListId == list.ListId
                };

                row.Click += (s, e) =>
                {
                    long listId = (long)((Control)s!).Tag;
                    _ = OnListSelectedAsync(listId);
                };

                // Right-click context menu
                ContextMenuStrip menu = new ContextMenuStrip();
                AppTheme.StyleContextMenu(menu);

                ToolStripMenuItem renameItem = new ToolStripMenuItem("Rename");
                renameItem.Click += (s, e) =>
                {
                    long listId = (long)row.Tag;
                    _ = RenameListInlineAsync(listId);
                };
                menu.Items.Add(renameItem);

                ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
                deleteItem.Click += (s, e) =>
                {
                    long listId = (long)row.Tag;
                    _ = DeleteListInlineAsync(listId);
                };
                menu.Items.Add(deleteItem);

                menu.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem setDefaultItem = new ToolStripMenuItem("Set as Default");
                setDefaultItem.Checked = settings.DefaultListId == list.ListId;
                setDefaultItem.Click += (s, e) =>
                {
                    settings.DefaultListId = list.ListId;
                    settings.Save();
                    RefreshSidebarLists();
                };
                menu.Items.Add(setDefaultItem);

                row.ContextMenuStrip = menu;

                listPanel.Controls.Add(row);
            }

            listPanel.ResumeLayout(true);
            EndUpdate(listPanel);
        }

        private void RefreshTaskList()
        {
            BeginUpdate(taskListPanel);
            taskListPanel.SuspendLayout();
            taskListPanel.Controls.Clear();
            selectedTaskPanels.Clear();

            // Re-add drag indicator
            taskListPanel.Controls.Add(dragIndicator);

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
                        ClearSelection();
                        tp.IsSelected = true;
                        selectedTaskPanels.Add(tp);
                        await EditTaskAsync();
                    }
                };
                panel.Click += (s, e) =>
                {
                    TaskPanel tp = (TaskPanel)s!;
                    bool ctrlHeld = (ModifierKeys & Keys.Control) != 0;

                    if (ctrlHeld)
                    {
                        if (tp.IsSelected)
                        {
                            tp.IsSelected = false;
                            selectedTaskPanels.Remove(tp);
                        }
                        else
                        {
                            tp.IsSelected = true;
                            selectedTaskPanels.Add(tp);
                        }
                    }
                    else
                    {
                        ClearSelection();
                        tp.IsSelected = true;
                        selectedTaskPanels.Add(tp);
                    }
                };

                taskListPanel.Controls.Add(panel);
            }

            taskListPanel.ResumeLayout(true);
            EndUpdate(taskListPanel);
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
                RemoteLogger.Error("MainForm", "LoadTasks failed", ex);
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
                RemoteLogger.Error("MainForm", "ToggleComplete failed", ex);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        // ── Task CRUD ───────────────────────────────────────────

        private async Task QuickAddTaskAsync(string title)
        {
            if (selectedListId < 0)
            {
                UpdateStatus("Select a list first");
                return;
            }

            try
            {
                TodoItem newItem = await apiClient.CreateItemAsync(
                    selectedListId, title, null, null, null, 0);
                currentItems.Add(newItem);
                RefreshTaskList();
            }
            catch (Exception ex)
            {
                RemoteLogger.Error("MainForm", "CreateTask failed", ex);
                UpdateStatus($"Error creating task: {ex.Message}");
            }
        }

        private async Task AddTaskAsync()
        {
            if (selectedListId < 0)
            {
                AppTheme.ShowMessage(this, "Please select a list first.", "No List Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            using TaskEditForm form = new TaskEditForm(categories, lists, null);
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
                    RemoteLogger.Error("MainForm", "CreateTask failed", ex);
                UpdateStatus($"Error creating task: {ex.Message}");
                }
            }
        }

        private async Task EditTaskAsync()
        {
            if (selectedTaskPanels.Count != 1)
            {
                return;
            }

            TodoItem item = selectedTaskPanels[0].Item;

            using TaskEditForm form = new TaskEditForm(categories, lists, item);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    long? newListId = null;
                    if (form.SelectedListId.HasValue && form.SelectedListId.Value != item.ListId)
                    {
                        newListId = form.SelectedListId.Value;
                    }

                    TodoItem updated = await apiClient.UpdateItemAsync(
                        item.ItemId, form.TaskTitle, form.TaskNotes,
                        form.SelectedCategoryId, item.IsCompleted, form.TaskDueDate, item.SortOrder,
                        newListId);

                    if (newListId.HasValue)
                    {
                        // Task moved to another list — remove from current view
                        currentItems.Remove(item);
                    }
                    else
                    {
                        item.Title = updated.Title;
                        item.Notes = updated.Notes;
                        item.CategoryId = updated.CategoryId;
                        item.DueDate = updated.DueDate;
                        item.ModifiedDate = updated.ModifiedDate;
                    }
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    RemoteLogger.Error("MainForm", "UpdateTask failed", ex);
                UpdateStatus($"Error updating task: {ex.Message}");
                }
            }
        }

        private async Task DeleteTaskAsync()
        {
            List<TodoItem> items = selectedTaskPanels.Select(tp => tp.Item).ToList();
            if (items.Count == 0)
            {
                return;
            }

            string message = items.Count == 1
                ? $"Delete task \"{items[0].Title}\"?"
                : $"Delete {items.Count} selected tasks?";

            DialogResult result = AppTheme.ShowMessage(this, message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    foreach (TodoItem item in items)
                    {
                        await apiClient.DeleteItemAsync(item.ItemId);
                        currentItems.Remove(item);
                    }
                    RefreshTaskList();
                }
                catch (Exception ex)
                {
                    RemoteLogger.Error("MainForm", "DeleteTask failed", ex);
                UpdateStatus($"Error deleting task: {ex.Message}");
                }
            }
        }

        private void ClearSelection()
        {
            foreach (TaskPanel tp in selectedTaskPanels)
            {
                tp.IsSelected = false;
            }
            selectedTaskPanels.Clear();
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

        // ── Inline List Operations (right-click) ────────────────

        private async Task RenameListInlineAsync(long listId)
        {
            TodoList? list = lists.FirstOrDefault(l => l.ListId == listId);
            if (list == null)
            {
                return;
            }

            string? newName = ShowInputDialog("Rename List", "New name:", list.Name);
            if (newName == null || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            try
            {
                TodoList updated = await apiClient.UpdateListAsync(list.ListId, newName.Trim(), list.SortOrder);
                list.Name = updated.Name;
                RefreshSidebarLists();

                if (list.ListId == selectedListId)
                {
                    lblHeaderTitle.Text = list.Name;
                }
            }
            catch (Exception ex)
            {
                RemoteLogger.Error("MainForm", "RenameList failed", ex);
                UpdateStatus($"Error renaming list: {ex.Message}");
            }
        }

        private async Task DeleteListInlineAsync(long listId)
        {
            TodoList? list = lists.FirstOrDefault(l => l.ListId == listId);
            if (list == null)
            {
                return;
            }

            DialogResult result = AppTheme.ShowMessage(this,
                $"Delete list \"{list.Name}\" and all its tasks?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await apiClient.DeleteListAsync(list.ListId);
                    lists.Remove(list);
                    RefreshSidebarLists();

                    if (list.ListId == selectedListId)
                    {
                        selectedListId = -1;
                        currentItems.Clear();
                        RefreshTaskList();
                        lblHeaderTitle.Text = "Tasks";
                    }
                }
                catch (Exception ex)
                {
                    RemoteLogger.Error("MainForm", "DeleteList failed", ex);
                UpdateStatus($"Error deleting list: {ex.Message}");
                }
            }
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue)
        {
            using Form dialog = new Form
            {
                Text = title,
                Size = new Size(380, 160),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = AppTheme.ContentBg
            };

            Label lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(16, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TitleColor
            };

            TextBox txtInput = new TextBox
            {
                Text = defaultValue,
                Location = new Point(16, 42),
                Width = 330,
                Font = new Font("Segoe UI", 10f),
                BackColor = AppTheme.HeaderBg,
                ForeColor = AppTheme.TitleColor
            };

            AccentButton btnOk = new AccentButton
            {
                Text = "OK",
                Width = 80,
                Location = new Point(180, 80)
            };
            btnOk.Click += (s, e) => { dialog.DialogResult = DialogResult.OK; };

            FlatButton btnCancel = new FlatButton
            {
                Text = "Cancel",
                Width = 80,
                Location = new Point(266, 80)
            };
            btnCancel.Click += (s, e) => { dialog.DialogResult = DialogResult.Cancel; };

            dialog.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnOk, btnCancel });
            AppTheme.StyleForm(dialog);
            dialog.AcceptButton = null; // We handle Enter manually
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    dialog.DialogResult = DialogResult.OK;
                }
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                return txtInput.Text;
            }

            return null;
        }

        // ── Drag-and-Drop Reordering ────────────────────────────

        private void TaskListPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(TaskPanel)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void TaskListPanel_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(TaskPanel)) != true)
            {
                return;
            }

            e.Effect = DragDropEffects.Move;
            Point clientPoint = taskListPanel.PointToClient(new Point(e.X, e.Y));
            int dropIndex = GetDropIndex(clientPoint);

            // Position the drag indicator
            int indicatorY = GetIndicatorY(dropIndex);
            dragIndicator.SetBounds(8, indicatorY, taskListPanel.ClientSize.Width - 16, 3);
            dragIndicator.Visible = true;
            dragIndicator.BringToFront();
        }

        private void TaskListPanel_DragLeave(object? sender, EventArgs e)
        {
            dragIndicator.Visible = false;
        }

        private void TaskListPanel_DragDrop(object? sender, DragEventArgs e)
        {
            dragIndicator.Visible = false;

            if (e.Data?.GetData(typeof(TaskPanel)) is not TaskPanel draggedPanel)
            {
                return;
            }

            Point clientPoint = taskListPanel.PointToClient(new Point(e.X, e.Y));
            int dropIndex = GetDropIndex(clientPoint);

            // Build the visual order of task panels (top to bottom)
            List<TaskPanel> panels = GetTaskPanelsInOrder();
            int currentIndex = panels.IndexOf(draggedPanel);
            if (currentIndex < 0 || currentIndex == dropIndex)
            {
                return;
            }

            // Move the item in the ordered list
            panels.RemoveAt(currentIndex);
            if (dropIndex > currentIndex)
            {
                dropIndex--;
            }
            if (dropIndex > panels.Count)
            {
                dropIndex = panels.Count;
            }
            panels.Insert(dropIndex, draggedPanel);

            // Assign new sort orders and update in-memory items
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].Item.SortOrder = i;
            }

            // Persist to server
            _ = SaveReorderAsync();

            // Refresh the display
            RefreshTaskList();
        }

        private List<TaskPanel> GetTaskPanelsInOrder()
        {
            // Controls are docked Top in reverse add order, so iterate and collect TaskPanels
            List<TaskPanel> panels = new();
            foreach (Control c in taskListPanel.Controls)
            {
                if (c is TaskPanel tp)
                {
                    panels.Add(tp);
                }
            }
            // With Dock.Top, the first control in Controls collection is the visual bottom.
            // The visual top-to-bottom order is the reverse of the Controls collection.
            panels.Reverse();
            return panels;
        }

        private int GetDropIndex(Point clientPoint)
        {
            List<TaskPanel> panels = GetTaskPanelsInOrder();
            int y = clientPoint.Y + taskListPanel.VerticalScroll.Value;

            for (int i = 0; i < panels.Count; i++)
            {
                int panelMid = panels[i].Top + (panels[i].Height / 2);
                if (y < panelMid)
                {
                    return i;
                }
            }

            return panels.Count;
        }

        private int GetIndicatorY(int dropIndex)
        {
            List<TaskPanel> panels = GetTaskPanelsInOrder();

            if (panels.Count == 0)
            {
                return 0;
            }

            if (dropIndex >= panels.Count)
            {
                return panels[panels.Count - 1].Bottom;
            }

            return panels[dropIndex].Top;
        }

        private async Task SaveReorderAsync()
        {
            if (selectedListId < 0)
            {
                return;
            }

            try
            {
                List<(long ItemId, int SortOrder)> entries = currentItems
                    .Select(i => (i.ItemId, i.SortOrder))
                    .ToList();
                await apiClient.ReorderItemsAsync(selectedListId, entries);
            }
            catch (Exception ex)
            {
                RemoteLogger.Error("MainForm", "Reorder failed", ex);
                UpdateStatus($"Error saving order: {ex.Message}");
            }
        }
    }
}
