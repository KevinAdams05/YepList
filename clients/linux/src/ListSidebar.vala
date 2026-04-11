public class ListSidebar : Gtk.Box {
    private Gtk.ListView list_view;
    private Gtk.SingleSelection selection;
    private GLib.ListStore list_store;
    private Gtk.Button add_button;

    public signal void list_selected (int64 list_id);
    public signal void new_list_requested ();

    public ListSidebar () {
        Object (
            orientation: Gtk.Orientation.VERTICAL,
            spacing: 0
        );

        build_ui ();
    }

    private void build_ui () {
        var header = new Adw.HeaderBar ();
        header.title_widget = new Adw.WindowTitle ("Lists", "");
        append (header);

        // List store and selection
        list_store = new GLib.ListStore (typeof (TodoList));
        selection = new Gtk.SingleSelection (list_store);
        selection.selection_changed.connect (on_selection_changed);

        // Factory
        var factory = new Gtk.SignalListItemFactory ();
        factory.setup.connect ((item) => {
            var label = new Gtk.Label ("");
            label.halign = Gtk.Align.START;
            label.margin_start = 12;
            label.margin_end = 12;
            label.margin_top = 8;
            label.margin_bottom = 8;
            item.child = label;
        });
        factory.bind.connect ((item) => {
            var label = (Gtk.Label) item.child;
            var todo_list = (TodoList) item.item;
            label.label = todo_list.name;
        });

        list_view = new Gtk.ListView (selection, factory);
        list_view.vexpand = true;

        var scrolled = new Gtk.ScrolledWindow ();
        scrolled.child = list_view;
        scrolled.vexpand = true;
        append (scrolled);

        // Add button
        add_button = new Gtk.Button.with_label ("New List");
        add_button.margin_start = 8;
        add_button.margin_end = 8;
        add_button.margin_top = 8;
        add_button.margin_bottom = 8;
        add_button.clicked.connect (() => {
            new_list_requested ();
        });
        append (add_button);
    }

    private void on_selection_changed () {
        var pos = selection.selected;
        if (pos == Gtk.INVALID_LIST_POSITION) {
            return;
        }
        var todo_list = (TodoList) list_store.get_item (pos);
        list_selected (todo_list.list_id);
    }

    public void update_lists (GenericArray<TodoList> lists) {
        list_store.remove_all ();
        for (uint i = 0; i < lists.length; i++) {
            list_store.append (lists[i]);
        }
    }
}
