using System;

namespace ToDoList.Windows.Models
{
    public class Category
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public override string ToString() => Name;
    }
}
