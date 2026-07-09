using System;

namespace ToDoList.Core.Dtos
{
    public class CategoryDto
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime ClientModifiedDate { get; set; }
    }
}
