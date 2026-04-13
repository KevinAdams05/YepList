public class AppSettings : Object {
    private static string settings_path;

    public string server_url { get; set; default = ""; }
    public int64 default_list_id { get; set; default = -1; }

    static construct {
        settings_path = Path.build_filename (
            Environment.get_user_config_dir (), "yep-list", "settings.json");
    }

    public AppSettings () {}

    public static AppSettings load () {
        var settings = new AppSettings ();
        if (!FileUtils.test (settings_path, FileTest.EXISTS)) {
            return settings;
        }

        try {
            string json;
            FileUtils.get_contents (settings_path, out json);
            var parser = new Json.Parser ();
            parser.load_from_data (json);
            var obj = parser.get_root ().get_object ();

            if (obj.has_member ("serverUrl")) {
                settings.server_url = obj.get_string_member ("serverUrl");
            }
            if (obj.has_member ("defaultListId")) {
                settings.default_list_id = obj.get_int_member ("defaultListId");
            }
        } catch (Error e) {
            // Return default settings on error
        }

        return settings;
    }

    public void save () {
        try {
            var dir = Path.get_dirname (settings_path);
            DirUtils.create_with_parents (dir, 0755);

            var builder = new Json.Builder ();
            builder.begin_object ();
            builder.set_member_name ("serverUrl");
            builder.add_string_value (server_url);
            builder.set_member_name ("defaultListId");
            builder.add_int_value (default_list_id);
            builder.end_object ();

            var generator = new Json.Generator ();
            generator.set_root (builder.get_root ());
            generator.pretty = true;
            FileUtils.set_contents (settings_path, generator.to_data (null));
        } catch (Error e) {
            warning ("Failed to save settings: %s", e.message);
        }
    }
}
