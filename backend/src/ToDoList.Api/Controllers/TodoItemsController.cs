using Microsoft.AspNetCore.Mvc;
using ToDoList.Core.Dtos;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoItemRepository itemRepository;

        public TodoItemsController(TodoItemRepository itemRepository)
        {
            this.itemRepository = itemRepository;
        }

        [HttpGet("api/lists/{listId}/items")]
        public async Task<IActionResult> GetByList(long listId)
        {
            var items = await itemRepository.GetByListIdAsync(listId);
            var dtos = items.Select(i => MapToDto(i));
            return Ok(dtos);
        }

        [HttpGet("api/items/{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await itemRepository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(item));
        }

        [HttpPost("api/lists/{listId}/items")]
        public async Task<IActionResult> Create(long listId, [FromBody] CreateTodoItemRequest request)
        {
            var item = await itemRepository.InsertAsync(
                listId, request.Title, request.Notes,
                request.CategoryId, request.DueDate, request.SortOrder);
            var dto = MapToDto(item);
            return CreatedAtAction(nameof(GetById), new { id = dto.ItemId }, dto);
        }

        [HttpPut("api/items/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateTodoItemRequest request)
        {
            var item = await itemRepository.UpdateAsync(
                id, request.Title, request.Notes, request.CategoryId,
                request.IsCompleted, request.DueDate, request.SortOrder);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(item));
        }

        [HttpPatch("api/items/{id}/complete")]
        public async Task<IActionResult> ToggleComplete(long id, [FromBody] ToggleCompleteRequest request)
        {
            var item = await itemRepository.ToggleCompleteAsync(id, request.IsCompleted);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(item));
        }

        [HttpDelete("api/items/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await itemRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        private static TodoItemDto MapToDto(Core.Models.TodoItem item)
        {
            return new TodoItemDto
            {
                ItemId = item.ItemId,
                ListId = item.ListId,
                CategoryId = item.CategoryId,
                Title = item.Title,
                Notes = item.Notes,
                IsCompleted = item.IsCompleted,
                DueDate = item.DueDate,
                SortOrder = item.SortOrder,
                CreatedDate = item.CreatedDate,
                ModifiedDate = item.ModifiedDate
            };
        }
    }
}
