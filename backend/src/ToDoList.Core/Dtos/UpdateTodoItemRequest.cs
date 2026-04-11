using System;

namespace ToDoList.Core.Dtos
{
    public class UpdateTodoItemRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public long? CategoryId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public int SortOrder { get; set; }
    }
}
