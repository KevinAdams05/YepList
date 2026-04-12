public class CategoryManagerDialog : Adw.AlertDialog {
    private ApiClient api_client;
    private GenericArray<Category> categories;

    private Gtk.ListView category_list;
    private GLib.ListStore category_store;
    private Gtk.SingleSelection category_selection;
    private Gtk.Entry name_entry;
    private Gtk.Entry color_entry;

    public CategoryManagerDialog (ApiClient api_client, GenericArray<Category> categories) {
        Object (heading: "Manage Categories");

        this.api_client = api_client;
        this.categories = categories;

        add_response ("close", "Close");
        default_response = "close";

        build_form ();
        refresh_list ();
    }

    private void build_form () {
        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 12);
        box.margin_start = 8;
        box.margin_end = 8;

        // Category list
        category_store = new GLib.ListStore (typeof (Category));
        category_selection = new Gtk.SingleSelection (category_store);
        category_selection.selection_changed.connect (on_selection_changed);

        var factory = new Gtk.SignalListItemFactory ();
        factory.setup.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var row_box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 8);
            row_box.margin_start = 8;
            row_box.margin_end = 8;
            row_box.margin_top = 4;
            row_box.margin_bottom = 4;

            var name_label = new Gtk.Label ("");
            name_label.halign = Gtk.Align.START;
            name_label.hexpand = true;
            row_box.append (name_label);

            var color_label = new Gtk.Label ("");
            color_label.halign = Gtk.Align.END;
            row_box.append (color_label);

            list_item.child = row_box;
        });
        factory.bind.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var row_box = (Gtk.Box) list_item.child;
            var cat = (Category) list_item.item;

            var name_label = (Gtk.Label) row_box.get_first_child ();
            name_label.label = cat.name;

            var color_label = (Gtk.Label) name_label.get_next_sibling ();
            color_label.label = cat.color ?? "";
        });

        category_list = new Gtk.ListView (category_selection, factory);
        category_list.set_size_request (350, 200);

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = category_list;
        scrolled.set_size_request (350, 200);
        box.append (scrolled);

        // Input fields
        var input_box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 8);

        name_entry = new Gtk.Entry ();
        name_entry.placeholder_text = "Category name";
        name_entry.hexpand = true;
        input_box.append (name_entry);

        color_entry = new Gtk.Entry ();
        color_entry.placeholder_text = "#FF5733";
        color_entry.width_chars = 9;
        input_box.append (color_entry);

        box.append (input_box);

        // Action buttons
        var button_box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 8);
        button_box.halign = Gtk.Align.END;

        var add_button = new Gtk.Button.with_label ("Add");
        add_button.add_css_class ("suggested-action");
        add_button.clicked.connect (() => { add_category.begin (); });
        button_box.append (add_button);

        var update_button = new Gtk.Button.with_label ("Update");
        update_button.clicked.connect (() => { update_category.begin (); });
        button_box.append (update_button);

        var delete_button = new Gtk.Button.with_label ("Delete");
        delete_button.add_css_class ("destructive-action");
        delete_button.clicked.connect (() => { delete_category.begin (); });
        button_box.append (delete_button);

        box.append (button_box);

        extra_child = box;
    }

    private void refresh_list () {
        category_store.remove_all ();
        for (uint i = 0; i < categories.length; i++) {
            category_store.append (categories[i]);
        }
    }

    private void on_selection_changed () {
        var pos = category_selection.selected;
        if (pos == Gtk.INVALID_LIST_POSITION) {
            return;
        }
        var cat = (Category) category_store.get_item (pos);
        name_entry.text = cat.name;
        color_entry.text = cat.color ?? "";
    }

    private Category? get_selected () {
        var pos = category_selection.selected;
        if (pos == Gtk.INVALID_LIST_POSITION) {
            return null;
        }
        return (Category) category_store.get_item (pos);
    }

    private async void add_category () {
        var name = name_entry.text.strip ();
        if (name == "") {
            return;
        }
        var color = color_entry.text.strip ();
        if (color == "") {
            color = null;
        }

        try {
            var new_cat = yield api_client.create_category_async (name, color);
            categories.add (new_cat);
            refresh_list ();
            name_entry.text = "";
            color_entry.text = "";
        } catch (Error e) {
            warning ("Error adding category: %s", e.message);
        }
    }

    private async void update_category () {
        var selected = get_selected ();
        if (selected == null) {
            return;
        }

        var name = name_entry.text.strip ();
        if (name == "") {
            return;
        }
        var color = color_entry.text.strip ();
        if (color == "") {
            color = null;
        }

        try {
            var updated = yield api_client.update_category_async (selected.category_id, name, color);
            selected.name = updated.name;
            selected.color = updated.color;
            refresh_list ();
        } catch (Error e) {
            warning ("Error updating category: %s", e.message);
        }
    }

    private async void delete_category () {
        var selected = get_selected ();
        if (selected == null) {
            return;
        }

        try {
            yield api_client.delete_category_async (selected.category_id);
            // Remove from local list
            for (uint i = 0; i < categories.length; i++) {
                if (categories[i].category_id == selected.category_id) {
                    categories.remove_index (i);
                    break;
                }
            }
            refresh_list ();
            name_entry.text = "";
            color_entry.text = "";
        } catch (Error e) {
            warning ("Error deleting category: %s", e.message);
        }
    }
}
