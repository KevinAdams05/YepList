using System;

namespace ToDoList.Core.Models
{
    public class TodoItem
    {
        public long ItemId { get; set; }
        public long ListId { get; set; }
        public long? CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public int SortOrder { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime ClientModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedByDevice { get; set; }
    }
}
