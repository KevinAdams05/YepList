using System;
using System.Collections.Generic;

namespace ToDoList.Windows.Models
{
    public class SyncResponse
    {
        public DateTime ServerTime { get; set; }
        public List<TodoList> Lists { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<TodoItem> Items { get; set; } = new();
        public List<long> DeletedItemIds { get; set; } = new();
        public List<long> DeletedListIds { get; set; } = new();
        public List<long> DeletedCategoryIds { get; set; } = new();
    }
}
