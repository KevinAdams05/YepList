using System;

namespace ToDoList.Core.Dtos
{
    public class CreateTodoItemRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public long? CategoryId { get; set; }
        public DateTime? DueDate { get; set; }
        public int SortOrder { get; set; }
    }
}
