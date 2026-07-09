using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class UpdateTodoItemRequest
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(10000)]
        public string? Notes { get; set; }

        public long? CategoryId { get; set; }

        public long? ListId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? DueDate { get; set; }

        public int SortOrder { get; set; }

        /// <summary>
        /// When the user actually made this edit (UTC). Used as the
        /// newest-edit-wins conflict arbiter. Server uses UtcNow if null.
        /// </summary>
        public DateTime? ClientModifiedDate { get; set; }
    }
}
