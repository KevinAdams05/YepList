using System;

namespace ToDoList.Windows.Models
{
    public class TodoList
    {
        public long ListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public override string ToString() => Name;
    }
}
