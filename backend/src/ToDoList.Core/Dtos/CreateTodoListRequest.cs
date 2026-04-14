using System.ComponentModel.DataAnnotations;

namespace ToDoList.Core.Dtos
{
    public class CreateTodoListRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
