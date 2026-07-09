using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class CreateTodoListRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        /// <summary>When the user made this change (UTC). Conflict arbiter; server uses UtcNow if null.</summary>
        public DateTime? ClientModifiedDate { get; set; }
    }
}
