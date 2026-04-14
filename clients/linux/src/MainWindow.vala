public class MainWindow : Adw.ApplicationWindow {
    private ApiClient api_client;
    private AppSettings settings;

    // UI widgets
    private ListSidebar list_sidebar;
    private Gtk.ListView task_list_view;
    private Gtk.MultiSelection task_selection;
    private GLib.ListStore task_store;
    private Adw.NavigationSplitView split_view;
    private Gtk.Label status_label;
    private Adw.HeaderBar content_header;
    private Gtk.Button delete_button;
    private Gtk.Entry quick_add_entry;

    // State
    private GenericArray<TodoList> lists = new GenericArray<TodoList> ();
    private GenericArray<Category> categories = new GenericArray<Category> ();
    private GenericArray<TodoItem> current_items = new GenericArray<TodoItem> ();
    private int64 selected_list_id = -1;
    private uint sync_timer_id = 0;

    // Drag-and-drop state
    private int drag_source_index = -1;

    public MainWindow (Gtk.Application app, AppSettings settings) {
        Object (
            application: app,
            title: "YepList",
            default_width: 900,
            default_height: 600
        );
        this.settings = settings;
    }

    construct {
        api_client = new ApiClient (TodoApp.server_url);
        build_ui ();
        load_initial_data.begin ();
        start_sync_timer ();
    }

    private void build_ui () {
        // Split view: sidebar + content
        split_view = new Adw.NavigationSplitView ();

        // ── Sidebar ─────────────────────────────────
        list_sidebar = new ListSidebar ();
        list_sidebar.list_selected.connect (on_list_selected);
        list_sidebar.new_list_requested.connect (on_new_list_requested);
        list_sidebar.rename_list_requested.connect (on_rename_list_requested);
        list_sidebar.delete_list_requested.connect (on_delete_list_requested);
        list_sidebar.set_default_requested.connect (on_set_default_requested);

        var sidebar_page = new Adw.NavigationPage.with_tag (list_sidebar, "Lists", "sidebar");
        split_view.sidebar = sidebar_page;

        // ── Content ─────────────────────────────────
        var content_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 0);

        content_header = new Adw.HeaderBar ();
        content_header.title_widget = new Adw.WindowTitle ("Tasks", "");

        // Add Task button
        var add_button = new Gtk.Button.with_label ("Add Task");
        add_button.add_css_class ("suggested-action");
        add_button.clicked.connect (on_add_task_clicked);
        content_header.pack_start (add_button);

        // Delete button (for multi-select)
        delete_button = new Gtk.Button.from_icon_name ("user-trash-symbolic");
        delete_button.tooltip_text = "Delete Selected Tasks";
        delete_button.add_css_class ("destructive-action");
        delete_button.sensitive = false;
        delete_button.clicked.connect (() => { delete_selected_tasks.begin (); });
        content_header.pack_start (delete_button);

        // Menu button
        var menu_model = new Menu ();
        menu_model.append ("Manage Categories", "win.manage-categories");
        menu_model.append ("Settings", "win.settings");
        menu_model.append ("Refresh", "win.refresh");
        menu_model.append ("About", "win.about");

        var menu_button = new Gtk.MenuButton ();
        menu_button.icon_name = "open-menu-symbolic";
        menu_button.menu_model = menu_model;
        content_header.pack_end (menu_button);

        content_box.append (content_header);

        // Task list with multi-selection
        task_store = new GLib.ListStore (typeof (TodoItem));
        task_selection = new Gtk.MultiSelection (task_store);
        task_selection.selection_changed.connect (on_task_selection_changed);

        var factory = new Gtk.SignalListItemFactory ();
        factory.setup.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var row = new TaskRow ();
            list_item.child = row;

            // Drag source
            var drag_source = new Gtk.DragSource ();
            drag_source.actions = Gdk.DragAction.MOVE;
            drag_source.prepare.connect ((source, x, y) => {
                var task_row = (TaskRow) source.get_widget ();
                var bound = task_row.get_bound_item ();
                if (bound == null) {
                    return null;
                }
                drag_source_index = find_item_index (bound.item_id);
                if (drag_source_index < 0) {
                    return null;
                }
                var val = Value (typeof (int));
                val.set_int (drag_source_index);
                return new Gdk.ContentProvider.for_value (val);
            });
            row.add_controller (drag_source);

            // Drop target
            var drop_target = new Gtk.DropTarget (typeof (int), Gdk.DragAction.MOVE);
            drop_target.drop.connect ((target, val, x, y) => {
                int source_idx = val.get_int ();
                var drop_row = (TaskRow) target.get_widget ();
                var bound = drop_row.get_bound_item ();
                if (bound == null) {
                    return false;
                }
                int dest_idx = find_item_index (bound.item_id);
                if (source_idx < 0 || dest_idx < 0 || source_idx == dest_idx) {
                    return false;
                }
                reorder_item (source_idx, dest_idx);
                return true;
            });
            row.add_controller (drop_target);
        });
        factory.bind.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var row = (TaskRow) list_item.child;
            var todo_item = (TodoItem) list_item.item;
            row.bind_item (todo_item, categories);
            row.completion_toggled.connect ((toggled_item) => {
                toggle_complete.begin (toggled_item);
            });
        });

        task_list_view = new Gtk.ListView (task_selection, factory);
        task_list_view.vexpand = true;
        task_list_view.activate.connect (on_task_activated);

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = task_list_view;
        scrolled.vexpand = true;
        content_box.append (scrolled);

        // Quick-add entry
        quick_add_entry = new Gtk.Entry ();
        quick_add_entry.placeholder_text = "Quick add task\u2026 (press Enter)";
        quick_add_entry.margin_start = 8;
        quick_add_entry.margin_end = 8;
        quick_add_entry.margin_top = 6;
        quick_add_entry.margin_bottom = 6;
        quick_add_entry.activate.connect (on_quick_add);
        content_box.append (quick_add_entry);

        // Status bar
        status_label = new Gtk.Label ("Connecting...");
        status_label.halign = Gtk.Align.START;
        status_label.margin_start = 8;
        status_label.margin_end = 8;
        status_label.margin_top = 4;
        status_label.margin_bottom = 4;
        status_label.add_css_class ("dim-label");
        content_box.append (status_label);

        var content_page = new Adw.NavigationPage.with_tag (content_box, "Tasks", "content");
        split_view.content = content_page;

        this.content = split_view;

        // Actions
        setup_actions ();
    }

    private void setup_actions () {
        var categories_action = new SimpleAction ("manage-categories", null);
        categories_action.activate.connect (() => {
            show_category_manager.begin ();
        });
        add_action (categories_action);

        var refresh_action = new SimpleAction ("refresh", null);
        refresh_action.activate.connect (() => {
            full_refresh.begin ();
        });
        add_action (refresh_action);

        var settings_action = new SimpleAction ("settings", null);
        settings_action.activate.connect (() => {
            var dialog = new SettingsDialog (settings);
            dialog.present (this);
        });
        add_action (settings_action);

        var about_action = new SimpleAction ("about", null);
        about_action.activate.connect (() => {
            var dialog = new AboutDialog ();
            dialog.present (this);
        });
        add_action (about_action);
    }

    // ── Data Loading ────────────────────────────────────

    private async void load_initial_data () {
        yield full_refresh ();

        // Apply default list setting
        if (settings != null && settings.default_list_id > 0) {
            list_sidebar.set_default_list_id (settings.default_list_id);
        }
    }

    private async void full_refresh () {
        try {
            api_client.reset_sync_time ();
            lists = yield api_client.get_lists_async ();
            categories = yield api_client.get_categories_async ();
            list_sidebar.update_lists (lists);

            if (settings != null && settings.default_list_id > 0) {
                list_sidebar.set_default_list_id (settings.default_list_id);
            }

            // Auto-select default list or first list
            if (selected_list_id < 0 && lists.length > 0) {
                bool found_default = false;
                if (settings != null && settings.default_list_id > 0) {
                    for (uint i = 0; i < lists.length; i++) {
                        if (lists[i].list_id == settings.default_list_id) {
                            selected_list_id = lists[i].list_id;
                            list_sidebar.select_list_id (selected_list_id);
                            found_default = true;
                            break;
                        }
                    }
                }
                if (!found_default) {
                    selected_list_id = lists[0].list_id;
                    list_sidebar.select_index (0);
                }
            }

            if (selected_list_id > 0) {
                current_items = yield api_client.get_items_by_list_async (selected_list_id);
                sort_items_by_order ();
                refresh_task_list ();

                // Update header title
                for (uint i = 0; i < lists.length; i++) {
                    if (lists[i].list_id == selected_list_id) {
                        ((Adw.WindowTitle) content_header.title_widget).title = lists[i].name;
                        break;
                    }
                }
            }

            update_status ("Connected");
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "FullRefresh failed", e);
            update_status ("Error: %s".printf (e.message));
        }
    }

    private async void sync () {
        try {
            var result = yield api_client.sync_async ();

            // Apply list changes
            for (uint i = 0; i < result.lists.length; i++) {
                var list = result.lists[i];
                bool found = false;
                for (uint j = 0; j < lists.length; j++) {
                    if (lists[j].list_id == list.list_id) {
                        lists[j].name = list.name;
                        lists[j].sort_order = list.sort_order;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    lists.add (list);
                }
            }

            // Apply category changes
            for (uint i = 0; i < result.categories.length; i++) {
                var cat = result.categories[i];
                bool found = false;
                for (uint j = 0; j < categories.length; j++) {
                    if (categories[j].category_id == cat.category_id) {
                        categories[j].name = cat.name;
                        categories[j].color = cat.color;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    categories.add (cat);
                }
            }

            // Apply item changes for current list
            if (selected_list_id > 0) {
                for (uint i = 0; i < result.items.length; i++) {
                    var item = result.items[i];
                    if (item.list_id != selected_list_id) {
                        continue;
                    }
                    bool found = false;
                    for (uint j = 0; j < current_items.length; j++) {
                        if (current_items[j].item_id == item.item_id) {
                            current_items[j].title = item.title;
                            current_items[j].notes = item.notes;
                            current_items[j].category_id = item.category_id;
                            current_items[j].has_category = item.has_category;
                            current_items[j].is_completed = item.is_completed;
                            current_items[j].due_date = item.due_date;
                            current_items[j].sort_order = item.sort_order;
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        current_items.add (item);
                    }
                }
                sort_items_by_order ();
                refresh_task_list ();
            }

            list_sidebar.update_lists (lists);
            if (settings != null && settings.default_list_id > 0) {
                list_sidebar.set_default_list_id (settings.default_list_id);
            }
            update_status ("Synced at %s".printf (new DateTime.now_local ().format ("%H:%M:%S")));
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "Sync failed", e);
            update_status ("Sync error: %s".printf (e.message));
        }
    }

    // ── UI Refresh ──────────────────────────────────────

    private void refresh_task_list () {
        task_store.remove_all ();
        for (uint i = 0; i < current_items.length; i++) {
            task_store.append (current_items[i]);
        }
        update_delete_button ();
    }

    private void update_status (string message) {
        status_label.label = "%s  |  %s".printf (TodoApp.server_url, message);
    }

    private void update_delete_button () {
        var bitset = task_selection.get_selection ();
        delete_button.sensitive = !bitset.is_empty ();
    }

    private void sort_items_by_order () {
        // Simple insertion sort by sort_order
        for (uint i = 1; i < current_items.length; i++) {
            var item = current_items[i];
            int j = (int) i - 1;
            while (j >= 0 && current_items[j].sort_order > item.sort_order) {
                current_items[j + 1] = current_items[j];
                j--;
            }
            current_items[j + 1] = item;
        }
    }

    // ── Sync Timer ──────────────────────────────────────

    private void start_sync_timer () {
        sync_timer_id = GLib.Timeout.add_seconds (30, () => {
            sync.begin ();
            return Source.CONTINUE;
        });
    }

    // ── Drag-and-Drop Reorder ───────────────────────────

    private int find_item_index (int64 item_id) {
        for (uint i = 0; i < current_items.length; i++) {
            if (current_items[i].item_id == item_id) {
                return (int) i;
            }
        }
        return -1;
    }

    private void reorder_item (int source_idx, int dest_idx) {
        if (source_idx < 0 || source_idx >= (int) current_items.length) {
            return;
        }
        if (dest_idx < 0 || dest_idx >= (int) current_items.length) {
            return;
        }

        var item = current_items[source_idx];

        // Remove from old position
        current_items.remove_index (source_idx);

        // Insert at new position
        // After removal, adjust dest_idx if needed
        if (dest_idx > source_idx) {
            dest_idx--;
        }
        current_items.insert (dest_idx, item);

        // Update sort_order values
        for (uint i = 0; i < current_items.length; i++) {
            current_items[i].sort_order = (int) i;
        }

        refresh_task_list ();
        save_reorder.begin ();
    }

    private async void save_reorder () {
        if (selected_list_id < 0) {
            return;
        }
        try {
            yield api_client.reorder_items_async (selected_list_id, current_items);
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "Reorder failed", e);
            update_status ("Reorder error: %s".printf (e.message));
        }
    }

    // ── Event Handlers ──────────────────────────────────

    private void on_list_selected (int64 list_id) {
        selected_list_id = list_id;
        load_list_items.begin ();
    }

    private async void load_list_items () {
        try {
            current_items = yield api_client.get_items_by_list_async (selected_list_id);
            sort_items_by_order ();
            refresh_task_list ();

            // Update header title
            for (uint i = 0; i < lists.length; i++) {
                if (lists[i].list_id == selected_list_id) {
                    ((Adw.WindowTitle) content_header.title_widget).title = lists[i].name;
                    break;
                }
            }
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "LoadItems failed", e);
            update_status ("Error: %s".printf (e.message));
        }
    }

    private void on_new_list_requested () {
        show_new_list_dialog.begin ();
    }

    private async void show_new_list_dialog () {
        var dialog = new Adw.AlertDialog ("New List", "Enter a name for the new list:");
        dialog.add_response ("cancel", "Cancel");
        dialog.add_response ("create", "Create");
        dialog.default_response = "create";
        dialog.set_response_appearance ("create", Adw.ResponseAppearance.SUGGESTED);

        var entry = new Gtk.Entry ();
        entry.placeholder_text = "List name";
        entry.margin_start = 16;
        entry.margin_end = 16;
        dialog.extra_child = entry;

        var response = yield dialog.choose (this, null);
        if (response == "create" && entry.text.strip () != "") {
            try {
                var new_list = yield api_client.create_list_async (entry.text.strip ());
                lists.add (new_list);
                list_sidebar.update_lists (lists);
                if (settings != null && settings.default_list_id > 0) {
                    list_sidebar.set_default_list_id (settings.default_list_id);
                }
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "CreateList failed", e);
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private void on_rename_list_requested (int64 list_id, string current_name) {
        show_rename_list_dialog.begin (list_id, current_name);
    }

    private async void show_rename_list_dialog (int64 list_id, string current_name) {
        var dialog = new Adw.AlertDialog ("Rename List", "Enter a new name:");
        dialog.add_response ("cancel", "Cancel");
        dialog.add_response ("rename", "Rename");
        dialog.default_response = "rename";
        dialog.set_response_appearance ("rename", Adw.ResponseAppearance.SUGGESTED);

        var entry = new Gtk.Entry ();
        entry.text = current_name;
        entry.margin_start = 16;
        entry.margin_end = 16;
        dialog.extra_child = entry;

        var response = yield dialog.choose (this, null);
        if (response == "rename" && entry.text.strip () != "") {
            try {
                // Find the list to get its sort_order
                int sort_order = 0;
                for (uint i = 0; i < lists.length; i++) {
                    if (lists[i].list_id == list_id) {
                        sort_order = lists[i].sort_order;
                        break;
                    }
                }
                var updated = yield api_client.update_list_async (list_id, entry.text.strip (), sort_order);
                for (uint i = 0; i < lists.length; i++) {
                    if (lists[i].list_id == list_id) {
                        lists[i].name = updated.name;
                        break;
                    }
                }
                list_sidebar.update_lists (lists);
                if (settings != null && settings.default_list_id > 0) {
                    list_sidebar.set_default_list_id (settings.default_list_id);
                }

                // Update header if this is the selected list
                if (list_id == selected_list_id) {
                    ((Adw.WindowTitle) content_header.title_widget).title = updated.name;
                }
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "RenameList failed", e);
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private void on_delete_list_requested (int64 list_id, string name) {
        show_delete_list_dialog.begin (list_id, name);
    }

    private async void show_delete_list_dialog (int64 list_id, string name) {
        var dialog = new Adw.AlertDialog ("Delete List",
            "Delete list \"%s\" and all its tasks?".printf (name));
        dialog.add_response ("cancel", "Cancel");
        dialog.add_response ("delete", "Delete");
        dialog.set_response_appearance ("delete", Adw.ResponseAppearance.DESTRUCTIVE);

        var response = yield dialog.choose (this, null);
        if (response == "delete") {
            try {
                yield api_client.delete_list_async (list_id);
                for (uint i = 0; i < lists.length; i++) {
                    if (lists[i].list_id == list_id) {
                        lists.remove_index (i);
                        break;
                    }
                }
                list_sidebar.update_lists (lists);
                if (settings != null && settings.default_list_id > 0) {
                    list_sidebar.set_default_list_id (settings.default_list_id);
                }

                // If deleted list was selected, select first available
                if (list_id == selected_list_id) {
                    if (lists.length > 0) {
                        selected_list_id = lists[0].list_id;
                        list_sidebar.select_index (0);
                    } else {
                        selected_list_id = -1;
                        current_items = new GenericArray<TodoItem> ();
                        refresh_task_list ();
                        ((Adw.WindowTitle) content_header.title_widget).title = "Tasks";
                    }
                }

                // Clear default if the deleted list was the default
                if (settings != null && settings.default_list_id == list_id) {
                    settings.default_list_id = -1;
                    settings.save ();
                    list_sidebar.set_default_list_id (-1);
                }
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "DeleteList failed", e);
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private void on_set_default_requested (int64 list_id) {
        if (settings == null) {
            return;
        }

        // Toggle: if already default, clear it
        if (settings.default_list_id == list_id) {
            settings.default_list_id = -1;
        } else {
            settings.default_list_id = list_id;
        }
        settings.save ();
        list_sidebar.set_default_list_id (settings.default_list_id);
    }

    private void on_task_selection_changed (uint position, uint n_items) {
        update_delete_button ();
    }

    private void on_add_task_clicked () {
        if (selected_list_id < 0) {
            return;
        }
        show_add_task_dialog.begin ();
    }

    private async void show_add_task_dialog () {
        var dialog = new TaskEditDialog (null, categories);
        var response = yield dialog.choose (this, null);
        if (response == "save") {
            try {
                var new_item = yield api_client.create_item_async (
                    selected_list_id,
                    dialog.task_title,
                    dialog.task_notes,
                    dialog.selected_category_id,
                    dialog.has_selected_category,
                    dialog.task_due_date);
                current_items.add (new_item);
                refresh_task_list ();
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "CreateTask failed", e);
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private void on_task_activated (uint position) {
        var item = (TodoItem) task_store.get_item (position);
        show_edit_task_dialog.begin (item);
    }

    private async void show_edit_task_dialog (TodoItem item) {
        var dialog = new TaskEditDialog (item, categories, lists);
        var response = yield dialog.choose (this, null);
        if (response == "save") {
            try {
                int64 new_list_id = -1;
                if (dialog.selected_list_id > 0 && dialog.selected_list_id != item.list_id) {
                    new_list_id = dialog.selected_list_id;
                }

                var updated = yield api_client.update_item_async (
                    item.item_id,
                    dialog.task_title,
                    dialog.task_notes,
                    dialog.selected_category_id,
                    dialog.has_selected_category,
                    item.is_completed,
                    dialog.task_due_date,
                    item.sort_order,
                    new_list_id);

                if (new_list_id > 0) {
                    // Task moved to another list — remove from current view
                    for (uint i = 0; i < current_items.length; i++) {
                        if (current_items[i].item_id == item.item_id) {
                            current_items.remove_index (i);
                            break;
                        }
                    }
                } else {
                    item.title = updated.title;
                    item.notes = updated.notes;
                    item.category_id = updated.category_id;
                    item.has_category = updated.has_category;
                    item.due_date = updated.due_date;
                }
                refresh_task_list ();
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "EditTask failed", e);
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private async void toggle_complete (TodoItem item) {
        try {
            var updated = yield api_client.toggle_complete_async (item.item_id, !item.is_completed);
            item.is_completed = updated.is_completed;
            refresh_task_list ();
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "ToggleComplete failed", e);
            update_status ("Error: %s".printf (e.message));
        }
    }

    private async void delete_selected_tasks () {
        var bitset = task_selection.get_selection ();
        if (bitset.is_empty ()) {
            return;
        }

        uint64 count = bitset.get_size ();
        string message = count == 1
            ? "Delete this task?"
            : "Delete %llu selected tasks?".printf (count);

        var dialog = new Adw.AlertDialog ("Delete Tasks", message);
        dialog.add_response ("cancel", "Cancel");
        dialog.add_response ("delete", "Delete");
        dialog.set_response_appearance ("delete", Adw.ResponseAppearance.DESTRUCTIVE);

        var response = yield dialog.choose (this, null);
        if (response != "delete") {
            return;
        }

        // Collect item IDs to delete
        var to_delete = new GenericArray<int64?> ();
        var iter = Gtk.BitsetIter ();
        uint pos;
        if (iter.init_first (bitset, out pos)) {
            do {
                var item = (TodoItem) task_store.get_item (pos);
                to_delete.add (item.item_id);
            } while (iter.next (out pos));
        }

        // Delete each
        for (uint i = 0; i < to_delete.length; i++) {
            try {
                yield api_client.delete_item_async (to_delete[i]);
            } catch (Error e) {
                RemoteLogger.error ("MainWindow", "Delete failed", e);
                update_status ("Delete error: %s".printf (e.message));
            }
        }

        // Remove from local list
        for (uint i = 0; i < to_delete.length; i++) {
            for (uint j = 0; j < current_items.length; j++) {
                if (current_items[j].item_id == to_delete[i]) {
                    current_items.remove_index (j);
                    break;
                }
            }
        }
        refresh_task_list ();
    }

    private void on_quick_add () {
        string title = quick_add_entry.text.strip ();
        if (title == "" || selected_list_id < 0) {
            return;
        }
        quick_add_entry.text = "";
        quick_add_task.begin (title);
    }

    private async void quick_add_task (string title) {
        try {
            var new_item = yield api_client.create_item_async (
                selected_list_id, title, null, 0, false, null);
            current_items.add (new_item);
            refresh_task_list ();
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "QuickAdd failed", e);
            update_status ("Error: %s".printf (e.message));
        }
    }

    private async void show_category_manager () {
        var dialog = new CategoryManagerDialog (api_client, categories);
        yield dialog.choose (this, null);
        // Refresh categories after dialog closes
        try {
            categories = yield api_client.get_categories_async ();
            refresh_task_list ();
        } catch (Error e) {
            RemoteLogger.error ("MainWindow", "RefreshCategories failed", e);
            update_status ("Error: %s".printf (e.message));
        }
    }
}
