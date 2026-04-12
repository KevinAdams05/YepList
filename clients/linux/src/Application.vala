public class TodoApp : Adw.Application {
    public static string server_url = "http://192.168.74.122:5000";

    public TodoApp () {
        Object (
            application_id: "com.github.kevinadams05.yeplist",
            flags: ApplicationFlags.FLAGS_NONE
        );
    }

    protected override void activate () {
        apply_dark_mode_if_needed ();

        // Set window icon for non-GNOME desktops
        Gtk.IconTheme.get_for_display (Gdk.Display.get_default ()).add_search_path (
            "/usr/local/share/icons/hicolor/256x256/apps");
        Gtk.Window.set_default_icon_name ("com.github.kevinadams05.yeplist");

        var win = new MainWindow (this);
        win.present ();
    }

    private void apply_dark_mode_if_needed () {
        var style_manager = Adw.StyleManager.get_default ();

        // On GNOME, the freedesktop color-scheme portal handles dark mode.
        // On XFCE and other desktops without portal support, libadwaita
        // overrides gtk_theme_name to "Adwaita-empty" so we can't rely on
        // Gtk.Settings. Instead, query xfconf (XFCE) or GTK_THEME env var.
        if (style_manager.system_supports_color_schemes) {
            return;
        }

        bool want_dark = false;

        // Check GTK_THEME environment variable
        string? gtk_theme_env = Environment.get_variable ("GTK_THEME");
        if (gtk_theme_env != null && gtk_theme_env.down ().contains ("dark")) {
            want_dark = true;
        }

        // Query XFCE's xfconf for the real theme name via D-Bus
        if (!want_dark) {
            want_dark = xfconf_theme_is_dark ();
        }

        if (want_dark) {
            style_manager.color_scheme = Adw.ColorScheme.FORCE_DARK;
        }
    }

    private bool xfconf_theme_is_dark () {
        try {
            var connection = Bus.get_sync (BusType.SESSION);
            var result = connection.call_sync (
                "org.xfce.Xfconf",
                "/org/xfce/Xfconf",
                "org.xfce.Xfconf",
                "GetProperty",
                new Variant ("(ss)", "xsettings", "/Net/ThemeName"),
                new VariantType ("(v)"),
                DBusCallFlags.NONE,
                1000);

            Variant inner;
            result.get ("(v)", out inner);
            string theme_name = inner.get_string ();

            return theme_name.down ().contains ("dark");
        } catch (Error e) {
            // xfconf not available (not XFCE), that's fine
            return false;
        }
    }

    public static int main (string[] args) {
        // Check for server URL argument
        for (int i = 0; i < args.length; i++) {
            if (args[i] == "--server" && i + 1 < args.length) {
                server_url = args[i + 1];
            }
        }

        var app = new TodoApp ();
        return app.run (args);
    }
}
