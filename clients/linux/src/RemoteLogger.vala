public class RemoteLogger : Object {
    private static Soup.Session? session = null;
    private static string base_url = "";
    private static string device_name = "";
    private static GenericArray<LogEntry> queue = new GenericArray<LogEntry> ();
    private static bool is_flushing = false;

    public static void init (string server_url) {
        base_url = server_url.has_suffix ("/") ? server_url : server_url + "/";
        session = new Soup.Session ();
        device_name = Environment.get_host_name ();
    }

    public static void debug (string tag, string message) {
        print ("[%s] %s\n", tag, message);
        enqueue ("DEBUG", tag, message);
    }

    public static void info (string tag, string message) {
        print ("[%s] %s\n", tag, message);
        enqueue ("INFO", tag, message);
    }

    public static void warn (string tag, string message) {
        print ("[WARN] [%s] %s\n", tag, message);
        enqueue ("WARN", tag, message);
    }

    public static void error (string tag, string message, Error? err = null) {
        string full_message = message;
        if (err != null) {
            full_message = "%s: %s".printf (message, err.message);
        }
        printerr ("[ERROR] [%s] %s\n", tag, full_message);
        enqueue ("ERROR", tag, full_message);
    }

    private static void enqueue (string level, string tag, string message) {
        var entry = new LogEntry ();
        entry.level = level;
        entry.tag = tag;
        entry.message = message;
        entry.device = device_name;
        entry.timestamp = new DateTime.now_utc ().format_iso8601 ();
        queue.add (entry);
        flush ();
    }

    private static void flush () {
        if (session == null || base_url == "") return;
        if (is_flushing) return;
        if (queue.length == 0) return;

        is_flushing = true;

        var entries = new GenericArray<LogEntry> ();
        uint count = uint.min (queue.length, 50);
        for (uint i = 0; i < count; i++) {
            entries.add (queue[i]);
        }
        // Remove sent entries from front of queue
        var remaining = new GenericArray<LogEntry> ();
        for (uint i = count; i < queue.length; i++) {
            remaining.add (queue[i]);
        }
        queue = remaining;

        var builder = new Json.Builder ();
        builder.begin_array ();
        for (uint i = 0; i < entries.length; i++) {
            var e = entries[i];
            builder.begin_object ();
            builder.set_member_name ("level"); builder.add_string_value (e.level);
            builder.set_member_name ("tag"); builder.add_string_value (e.tag);
            builder.set_member_name ("message"); builder.add_string_value (e.message);
            builder.set_member_name ("device"); builder.add_string_value (e.device);
            builder.set_member_name ("timestamp"); builder.add_string_value (e.timestamp);
            builder.end_object ();
        }
        builder.end_array ();

        var generator = new Json.Generator ();
        generator.set_root (builder.get_root ());
        string json_body = generator.to_data (null);

        var url = base_url + "api/debug/log/batch";
        var msg = new Soup.Message ("POST", url);
        msg.set_request_body_from_bytes ("application/json", new Bytes (json_body.data));

        session.send_and_read_async.begin (msg, Priority.DEFAULT, null, (obj, res) => {
            try {
                session.send_and_read_async.end (res);
            } catch (Error e) {
                // Silently fail — don't let logging break the app
            }
            is_flushing = false;
            if (queue.length > 0) {
                flush ();
            }
        });
    }

    private class LogEntry {
        public string level = "";
        public string tag = "";
        public string message = "";
        public string device = "";
        public string timestamp = "";
    }
}
