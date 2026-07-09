using Microsoft.AspNetCore.Mvc;
using ToDoList.Api.Middleware;
using ToDoList.Core.Dtos;
using ToDoList.Core.Models;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoItemRepository itemRepository;
        private readonly TodoListRepository listRepository;
        private readonly SyncLogRepository syncLogRepository;

        public TodoItemsController(
            TodoItemRepository itemRepository,
            TodoListRepository listRepository,
            SyncLogRepository syncLogRepository)
        {
            this.itemRepository = itemRepository;
            this.listRepository = listRepository;
            this.syncLogRepository = syncLogRepository;
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
            if (item == null || item.IsDeleted)
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
                request.CategoryId, request.DueDate, request.SortOrder,
                request.ClientModifiedDate ?? DateTime.UtcNow);
            TodoItemDto dto = MapToDto(item);

            await LogWrite("create", item.ItemId, "applied");
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
                if (targetList == null || targetList.IsDeleted)
                {
                    return BadRequest($"Target list {request.ListId.Value} does not exist.");
                }
            }

            (WriteOutcome outcome, Core.Models.TodoItem? item) = await itemRepository.UpdateAsync(
                id, request.Title, request.Notes, request.CategoryId,
                request.IsCompleted, request.DueDate, request.SortOrder,
                request.ClientModifiedDate ?? DateTime.UtcNow, request.ListId);

            if (outcome == WriteOutcome.NotFound)
            {
                await LogWrite("update", id, "notfound");
                return NotFound();
            }

            // On a stale write the server keeps its newer state; we return it
            // so the client adopts the winning version on the next pull.
            await LogWrite("update", id, outcome == WriteOutcome.Applied ? "applied" : "stale");
            return Ok(MapToDto(item!));
        }

        [HttpPatch("api/items/{id}/complete")]
        public async Task<IActionResult> ToggleComplete(long id, [FromBody] ToggleCompleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            (WriteOutcome outcome, Core.Models.TodoItem? item) = await itemRepository.ToggleCompleteAsync(
                id, request.IsCompleted!.Value, request.ClientModifiedDate ?? DateTime.UtcNow);

            if (outcome == WriteOutcome.NotFound)
            {
                await LogWrite("toggle", id, "notfound");
                return NotFound();
            }

            await LogWrite("toggle", id, outcome == WriteOutcome.Applied ? "applied" : "stale");
            return Ok(MapToDto(item!));
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

            await LogWrite("reorder", listId, "ok");
            return NoContent();
        }

        [HttpDelete("api/items/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await itemRepository.DeleteAsync(id, HttpContext.GetDeviceName());
            if (!deleted)
            {
                await LogWrite("delete", id, "notfound");
                return NotFound();
            }

            await LogWrite("delete", id, "applied");
            return NoContent();
        }

        private Task LogWrite(string action, long entityId, string result)
        {
            return syncLogRepository.InsertAsync(new SyncLogEntry
            {
                DeviceId = HttpContext.GetDeviceId(),
                DeviceName = HttpContext.GetDeviceName(),
                Action = action,
                EntityType = "TodoItem",
                EntityId = entityId,
                Result = result
            });
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
                Version = item.Version,
                CreatedDate = item.CreatedDate,
                ModifiedDate = item.ModifiedDate,
                ClientModifiedDate = item.ClientModifiedDate
            };
        }
    }
}
