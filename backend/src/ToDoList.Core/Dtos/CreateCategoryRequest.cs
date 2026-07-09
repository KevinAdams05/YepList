using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class CreateCategoryRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a hex code like #FF5733")]
        public string? Color { get; set; }

        /// <summary>When the user made this change (UTC). Conflict arbiter; server uses UtcNow if null.</summary>
        public DateTime? ClientModifiedDate { get; set; }
    }
}
