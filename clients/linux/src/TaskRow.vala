public class TaskRow : Gtk.Box {
    private Gtk.CheckButton check_button;
    private Gtk.Label title_label;
    private Gtk.Label category_label;
    private Gtk.Label due_date_label;

    private TodoItem? bound_item = null;

    public signal void completion_toggled (TodoItem item);

    public TaskRow () {
        Object (
            orientation: Gtk.Orientation.HORIZONTAL,
            spacing: 8
        );

        margin_start = 12;
        margin_end = 12;
        margin_top = 6;
        margin_bottom = 6;

        check_button = new Gtk.CheckButton ();
        check_button.toggled.connect (on_check_toggled);
        append (check_button);

        title_label = new Gtk.Label ("");
        title_label.halign = Gtk.Align.START;
        title_label.hexpand = true;
        title_label.ellipsize = Pango.EllipsizeMode.END;
        append (title_label);

        category_label = new Gtk.Label ("");
        category_label.halign = Gtk.Align.END;
        category_label.add_css_class ("dim-label");
        append (category_label);

        due_date_label = new Gtk.Label ("");
        due_date_label.halign = Gtk.Align.END;
        due_date_label.add_css_class ("dim-label");
        due_date_label.width_chars = 10;
        append (due_date_label);
    }

    public void bind_item (TodoItem item, GenericArray<Category> categories) {
        bound_item = item;

        // Block signal during binding to prevent triggering API calls
        check_button.toggled.disconnect (on_check_toggled);
        check_button.active = item.is_completed;
        check_button.toggled.connect (on_check_toggled);

        title_label.label = item.title;

        // Style completed tasks
        if (item.is_completed) {
            title_label.add_css_class ("dim-label");
            var attrs = new Pango.AttrList ();
            attrs.insert (Pango.attr_strikethrough_new (true));
            title_label.attributes = attrs;
        } else {
            title_label.remove_css_class ("dim-label");
            title_label.attributes = null;
        }

        // Category name
        category_label.label = "";
        if (item.has_category) {
            for (uint i = 0; i < categories.length; i++) {
                if (categories[i].category_id == item.category_id) {
                    category_label.label = categories[i].name;
                    break;
                }
            }
        }

        // Due date
        if (item.due_date != null) {
            // Parse date string (yyyy-MM-dd or ISO format)
            var date_str = item.due_date;
            if (date_str.length >= 10) {
                due_date_label.label = date_str.substring (0, 10);
            } else {
                due_date_label.label = date_str;
            }
        } else {
            due_date_label.label = "";
        }
    }

    private void on_check_toggled () {
        if (bound_item != null) {
            completion_toggled (bound_item);
        }
    }
}
