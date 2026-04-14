public class AboutDialog : Adw.Dialog {
    public AboutDialog () {
        Object (
            title: "About YepList",
            content_width: 440,
            content_height: 520
        );

        build_ui ();
    }

    private void build_ui () {
        var notebook = new Gtk.Notebook ();
        notebook.margin_start = 4;
        notebook.margin_end = 4;
        notebook.margin_bottom = 4;

        notebook.append_page (build_about_tab (), new Gtk.Label ("About"));
        notebook.append_page (build_libraries_tab (), new Gtk.Label ("Libraries"));
        notebook.append_page (build_changelog_tab (), new Gtk.Label ("Changelog"));

        var toolbar_view = new Adw.ToolbarView ();
        toolbar_view.add_top_bar (new Adw.HeaderBar ());
        toolbar_view.content = notebook;

        this.child = toolbar_view;
    }

    private Gtk.Widget build_about_tab () {
        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 12);
        box.margin_top = 24;
        box.margin_bottom = 24;
        box.margin_start = 24;
        box.margin_end = 24;
        box.halign = Gtk.Align.CENTER;

        // Theme-aware logo
        var logo_image = new Gtk.Picture ();
        logo_image.set_size_request (240, 72);
        logo_image.content_fit = Gtk.ContentFit.CONTAIN;

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

        box.append (logo_image);

        // Version
        var version_label = new Gtk.Label ("Version 0.5.0");
        version_label.add_css_class ("dim-label");
        box.append (version_label);

        // Author
        var author_label = new Gtk.Label ("by Kevin Adams");
        box.append (author_label);

        // License
        var license_label = new Gtk.Label ("Licensed under the MIT License");
        license_label.add_css_class ("heading");
        license_label.margin_top = 12;
        box.append (license_label);

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = box;
        scrolled.vexpand = true;
        return scrolled;
    }

    private Gtk.Widget build_libraries_tab () {
        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        box.margin_top = 16;
        box.margin_bottom = 16;
        box.margin_start = 24;
        box.margin_end = 24;

        add_credit_row (box, "GTK 4", "LGPL 2.1+", "GNOME Project");
        add_credit_row (box, "libadwaita", "LGPL 2.1+", "GNOME Project");
        add_credit_row (box, "libsoup 3.0", "LGPL 2.1+", "GNOME Project");
        add_credit_row (box, "json-glib", "LGPL 2.1+", "GNOME Project");
        add_credit_row (box, "GLib / GIO", "LGPL 2.1+", "GNOME Project");

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = box;
        scrolled.vexpand = true;
        return scrolled;
    }

    private Gtk.Widget build_changelog_tab () {
        var text_view = new Gtk.TextView ();
        text_view.editable = false;
        text_view.cursor_visible = false;
        text_view.wrap_mode = Gtk.WrapMode.WORD;
        text_view.left_margin = 16;
        text_view.right_margin = 16;
        text_view.top_margin = 12;
        text_view.bottom_margin = 12;

        string changelog = load_changelog ();
        apply_markdown (text_view.buffer, changelog);

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = text_view;
        scrolled.vexpand = true;
        return scrolled;
    }

    private void apply_markdown (Gtk.TextBuffer buffer, string markdown) {
        var tag_h1 = buffer.create_tag ("h1", "weight", Pango.Weight.BOLD, "scale", 1.5);
        var tag_h2 = buffer.create_tag ("h2", "weight", Pango.Weight.BOLD, "scale", 1.3);
        var tag_h3 = buffer.create_tag ("h3", "weight", Pango.Weight.BOLD, "scale", 1.1);
        var tag_bold = buffer.create_tag ("bold", "weight", Pango.Weight.BOLD);

        string[] lines = markdown.split ("\n");
        Gtk.TextIter iter;
        buffer.get_start_iter (out iter);

        foreach (string raw_line in lines) {
            string line = raw_line.strip ();

            if (line.has_prefix ("### ")) {
                string text = line.substring (4) + "\n";
                buffer.insert_with_tags (ref iter, text, text.length, tag_h3);
            } else if (line.has_prefix ("## ")) {
                string text = line.substring (3) + "\n";
                buffer.insert_with_tags (ref iter, text, text.length, tag_h2);
            } else if (line.has_prefix ("# ")) {
                string text = line.substring (2) + "\n";
                buffer.insert_with_tags (ref iter, text, text.length, tag_h1);
            } else if (line.has_prefix ("- ")) {
                string content = line.substring (2);
                buffer.insert (ref iter, "  \u2022 ", -1);
                insert_with_inline_bold (buffer, ref iter, content, tag_bold);
                buffer.insert (ref iter, "\n", -1);
            } else {
                buffer.insert (ref iter, line + "\n", -1);
            }
        }
    }

    private void insert_with_inline_bold (Gtk.TextBuffer buffer, ref Gtk.TextIter iter, string text, Gtk.TextTag bold_tag) {
        int i = 0;
        while (i < text.length) {
            int bold_start = text.index_of ("**", i);
            if (bold_start == -1) {
                buffer.insert (ref iter, text.substring (i), -1);
                break;
            }

            if (bold_start > i) {
                buffer.insert (ref iter, text.substring (i, bold_start - i), -1);
            }

            int bold_end = text.index_of ("**", bold_start + 2);
            if (bold_end == -1) {
                buffer.insert (ref iter, text.substring (bold_start), -1);
                break;
            }

            string bold_text = text.substring (bold_start + 2, bold_end - bold_start - 2);
            buffer.insert_with_tags (ref iter, bold_text, bold_text.length, bold_tag);
            i = bold_end + 2;
        }
    }

    private string load_changelog () {
        // Check installed locations
        string[] paths = {
            "/usr/local/share/yep-list/CHANGELOG.md",
            "/usr/share/yep-list/CHANGELOG.md"
        };

        foreach (string path in paths) {
            if (FileUtils.test (path, FileTest.EXISTS)) {
                try {
                    string contents;
                    FileUtils.get_contents (path, out contents);
                    return contents;
                } catch (FileError e) {
                    // Fall through
                }
            }
        }

        return "Changelog not found.";
    }

    private void add_credit_row (Gtk.Box parent, string library, string license, string author) {
        var row_box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 8);

        var name_label = new Gtk.Label (library);
        name_label.halign = Gtk.Align.START;
        name_label.hexpand = true;
        name_label.xalign = 0;

        var info_label = new Gtk.Label ("%s — %s".printf (license, author));
        info_label.add_css_class ("dim-label");

        row_box.append (name_label);
        row_box.append (info_label);

        parent.append (row_box);
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
