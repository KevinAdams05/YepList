using System;

namespace ToDoList.Core.Models
{
    public class TodoList
    {
        public long ListId { get; set; }
        public string Name { get; set; } = string.Empty;
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
