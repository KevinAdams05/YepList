namespace ToDoList.Core.Dtos
{
    public class CreateTodoListRequest
    {
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
