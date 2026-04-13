using System.Collections.Generic;

namespace ToDoList.Core.Dtos
{
    public class ReorderItemsRequest
    {
        public List<ReorderEntry> Items { get; set; } = new();
    }

    public class ReorderEntry
    {
        public long ItemId { get; set; }
        public int SortOrder { get; set; }
    }
}
