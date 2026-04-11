public class MainWindow : Adw.ApplicationWindow {
    private ApiClient api_client;

    // UI widgets
    private ListSidebar list_sidebar;
    private Gtk.ListView task_list_view;
    private Gtk.SingleSelection task_selection;
    private GLib.ListStore task_store;
    private Adw.NavigationSplitView split_view;
    private Gtk.Label status_label;
    private Adw.HeaderBar content_header;

    // State
    private GenericArray<TodoList> lists = new GenericArray<TodoList> ();
    private GenericArray<Category> categories = new GenericArray<Category> ();
    private GenericArray<TodoItem> current_items = new GenericArray<TodoItem> ();
    private int64 selected_list_id = -1;
    private uint sync_timer_id = 0;

    public MainWindow (Gtk.Application app) {
        Object (
            application: app,
            title: "ToDoList",
            default_width: 900,
            default_height: 600
        );
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

        var sidebar_page = new Adw.NavigationPage.with_tag ("sidebar", "Lists");
        sidebar_page.child = list_sidebar;
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

        // Menu button
        var menu_model = new Menu ();
        menu_model.append ("Manage Categories", "win.manage-categories");
        menu_model.append ("Refresh", "win.refresh");

        var menu_button = new Gtk.MenuButton ();
        menu_button.icon_name = "open-menu-symbolic";
        menu_button.menu_model = menu_model;
        content_header.pack_end (menu_button);

        content_box.append (content_header);

        // Task list
        task_store = new GLib.ListStore (typeof (TodoItem));
        task_selection = new Gtk.SingleSelection (task_store);

        var factory = new Gtk.SignalListItemFactory ();
        factory.setup.connect ((item) => {
            var row = new TaskRow ();
            item.child = row;
        });
        factory.bind.connect ((item) => {
            var row = (TaskRow) item.child;
            var todo_item = (TodoItem) item.item;
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

        // Status bar
        status_label = new Gtk.Label ("Connecting...");
        status_label.halign = Gtk.Align.START;
        status_label.margin_start = 8;
        status_label.margin_end = 8;
        status_label.margin_top = 4;
        status_label.margin_bottom = 4;
        status_label.add_css_class ("dim-label");
        content_box.append (status_label);

        var content_page = new Adw.NavigationPage.with_tag ("content", "Tasks");
        content_page.child = content_box;
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
    }

    // ── Data Loading ────────────────────────────────────

    private async void load_initial_data () {
        yield full_refresh ();
    }

    private async void full_refresh () {
        try {
            api_client.reset_sync_time ();
            lists = yield api_client.get_lists_async ();
            categories = yield api_client.get_categories_async ();
            list_sidebar.update_lists (lists);

            if (selected_list_id > 0) {
                current_items = yield api_client.get_items_by_list_async (selected_list_id);
                refresh_task_list ();
            }

            update_status ("Connected");
        } catch (Error e) {
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
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        current_items.add (item);
                    }
                }
                refresh_task_list ();
            }

            list_sidebar.update_lists (lists);
            update_status ("Synced at %s".printf (new DateTime.now_local ().format ("%H:%M:%S")));
        } catch (Error e) {
            update_status ("Sync error: %s".printf (e.message));
        }
    }

    // ── UI Refresh ──────────────────────────────────────

    private void refresh_task_list () {
        task_store.remove_all ();
        for (uint i = 0; i < current_items.length; i++) {
            task_store.append (current_items[i]);
        }
    }

    private void update_status (string message) {
        status_label.label = "%s  |  %s".printf (TodoApp.server_url, message);
    }

    // ── Sync Timer ──────────────────────────────────────

    private void start_sync_timer () {
        sync_timer_id = GLib.Timeout.add_seconds (30, () => {
            sync.begin ();
            return Source.CONTINUE;
        });
    }

    // ── Event Handlers ──────────────────────────────────

    private void on_list_selected (int64 list_id) {
        selected_list_id = list_id;
        load_list_items.begin ();
    }

    private async void load_list_items () {
        try {
            current_items = yield api_client.get_items_by_list_async (selected_list_id);
            refresh_task_list ();

            // Update header title
            for (uint i = 0; i < lists.length; i++) {
                if (lists[i].list_id == selected_list_id) {
                    ((Adw.WindowTitle) content_header.title_widget).title = lists[i].name;
                    break;
                }
            }
        } catch (Error e) {
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
            } catch (Error e) {
                update_status ("Error: %s".printf (e.message));
            }
        }
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
                update_status ("Error: %s".printf (e.message));
            }
        }
    }

    private void on_task_activated (uint position) {
        var item = (TodoItem) task_store.get_item (position);
        show_edit_task_dialog.begin (item);
    }

    private async void show_edit_task_dialog (TodoItem item) {
        var dialog = new TaskEditDialog (item, categories);
        var response = yield dialog.choose (this, null);
        if (response == "save") {
            try {
                var updated = yield api_client.update_item_async (
                    item.item_id,
                    dialog.task_title,
                    dialog.task_notes,
                    dialog.selected_category_id,
                    dialog.has_selected_category,
                    item.is_completed,
                    dialog.task_due_date,
                    item.sort_order);
                item.title = updated.title;
                item.notes = updated.notes;
                item.category_id = updated.category_id;
                item.has_category = updated.has_category;
                item.due_date = updated.due_date;
                refresh_task_list ();
            } catch (Error e) {
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
            update_status ("Error: %s".printf (e.message));
        }
    }
}
