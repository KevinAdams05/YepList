public class ListSidebar : Gtk.Box {
    private Gtk.ListView list_view;
    private Gtk.SingleSelection selection;
    private GLib.ListStore list_store;
    private Gtk.Button add_button;
    private Gtk.PopoverMenu context_menu;

    private int64 default_list_id = -1;
    private int64 context_list_id = -1;

    public signal void list_selected (int64 list_id);
    public signal void new_list_requested ();
    public signal void rename_list_requested (int64 list_id, string current_name);
    public signal void delete_list_requested (int64 list_id, string name);
    public signal void set_default_requested (int64 list_id);

    public ListSidebar () {
        Object (
            orientation: Gtk.Orientation.VERTICAL,
            spacing: 0
        );

        build_ui ();
    }

    private void build_ui () {
        var header = new Adw.HeaderBar ();
        header.title_widget = new Adw.WindowTitle ("", "");
        append (header);

        // Logo
        var logo_image = new Gtk.Picture ();
        logo_image.set_size_request (160, 48);
        logo_image.content_fit = Gtk.ContentFit.CONTAIN;
        logo_image.margin_top = 4;
        logo_image.margin_bottom = 8;
        logo_image.halign = Gtk.Align.CENTER;

        string logo_path = get_logo_path ();
        var logo_file = File.new_for_path (logo_path);
        if (logo_file.query_exists ()) {
            logo_image.set_filename (logo_path);
        }

        var style_manager = Adw.StyleManager.get_default ();
        style_manager.notify["dark"].connect (() => {
            string new_path = get_logo_path ();
            var new_file = File.new_for_path (new_path);
            if (new_file.query_exists ()) {
                logo_image.set_filename (new_path);
            }
        });

        append (logo_image);

        // List store and selection
        list_store = new GLib.ListStore (typeof (TodoList));
        selection = new Gtk.SingleSelection (list_store);
        selection.selection_changed.connect (on_selection_changed);

        // Factory
        var factory = new Gtk.SignalListItemFactory ();
        factory.setup.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 6);
            box.margin_start = 12;
            box.margin_end = 12;
            box.margin_top = 8;
            box.margin_bottom = 8;

            var label = new Gtk.Label ("");
            label.halign = Gtk.Align.START;
            label.hexpand = true;
            box.append (label);

            var default_icon = new Gtk.Image.from_icon_name ("starred-symbolic");
            default_icon.visible = false;
            default_icon.add_css_class ("dim-label");
            box.append (default_icon);

            list_item.child = box;
        });
        factory.bind.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var box = (Gtk.Box) list_item.child;
            var todo_list = (TodoList) list_item.item;

            var label = (Gtk.Label) box.get_first_child ();
            label.label = todo_list.name;

            var icon = (Gtk.Image) label.get_next_sibling ();
            icon.visible = (todo_list.list_id == default_list_id);
        });

        list_view = new Gtk.ListView (selection, factory);
        list_view.vexpand = true;

        // Right-click gesture
        var right_click = new Gtk.GestureClick ();
        right_click.button = 3;
        right_click.pressed.connect (on_right_click);
        list_view.add_controller (right_click);

        // Build context menu
        build_context_menu ();

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = list_view;
        scrolled.vexpand = true;
        append (scrolled);

        // Add button
        add_button = new Gtk.Button.with_label ("New List");
        add_button.margin_start = 8;
        add_button.margin_end = 8;
        add_button.margin_top = 8;
        add_button.margin_bottom = 8;
        add_button.clicked.connect (() => {
            new_list_requested ();
        });
        append (add_button);
    }

    private void build_context_menu () {
        var menu_model = new Menu ();
        menu_model.append ("Rename", "sidebar.rename-list");
        menu_model.append ("Set as Default", "sidebar.set-default");
        menu_model.append ("Delete", "sidebar.delete-list");

        context_menu = new Gtk.PopoverMenu.from_model (menu_model);
        context_menu.has_arrow = false;
        context_menu.set_parent (list_view);

        // Actions
        var action_group = new SimpleActionGroup ();

        var rename_action = new SimpleAction ("rename-list", null);
        rename_action.activate.connect (() => {
            if (context_list_id > 0) {
                string name = get_list_name (context_list_id);
                rename_list_requested (context_list_id, name);
            }
        });
        action_group.add_action (rename_action);

        var default_action = new SimpleAction ("set-default", null);
        default_action.activate.connect (() => {
            if (context_list_id > 0) {
                set_default_requested (context_list_id);
            }
        });
        action_group.add_action (default_action);

        var delete_action = new SimpleAction ("delete-list", null);
        delete_action.activate.connect (() => {
            if (context_list_id > 0) {
                string name = get_list_name (context_list_id);
                delete_list_requested (context_list_id, name);
            }
        });
        action_group.add_action (delete_action);

        list_view.insert_action_group ("sidebar", action_group);
    }

    private void on_right_click (int n_press, double x, double y) {
        // Find which item was clicked by checking the position
        // Pick the item at the click point
        uint pos = find_item_at_y (y);
        if (pos == Gtk.INVALID_LIST_POSITION) {
            return;
        }

        var todo_list = (TodoList) list_store.get_item (pos);
        context_list_id = todo_list.list_id;

        // Position and show the popover
        Gdk.Rectangle rect = { (int) x, (int) y, 1, 1 };
        context_menu.pointing_to = rect;
        context_menu.popup ();
    }

    private uint find_item_at_y (double y) {
        // Use the selection model to approximate — pick based on known row height
        // Each row is ~32-40px with margins. We iterate items to find the best match.
        uint n = list_store.get_n_items ();
        if (n == 0) {
            return Gtk.INVALID_LIST_POSITION;
        }

        // Approximate row height based on label margins (8+8 top/bottom + ~20 text)
        double row_height = 36.0;
        uint pos = (uint) (y / row_height);
        if (pos >= n) {
            pos = n - 1;
        }
        return pos;
    }

    private string get_list_name (int64 list_id) {
        for (uint i = 0; i < list_store.get_n_items (); i++) {
            var list = (TodoList) list_store.get_item (i);
            if (list.list_id == list_id) {
                return list.name;
            }
        }
        return "";
    }

    private void on_selection_changed () {
        var pos = selection.selected;
        if (pos == Gtk.INVALID_LIST_POSITION) {
            return;
        }
        var todo_list = (TodoList) list_store.get_item (pos);
        list_selected (todo_list.list_id);
    }

    public void update_lists (GenericArray<TodoList> lists) {
        // Remember selected list ID before clearing
        int64 selected_id = -1;
        var pos = selection.selected;
        if (pos != Gtk.INVALID_LIST_POSITION && pos < list_store.get_n_items ()) {
            var selected_list = (TodoList) list_store.get_item (pos);
            selected_id = selected_list.list_id;
        }

        list_store.remove_all ();
        for (uint i = 0; i < lists.length; i++) {
            list_store.append (lists[i]);
        }

        // Restore selection
        if (selected_id > 0) {
            for (uint i = 0; i < list_store.get_n_items (); i++) {
                var list = (TodoList) list_store.get_item (i);
                if (list.list_id == selected_id) {
                    selection.selected = i;
                    return;
                }
            }
        }
    }

    public void set_default_list_id (int64 list_id) {
        default_list_id = list_id;
        // Force refresh of visible items to update the star icon
        var n = list_store.get_n_items ();
        if (n > 0) {
            list_store.items_changed (0, n, n);
        }
    }

    public void select_index (uint index) {
        if (index < list_store.get_n_items ()) {
            selection.selected = index;
        }
    }

    public void select_list_id (int64 list_id) {
        for (uint i = 0; i < list_store.get_n_items (); i++) {
            var list = (TodoList) list_store.get_item (i);
            if (list.list_id == list_id) {
                selection.selected = i;
                return;
            }
        }
    }

    private string get_logo_path () {
        var style_manager = Adw.StyleManager.get_default ();
        string filename = style_manager.dark ? "logo-dark.png" : "logo-light.png";

        string installed_path = "/usr/local/share/yep-list/" + filename;
        if (FileUtils.test (installed_path, FileTest.EXISTS)) {
            return installed_path;
        }

        string system_path = "/usr/share/yep-list/" + filename;
        if (FileUtils.test (system_path, FileTest.EXISTS)) {
            return system_path;
        }

        return installed_path;
    }
}
