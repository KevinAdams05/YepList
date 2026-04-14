public class CategoryManagerDialog : Adw.AlertDialog {
    private ApiClient api_client;
    private GenericArray<Category> categories;

    private Gtk.ListView category_list;
    private GLib.ListStore category_store;
    private Gtk.SingleSelection category_selection;
    private Gtk.Entry name_entry;
    private Gtk.Entry color_entry;
    private Gtk.FlowBox color_grid;
    private Gtk.Button? selected_swatch = null;

    private const string[] PRESET_COLORS = {
        "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3",
        "#03A9F4", "#00BCD4", "#009688", "#4CAF50", "#8BC34A", "#CDDC39",
        "#FFEB3B", "#FFC107", "#FF9800", "#FF5722", "#795548", "#607D8B"
    };

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

            var swatch = new Gtk.DrawingArea ();
            swatch.set_size_request (16, 16);
            swatch.valign = Gtk.Align.CENTER;
            row_box.append (swatch);

            var name_label = new Gtk.Label ("");
            name_label.halign = Gtk.Align.START;
            name_label.hexpand = true;
            row_box.append (name_label);

            list_item.child = row_box;
        });
        factory.bind.connect ((item) => {
            var list_item = (Gtk.ListItem) item;
            var row_box = (Gtk.Box) list_item.child;
            var cat = (Category) list_item.item;

            var swatch = (Gtk.DrawingArea) row_box.get_first_child ();
            var name_label = (Gtk.Label) swatch.get_next_sibling ();
            name_label.label = cat.name;

            string color_hex = cat.color ?? "";
            if (color_hex != "") {
                swatch.visible = true;
                swatch.set_draw_func ((area, cr, width, height) => {
                    Gdk.RGBA rgba = {};
                    if (rgba.parse (color_hex)) {
                        cr.set_source_rgba (rgba.red, rgba.green, rgba.blue, rgba.alpha);
                        draw_rounded_rect (cr, 0, 0, width, height, 4);
                        cr.fill ();
                    }
                });
            } else {
                swatch.visible = false;
            }
        });

        category_list = new Gtk.ListView (category_selection, factory);
        category_list.set_size_request (400, 200);

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = category_list;
        scrolled.set_size_request (400, 200);
        box.append (scrolled);

        // Name entry
        name_entry = new Gtk.Entry ();
        name_entry.placeholder_text = "Category name";
        box.append (name_entry);

        // Color label
        var color_label = new Gtk.Label ("Color");
        color_label.halign = Gtk.Align.START;
        color_label.add_css_class ("heading");
        box.append (color_label);

        // Color swatches grid
        color_grid = new Gtk.FlowBox ();
        color_grid.max_children_per_line = 6;
        color_grid.min_children_per_line = 6;
        color_grid.selection_mode = Gtk.SelectionMode.NONE;
        color_grid.homogeneous = true;
        color_grid.row_spacing = 4;
        color_grid.column_spacing = 4;

        for (int i = 0; i < PRESET_COLORS.length; i++) {
            string hex = PRESET_COLORS[i];
            var swatch_btn = create_color_swatch (hex);
            color_grid.append (swatch_btn);
        }

        box.append (color_grid);

        // Custom color entry
        color_entry = new Gtk.Entry ();
        color_entry.placeholder_text = "Custom (#FF5733)";
        color_entry.changed.connect (() => {
            string custom = color_entry.text.strip ();
            if (custom != "") {
                // Clear swatch selection when typing custom color
                if (selected_swatch != null) {
                    selected_swatch.remove_css_class ("color-swatch-selected");
                    selected_swatch = null;
                }
            }
        });
        box.append (color_entry);

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

        // Add CSS for swatch styling
        var css_provider = new Gtk.CssProvider ();
        css_provider.load_from_string (
            ".color-swatch { border-radius: 50%; min-width: 32px; min-height: 32px; border: 2px solid transparent; }" +
            ".color-swatch-selected { border: 3px solid @accent_color; }"
        );
        Gtk.StyleContext.add_provider_for_display (
            Gdk.Display.get_default (),
            css_provider,
            Gtk.STYLE_PROVIDER_PRIORITY_APPLICATION
        );

        extra_child = box;
    }

    private Gtk.Button create_color_swatch (string hex) {
        var btn = new Gtk.Button ();
        btn.add_css_class ("color-swatch");
        btn.set_size_request (32, 32);

        var drawing = new Gtk.DrawingArea ();
        drawing.set_size_request (28, 28);
        drawing.set_draw_func ((area, cr, width, height) => {
            Gdk.RGBA rgba = {};
            if (rgba.parse (hex)) {
                cr.set_source_rgba (rgba.red, rgba.green, rgba.blue, rgba.alpha);
                double radius = double.min (width, height) / 2.0;
                cr.arc (width / 2.0, height / 2.0, radius, 0, 2 * Math.PI);
                cr.fill ();
            }
        });
        btn.child = drawing;

        btn.clicked.connect (() => {
            // Clear previous selection
            if (selected_swatch != null) {
                selected_swatch.remove_css_class ("color-swatch-selected");
            }
            btn.add_css_class ("color-swatch-selected");
            selected_swatch = btn;
            color_entry.text = hex;
        });

        return btn;
    }

    private void select_swatch_for_color (string? color) {
        // Clear previous selection
        if (selected_swatch != null) {
            selected_swatch.remove_css_class ("color-swatch-selected");
            selected_swatch = null;
        }

        if (color == null || color == "") {
            return;
        }

        // Find matching preset swatch
        for (int i = 0; i < PRESET_COLORS.length; i++) {
            if (PRESET_COLORS[i].ascii_casecmp (color) == 0) {
                var child = color_grid.get_child_at_index (i);
                if (child != null) {
                    var btn = child.child as Gtk.Button;
                    if (btn != null) {
                        btn.add_css_class ("color-swatch-selected");
                        selected_swatch = btn;
                    }
                }
                return;
            }
        }
    }

    private void draw_rounded_rect (Cairo.Context cr, double x, double y, double w, double h, double r) {
        cr.move_to (x + r, y);
        cr.line_to (x + w - r, y);
        cr.arc (x + w - r, y + r, r, -Math.PI / 2, 0);
        cr.line_to (x + w, y + h - r);
        cr.arc (x + w - r, y + h - r, r, 0, Math.PI / 2);
        cr.line_to (x + r, y + h);
        cr.arc (x + r, y + h - r, r, Math.PI / 2, Math.PI);
        cr.line_to (x, y + r);
        cr.arc (x + r, y + r, r, Math.PI, 3 * Math.PI / 2);
        cr.close_path ();
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
        select_swatch_for_color (cat.color);
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
            select_swatch_for_color (null);
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
            select_swatch_for_color (null);
        } catch (Error e) {
            warning ("Error deleting category: %s", e.message);
        }
    }
}
