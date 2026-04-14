public class ApiClient : Object {
    private Soup.Session session;
    private string base_url;
    private string last_sync_time = "";

    public ApiClient (string base_url) {
        this.base_url = base_url.has_suffix ("/") ? base_url : base_url + "/";
        this.session = new Soup.Session ();
    }

    // ── Lists ─────────────────────────────────────────────

    public async GenericArray<TodoList> get_lists_async () throws Error {
        var body = yield get_async ("api/lists");
        return parse_array<TodoList> (body, TodoList.from_json);
    }

    public async TodoList create_list_async (string name, int sort_order = 0) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("name"); builder.add_string_value (name);
        builder.set_member_name ("sortOrder"); builder.add_int_value (sort_order);
        builder.end_object ();

        var body = yield post_async ("api/lists", builder_to_string (builder));
        return parse_object<TodoList> (body, TodoList.from_json);
    }

    public async TodoList update_list_async (int64 list_id, string name, int sort_order = 0) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("name"); builder.add_string_value (name);
        builder.set_member_name ("sortOrder"); builder.add_int_value (sort_order);
        builder.end_object ();

        var body = yield put_async ("api/lists/%lld".printf (list_id), builder_to_string (builder));
        return parse_object<TodoList> (body, TodoList.from_json);
    }

    public async void delete_list_async (int64 list_id) throws Error {
        yield delete_request_async ("api/lists/%lld".printf (list_id));
    }

    // ── Categories ────────────────────────────────────────

    public async GenericArray<Category> get_categories_async () throws Error {
        var body = yield get_async ("api/categories");
        return parse_array<Category> (body, Category.from_json);
    }

    public async Category create_category_async (string name, string? color) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("name"); builder.add_string_value (name);
        builder.set_member_name ("color");
        if (color != null) {
            builder.add_string_value (color);
        } else {
            builder.add_null_value ();
        }
        builder.end_object ();

        var body = yield post_async ("api/categories", builder_to_string (builder));
        return parse_object<Category> (body, Category.from_json);
    }

    public async Category update_category_async (int64 category_id, string name, string? color) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("name"); builder.add_string_value (name);
        builder.set_member_name ("color");
        if (color != null) {
            builder.add_string_value (color);
        } else {
            builder.add_null_value ();
        }
        builder.end_object ();

        var body = yield put_async ("api/categories/%lld".printf (category_id), builder_to_string (builder));
        return parse_object<Category> (body, Category.from_json);
    }

    public async void delete_category_async (int64 category_id) throws Error {
        yield delete_request_async ("api/categories/%lld".printf (category_id));
    }

    // ── Items ─────────────────────────────────────────────

    public async GenericArray<TodoItem> get_items_by_list_async (int64 list_id) throws Error {
        var body = yield get_async ("api/lists/%lld/items".printf (list_id));
        return parse_array<TodoItem> (body, TodoItem.from_json);
    }

    public async TodoItem create_item_async (int64 list_id, string title, string? notes,
                                              int64 category_id, bool has_category,
                                              string? due_date, int sort_order = 0) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("title"); builder.add_string_value (title);
        builder.set_member_name ("notes");
        if (notes != null) {
            builder.add_string_value (notes);
        } else {
            builder.add_null_value ();
        }
        builder.set_member_name ("categoryId");
        if (has_category) {
            builder.add_int_value (category_id);
        } else {
            builder.add_null_value ();
        }
        builder.set_member_name ("dueDate");
        if (due_date != null) {
            builder.add_string_value (due_date);
        } else {
            builder.add_null_value ();
        }
        builder.set_member_name ("sortOrder"); builder.add_int_value (sort_order);
        builder.end_object ();

        var body = yield post_async ("api/lists/%lld/items".printf (list_id), builder_to_string (builder));
        return parse_object<TodoItem> (body, TodoItem.from_json);
    }

    public async TodoItem update_item_async (int64 item_id, string title, string? notes,
                                              int64 category_id, bool has_category,
                                              bool is_completed, string? due_date,
                                              int sort_order = 0,
                                              int64 list_id = -1) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("title"); builder.add_string_value (title);
        builder.set_member_name ("notes");
        if (notes != null) {
            builder.add_string_value (notes);
        } else {
            builder.add_null_value ();
        }
        builder.set_member_name ("categoryId");
        if (has_category) {
            builder.add_int_value (category_id);
        } else {
            builder.add_null_value ();
        }
        if (list_id > 0) {
            builder.set_member_name ("listId"); builder.add_int_value (list_id);
        }
        builder.set_member_name ("isCompleted"); builder.add_boolean_value (is_completed);
        builder.set_member_name ("dueDate");
        if (due_date != null) {
            builder.add_string_value (due_date);
        } else {
            builder.add_null_value ();
        }
        builder.set_member_name ("sortOrder"); builder.add_int_value (sort_order);
        builder.end_object ();

        var body = yield put_async ("api/items/%lld".printf (item_id), builder_to_string (builder));
        return parse_object<TodoItem> (body, TodoItem.from_json);
    }

    public async TodoItem toggle_complete_async (int64 item_id, bool is_completed) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("isCompleted"); builder.add_boolean_value (is_completed);
        builder.end_object ();

        var body = yield patch_async ("api/items/%lld/complete".printf (item_id), builder_to_string (builder));
        return parse_object<TodoItem> (body, TodoItem.from_json);
    }

    public async void delete_item_async (int64 item_id) throws Error {
        yield delete_request_async ("api/items/%lld".printf (item_id));
    }

    public async void reorder_items_async (int64 list_id,
                                            GenericArray<TodoItem> items) throws Error {
        var builder = new Json.Builder ();
        builder.begin_object ();
        builder.set_member_name ("items");
        builder.begin_array ();
        for (uint i = 0; i < items.length; i++) {
            builder.begin_object ();
            builder.set_member_name ("itemId");
            builder.add_int_value (items[i].item_id);
            builder.set_member_name ("sortOrder");
            builder.add_int_value ((int64) i);
            builder.end_object ();
        }
        builder.end_array ();
        builder.end_object ();

        yield put_async ("api/lists/%lld/items/reorder".printf (list_id),
                         builder_to_string (builder));
    }

    // ── Sync ──────────────────────────────────────────────

    public async SyncResponse sync_async () throws Error {
        var path = "api/sync";
        if (last_sync_time != "") {
            path += "?since=" + GLib.Uri.escape_string (last_sync_time, null, true);
        }

        var body = yield get_async (path);
        var response = parse_object<SyncResponse> (body, SyncResponse.from_json);
        last_sync_time = response.server_time;
        return response;
    }

    public void reset_sync_time () {
        last_sync_time = "";
    }

    // ── HTTP Helpers ──────────────────────────────────────

    private async string get_async (string path) throws Error {
        var msg = new Soup.Message ("GET", base_url + path);
        var bytes = yield session.send_and_read_async (msg, Priority.DEFAULT, null);
        check_status (msg);
        return (string) bytes.get_data ();
    }

    private async string post_async (string path, string json_body) throws Error {
        var msg = new Soup.Message ("POST", base_url + path);
        msg.set_request_body_from_bytes ("application/json", new Bytes (json_body.data));
        var bytes = yield session.send_and_read_async (msg, Priority.DEFAULT, null);
        check_status (msg);
        return (string) bytes.get_data ();
    }

    private async string put_async (string path, string json_body) throws Error {
        var msg = new Soup.Message ("PUT", base_url + path);
        msg.set_request_body_from_bytes ("application/json", new Bytes (json_body.data));
        var bytes = yield session.send_and_read_async (msg, Priority.DEFAULT, null);
        check_status (msg);
        return (string) bytes.get_data ();
    }

    private async string patch_async (string path, string json_body) throws Error {
        var msg = new Soup.Message ("PATCH", base_url + path);
        msg.set_request_body_from_bytes ("application/json", new Bytes (json_body.data));
        var bytes = yield session.send_and_read_async (msg, Priority.DEFAULT, null);
        check_status (msg);
        return (string) bytes.get_data ();
    }

    private async void delete_request_async (string path) throws Error {
        var msg = new Soup.Message ("DELETE", base_url + path);
        yield session.send_and_read_async (msg, Priority.DEFAULT, null);
        check_status (msg);
    }

    private void check_status (Soup.Message msg) throws Error {
        if (msg.status_code < 200 || msg.status_code >= 300) {
            throw new IOError.FAILED ("HTTP %u: %s", msg.status_code, msg.reason_phrase);
        }
    }

    // ── JSON Helpers ──────────────────────────────────────

    private string builder_to_string (Json.Builder builder) {
        var generator = new Json.Generator ();
        generator.set_root (builder.get_root ());
        return generator.to_data (null);
    }

    private delegate T FromJsonFunc<T> (Json.Object obj);

    private GenericArray<T> parse_array<T> (string body, FromJsonFunc<T> from_json) throws Error {
        var parser = new Json.Parser ();
        parser.load_from_data (body);
        var arr = parser.get_root ().get_array ();
        var result = new GenericArray<T> ();
        for (uint i = 0; i < arr.get_length (); i++) {
            result.add (from_json (arr.get_object_element (i)));
        }
        return result;
    }

    private T parse_object<T> (string body, FromJsonFunc<T> from_json) throws Error {
        var parser = new Json.Parser ();
        parser.load_from_data (body);
        return from_json (parser.get_root ().get_object ());
    }
}
