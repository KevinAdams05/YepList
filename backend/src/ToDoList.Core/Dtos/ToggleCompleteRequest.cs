using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class ToggleCompleteRequest
    {
        [Required]
        public bool? IsCompleted { get; set; }
    }
}
