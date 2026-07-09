using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class ToggleCompleteRequest
    {
        [Required]
        public bool? IsCompleted { get; set; }

        /// <summary>When the user toggled this (UTC). Conflict arbiter; server uses UtcNow if null.</summary>
        public DateTime? ClientModifiedDate { get; set; }
    }
}
