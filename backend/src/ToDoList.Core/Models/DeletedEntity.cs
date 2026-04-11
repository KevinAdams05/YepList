using System;

namespace ToDoList.Core.Models
{
    public class DeletedEntity
    {
        public long Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
