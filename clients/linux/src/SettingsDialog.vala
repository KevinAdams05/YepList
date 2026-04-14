public class SettingsDialog : Adw.AlertDialog {
    private Gtk.Entry server_url_entry;

    public signal void settings_saved ();

    public SettingsDialog (AppSettings settings) {
        Object (heading: "Settings");

        add_response ("cancel", "Cancel");
        add_response ("save", "Save");
        default_response = "save";
        set_response_appearance ("save", Adw.ResponseAppearance.SUGGESTED);

        var box = new Gtk.Box (Gtk.Orientation.VERTICAL, 12);
        box.margin_start = 8;
        box.margin_end = 8;

        var url_label = new Gtk.Label ("Server URL");
        url_label.halign = Gtk.Align.START;
        url_label.add_css_class ("heading");
        box.append (url_label);

        server_url_entry = new Gtk.Entry ();
        server_url_entry.placeholder_text = "http://192.168.1.100:5000";
        server_url_entry.text = settings.server_url;
        server_url_entry.hexpand = true;
        box.append (server_url_entry);

        var hint_label = new Gtk.Label ("Changes take effect after restarting the app.");
        hint_label.add_css_class ("dim-label");
        hint_label.halign = Gtk.Align.START;
        hint_label.wrap = true;
        box.append (hint_label);

        extra_child = box;

        response.connect ((id) => {
            if (id == "save") {
                string url = server_url_entry.text.strip ();
                if (url != "") {
                    settings.server_url = url;
                    settings.save ();
                    settings_saved ();
                }
            }
        });
    }
}
