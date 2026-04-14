using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class ReorderItemsRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(500)]
        public List<ReorderEntry> Items { get; set; } = new();
    }

    public class ReorderEntry
    {
        public long ItemId { get; set; }

        public int SortOrder { get; set; }
    }
}
