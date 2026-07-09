using System;

namespace ToDoList.Core.Dtos
{
    public class TodoListDto
    {
        public long ListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime ClientModifiedDate { get; set; }
    }
}
