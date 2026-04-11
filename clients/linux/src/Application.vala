public class TodoApp : Adw.Application {
    public static string server_url = "http://192.168.74.122:5000";

    public TodoApp () {
        Object (
            application_id: "com.github.kevinadams05.todolist",
            flags: ApplicationFlags.FLAGS_NONE
        );
    }

    protected override void activate () {
        var win = new MainWindow (this);
        win.present ();
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
