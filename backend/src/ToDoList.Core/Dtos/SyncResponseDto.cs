using System;
using System.Collections.Generic;

namespace ToDoList.Core.Dtos
{
    public class SyncResponseDto
    {
        public DateTime ServerTime { get; set; }
        public List<TodoListDto> Lists { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public List<TodoItemDto> Items { get; set; } = new();
        public List<long> DeletedItemIds { get; set; } = new();
        public List<long> DeletedListIds { get; set; } = new();
        public List<long> DeletedCategoryIds { get; set; } = new();
    }
}
