using System;

namespace ToDoList.Core.Models
{
    /// <summary>
    /// One audit-trail row: a sync pull or a write, with the device that did it.
    /// </summary>
    public class SyncLogEntry
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string Action { get; set; } = string.Empty;   // pull | create | update | toggle | delete | reorder
        public string? EntityType { get; set; }              // TodoList | TodoItem | Category
        public long? EntityId { get; set; }
        public DateTime? SinceValue { get; set; }
        public int? ListsCount { get; set; }
        public int? ItemsCount { get; set; }
        public int? CategoriesCount { get; set; }
        public int? DeletedCount { get; set; }
        public string? Result { get; set; }                  // applied | stale | notfound | ok
        public string? Detail { get; set; }
    }
}
