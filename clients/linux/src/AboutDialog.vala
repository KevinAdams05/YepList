public class AboutDialog : Adw.Dialog {
    public AboutDialog () {
        Object (
            title: "About YepList",
            content_width: 400,
            content_height: 480
        );

        build_ui ();
    }

    private void build_ui () {
        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 12);
        box.margin_top = 24;
        box.margin_bottom = 24;
        box.margin_start = 24;
        box.margin_end = 24;

        // Theme-aware logo
        var logo_image = new Gtk.Picture ();
        logo_image.set_size_request (240, 72);
        logo_image.content_fit = Gtk.ContentFit.CONTAIN;

        string logo_path = get_logo_path ();
        var logo_file = File.new_for_path (logo_path);
        if (logo_file.query_exists ()) {
            logo_image.set_filename (logo_path);
        }

        // Listen for theme changes to swap logo
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
        var version_label = new Gtk.Label ("Version 0.4.1");
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

        // Separator
        var separator = new Gtk.Separator (Gtk.Orientation.HORIZONTAL);
        separator.margin_top = 8;
        separator.margin_bottom = 8;
        box.append (separator);

        // Third-party libraries header
        var credits_header = new Gtk.Label ("Third-Party Libraries");
        credits_header.add_css_class ("heading");
        credits_header.halign = Gtk.Align.START;
        box.append (credits_header);

        // Library credits as a list
        var credits_box = new Gtk.Box (Gtk.Orientation.VERTICAL, 4);
        credits_box.margin_start = 8;

        add_credit_row (credits_box, "GTK 4", "LGPL 2.1+", "GNOME Project");
        add_credit_row (credits_box, "libadwaita", "LGPL 2.1+", "GNOME Project");
        add_credit_row (credits_box, "libsoup 3.0", "LGPL 2.1+", "GNOME Project");
        add_credit_row (credits_box, "json-glib", "LGPL 2.1+", "GNOME Project");
        add_credit_row (credits_box, "GLib / GIO", "LGPL 2.1+", "GNOME Project");

        box.append (credits_box);

        // Close button
        var close_button = new Gtk.Button.with_label ("Close");
        close_button.halign = Gtk.Align.CENTER;
        close_button.margin_top = 16;
        close_button.clicked.connect (() => {
            close ();
        });
        box.append (close_button);

        var clamp = new Adw.Clamp ();
        clamp.maximum_size = 400;
        clamp.child = box;

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = clamp;
        scrolled.vexpand = true;

        var toolbar_view = new Adw.ToolbarView ();
        toolbar_view.add_top_bar (new Adw.HeaderBar ());
        toolbar_view.content = scrolled;

        this.child = toolbar_view;
    }

    private void add_credit_row (Gtk.Box parent, string library, string license, string author) {
        var row_box = new Gtk.Box (Gtk.Orientation.HORIZONTAL, 8);

        var name_label = new Gtk.Label (library);
        name_label.halign = Gtk.Align.START;
        name_label.hexpand = true;
        name_label.xalign = 0;

        var license_label = new Gtk.Label (license);
        license_label.add_css_class ("dim-label");

        row_box.append (name_label);
        row_box.append (license_label);

        parent.append (row_box);
    }

    private string get_logo_path () {
        var style_manager = Adw.StyleManager.get_default ();
        string filename = style_manager.dark ? "logo-dark.png" : "logo-light.png";

        // Check installed location first
        string installed_path = "/usr/local/share/yep-list/" + filename;
        if (FileUtils.test (installed_path, FileTest.EXISTS)) {
            return installed_path;
        }

        // Fallback to system share
        string system_path = "/usr/share/yep-list/" + filename;
        if (FileUtils.test (system_path, FileTest.EXISTS)) {
            return system_path;
        }

        return installed_path;
    }
}
