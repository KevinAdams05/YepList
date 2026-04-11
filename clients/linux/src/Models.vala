public class TodoList : Object {
    public int64 list_id { get; set; }
    public string name { get; set; default = ""; }
    public int sort_order { get; set; }
    public string created_date { get; set; default = ""; }
    public string modified_date { get; set; default = ""; }

    public TodoList () {}

    public static TodoList from_json (Json.Object obj) {
        var list = new TodoList ();
        list.list_id = obj.get_int_member_with_default ("listId", 0);
        list.name = obj.get_string_member_with_default ("name", "");
        list.sort_order = (int) obj.get_int_member_with_default ("sortOrder", 0);
        list.created_date = obj.get_string_member_with_default ("createdDate", "");
        list.modified_date = obj.get_string_member_with_default ("modifiedDate", "");
        return list;
    }
}

public class Category : Object {
    public int64 category_id { get; set; }
    public string name { get; set; default = ""; }
    public string? color { get; set; default = null; }
    public string created_date { get; set; default = ""; }
    public string modified_date { get; set; default = ""; }

    public Category () {}

    public static Category from_json (Json.Object obj) {
        var cat = new Category ();
        cat.category_id = obj.get_int_member_with_default ("categoryId", 0);
        cat.name = obj.get_string_member_with_default ("name", "");
        if (obj.has_member ("color") && !obj.get_null_member ("color")) {
            cat.color = obj.get_string_member ("color");
        }
        cat.created_date = obj.get_string_member_with_default ("createdDate", "");
        cat.modified_date = obj.get_string_member_with_default ("modifiedDate", "");
        return cat;
    }
}

public class TodoItem : Object {
    public int64 item_id { get; set; }
    public int64 list_id { get; set; }
    public int64 category_id { get; set; default = 0; }
    public bool has_category { get; set; default = false; }
    public string title { get; set; default = ""; }
    public string? notes { get; set; default = null; }
    public bool is_completed { get; set; default = false; }
    public string? due_date { get; set; default = null; }
    public int sort_order { get; set; }
    public string created_date { get; set; default = ""; }
    public string modified_date { get; set; default = ""; }

    public TodoItem () {}

    public static TodoItem from_json (Json.Object obj) {
        var item = new TodoItem ();
        item.item_id = obj.get_int_member_with_default ("itemId", 0);
        item.list_id = obj.get_int_member_with_default ("listId", 0);
        if (obj.has_member ("categoryId") && !obj.get_null_member ("categoryId")) {
            item.category_id = obj.get_int_member ("categoryId");
            item.has_category = true;
        }
        item.title = obj.get_string_member_with_default ("title", "");
        if (obj.has_member ("notes") && !obj.get_null_member ("notes")) {
            item.notes = obj.get_string_member ("notes");
        }
        item.is_completed = obj.get_boolean_member_with_default ("isCompleted", false);
        if (obj.has_member ("dueDate") && !obj.get_null_member ("dueDate")) {
            item.due_date = obj.get_string_member ("dueDate");
        }
        item.sort_order = (int) obj.get_int_member_with_default ("sortOrder", 0);
        item.created_date = obj.get_string_member_with_default ("createdDate", "");
        item.modified_date = obj.get_string_member_with_default ("modifiedDate", "");
        return item;
    }
}

public class SyncResponse : Object {
    public string server_time { get; set; default = ""; }
    public GenericArray<TodoList> lists { get; owned set; default = new GenericArray<TodoList> (); }
    public GenericArray<Category> categories { get; owned set; default = new GenericArray<Category> (); }
    public GenericArray<TodoItem> items { get; owned set; default = new GenericArray<TodoItem> (); }
    public GenericArray<int64?> deleted_list_ids { get; owned set; default = new GenericArray<int64?> (); }
    public GenericArray<int64?> deleted_item_ids { get; owned set; default = new GenericArray<int64?> (); }
    public GenericArray<int64?> deleted_category_ids { get; owned set; default = new GenericArray<int64?> (); }

    public SyncResponse () {}

    public static SyncResponse from_json (Json.Object obj) {
        var response = new SyncResponse ();
        response.server_time = obj.get_string_member_with_default ("serverTime", "");

        if (obj.has_member ("lists")) {
            var arr = obj.get_array_member ("lists");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.lists.add (TodoList.from_json (arr.get_object_element (i)));
            }
        }

        if (obj.has_member ("categories")) {
            var arr = obj.get_array_member ("categories");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.categories.add (Category.from_json (arr.get_object_element (i)));
            }
        }

        if (obj.has_member ("items")) {
            var arr = obj.get_array_member ("items");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.items.add (TodoItem.from_json (arr.get_object_element (i)));
            }
        }

        if (obj.has_member ("deletedListIds")) {
            var arr = obj.get_array_member ("deletedListIds");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.deleted_list_ids.add (arr.get_int_element (i));
            }
        }

        if (obj.has_member ("deletedItemIds")) {
            var arr = obj.get_array_member ("deletedItemIds");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.deleted_item_ids.add (arr.get_int_element (i));
            }
        }

        if (obj.has_member ("deletedCategoryIds")) {
            var arr = obj.get_array_member ("deletedCategoryIds");
            for (uint i = 0; i < arr.get_length (); i++) {
                response.deleted_category_ids.add (arr.get_int_element (i));
            }
        }

        return response;
    }
}
