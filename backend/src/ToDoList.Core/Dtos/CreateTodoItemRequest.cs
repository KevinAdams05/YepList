using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class CreateTodoItemRequest
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(10000)]
        public string? Notes { get; set; }

        public long? CategoryId { get; set; }

        public DateTime? DueDate { get; set; }

        public int SortOrder { get; set; }
    }
}
