using Microsoft.AspNetCore.Mvc;
using ToDoList.Core.Dtos;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoItemRepository itemRepository;
        private readonly TodoListRepository listRepository;

        public TodoItemsController(
            TodoItemRepository itemRepository,
            TodoListRepository listRepository)
        {
            this.itemRepository = itemRepository;
            this.listRepository = listRepository;
        }

        [HttpGet("api/lists/{listId}/items")]
        public async Task<IActionResult> GetByList(long listId)
        {
            IEnumerable<Core.Models.TodoItem> items = await itemRepository.GetByListIdAsync(listId);
            IEnumerable<TodoItemDto> dtos = items.Select(i => MapToDto(i));

            return Ok(dtos);
        }

        [HttpGet("api/items/{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            Core.Models.TodoItem? item = await itemRepository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(item));
        }

        [HttpPost("api/lists/{listId}/items")]
        public async Task<IActionResult> Create(long listId, [FromBody] CreateTodoItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Core.Models.TodoItem item = await itemRepository.InsertAsync(
                listId, request.Title, request.Notes,
                request.CategoryId, request.DueDate, request.SortOrder);
            TodoItemDto dto = MapToDto(item);

            return CreatedAtAction(nameof(GetById), new { id = dto.ItemId }, dto);
        }

        [HttpPut("api/items/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateTodoItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // If the caller is reparenting the item, confirm the target
            // list exists. Without this we'd surface a raw FK violation
            // from MySQL as a 500.
            if (request.ListId.HasValue)
            {
                Core.Models.TodoList? targetList = await listRepository.GetByIdAsync(request.ListId.Value);
                if (targetList == null)
                {
                    return BadRequest($"Target list {request.ListId.Value} does not exist.");
                }
            }

            Core.Models.TodoItem? item = await itemRepository.UpdateAsync(
                id, request.Title, request.Notes, request.CategoryId,
                request.IsCompleted, request.DueDate, request.SortOrder,
                request.ListId);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(item));
        }

        [HttpPatch("api/items/{id}/complete")]
        public async Task<IActionResult> ToggleComplete(long id, [FromBody] ToggleCompleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Core.Models.TodoItem? item = await itemRepository.ToggleCompleteAsync(id, request.IsCompleted!.Value);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(item));
        }

        [HttpPut("api/lists/{listId}/items/reorder")]
        public async Task<IActionResult> Reorder(long listId, [FromBody] ReorderItemsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entries = request.Items.Select(e => (e.ItemId, e.SortOrder));
            await itemRepository.ReorderAsync(entries);

            return NoContent();
        }

        [HttpDelete("api/items/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await itemRepository.DeleteAsync(id);
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
