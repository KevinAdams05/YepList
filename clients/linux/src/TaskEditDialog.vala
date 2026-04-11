public class TaskEditDialog : Adw.AlertDialog {
    private Gtk.Entry title_entry;
    private Gtk.TextView notes_view;
    private Gtk.DropDown category_dropdown;
    private Gtk.CheckButton due_date_check;
    private Gtk.Calendar calendar;

    private GenericArray<Category> categories;

    public string task_title {
        get { return title_entry.text.strip (); }
    }

    public string? task_notes {
        get {
            var text = notes_view.buffer.text.strip ();
            return text == "" ? null : text;
        }
    }

    public int64 selected_category_id {
        get {
            uint pos = category_dropdown.selected;
            if (pos == 0 || pos == Gtk.INVALID_LIST_POSITION) {
                return 0;
            }
            return categories[pos - 1].category_id;
        }
    }

    public bool has_selected_category {
        get {
            uint pos = category_dropdown.selected;
            return pos > 0 && pos != Gtk.INVALID_LIST_POSITION;
        }
    }

    public string? task_due_date {
        get {
            if (!due_date_check.active) {
                return null;
            }
            var date = calendar.get_date ();
            return "%04d-%02d-%02d".printf (date.get_year (), date.get_month (), date.get_day_of_month ());
        }
    }

    public TaskEditDialog (TodoItem? existing, GenericArray<Category> categories) {
        this.categories = categories;

        string dialog_title = existing != null ? "Edit Task" : "New Task";
        Object (heading: dialog_title);

        add_response ("cancel", "Cancel");
        add_response ("save", "Save");
        default_response = "save";
        set_response_appearance ("save", Adw.ResponseAppearance.SUGGESTED);

        build_form (existing);
    }

    private void build_form (TodoItem? existing) {
        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 12);
        box.margin_start = 16;
        box.margin_end = 16;

        // Title
        var title_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        var title_label = new Gtk.Label ("Title");
        title_label.halign = Gtk.Align.START;
        title_label.add_css_class ("heading");
        title_box.append (title_label);

        title_entry = new Gtk.Entry ();
        title_entry.placeholder_text = "Task title";
        if (existing != null) {
            title_entry.text = existing.title;
        }
        title_box.append (title_entry);
        box.append (title_box);

        // Notes
        var notes_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        var notes_label = new Gtk.Label ("Notes");
        notes_label.halign = Gtk.Align.START;
        notes_label.add_css_class ("heading");
        notes_box.append (notes_label);

        notes_view = new Gtk.TextView ();
        notes_view.wrap_mode = Gtk.WrapMode.WORD_CHAR;
        notes_view.set_size_request (-1, 80);
        if (existing != null && existing.notes != null) {
            notes_view.buffer.text = existing.notes;
        }

        var notes_scroll = new Gtk.ScrolledWindow ();
        notes_scroll.child = notes_view;
        notes_scroll.set_size_request (-1, 80);
        notes_box.append (notes_scroll);
        box.append (notes_box);

        // Category
        var category_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        var category_label = new Gtk.Label ("Category");
        category_label.halign = Gtk.Align.START;
        category_label.add_css_class ("heading");
        category_box.append (category_label);

        var category_strings = new string[categories.length + 1];
        category_strings[0] = "(None)";
        for (uint i = 0; i < categories.length; i++) {
            category_strings[i + 1] = categories[i].name;
        }
        category_dropdown = new Gtk.DropDown.from_strings (category_strings);
        category_dropdown.selected = 0;

        if (existing != null && existing.has_category) {
            for (uint i = 0; i < categories.length; i++) {
                if (categories[i].category_id == existing.category_id) {
                    category_dropdown.selected = i + 1;
                    break;
                }
            }
        }
        category_box.append (category_dropdown);
        box.append (category_box);

        // Due date
        var due_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        due_date_check = new Gtk.CheckButton.with_label ("Due Date");
        due_date_check.add_css_class ("heading");
        due_box.append (due_date_check);

        calendar = new Gtk.Calendar ();
        calendar.sensitive = false;
        due_date_check.toggled.connect (() => {
            calendar.sensitive = due_date_check.active;
        });

        if (existing != null && existing.due_date != null) {
            due_date_check.active = true;
            calendar.sensitive = true;
            // Parse the date string and set calendar
            var parts = existing.due_date.substring (0, 10).split ("-");
            if (parts.length >= 3) {
                var date = new DateTime.local (int.parse (parts[0]), int.parse (parts[1]),
                                                int.parse (parts[2]), 0, 0, 0);
                calendar.select_day (date);
            }
        }

        due_box.append (calendar);
        box.append (due_box);

        extra_child = box;
    }
}
