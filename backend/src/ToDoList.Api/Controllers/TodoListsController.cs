using Microsoft.AspNetCore.Mvc;
using ToDoList.Api.Middleware;
using ToDoList.Core.Dtos;
using ToDoList.Core.Models;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/lists")]
    public class TodoListsController : ControllerBase
    {
        private readonly TodoListRepository listRepository;
        private readonly SyncLogRepository syncLogRepository;

        public TodoListsController(TodoListRepository listRepository, SyncLogRepository syncLogRepository)
        {
            this.listRepository = listRepository;
            this.syncLogRepository = syncLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<Core.Models.TodoList> lists = await listRepository.GetAllAsync();
            IEnumerable<TodoListDto> dtos = lists.Select(l => MapToDto(l));

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            Core.Models.TodoList? list = await listRepository.GetByIdAsync(id);
            if (list == null || list.IsDeleted)
            {
                return NotFound();
            }

            return Ok(MapToDto(list));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTodoListRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Core.Models.TodoList list = await listRepository.InsertAsync(
                request.Name, request.SortOrder, request.ClientModifiedDate ?? DateTime.UtcNow);
            TodoListDto dto = MapToDto(list);

            await LogWrite("create", list.ListId, "applied");
            return CreatedAtAction(nameof(GetById), new { id = dto.ListId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateTodoListRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            (WriteOutcome outcome, Core.Models.TodoList? list) = await listRepository.UpdateAsync(
                id, request.Name, request.SortOrder, request.ClientModifiedDate ?? DateTime.UtcNow);

            if (outcome == WriteOutcome.NotFound)
            {
                await LogWrite("update", id, "notfound");
                return NotFound();
            }

            await LogWrite("update", id, outcome == WriteOutcome.Applied ? "applied" : "stale");
            return Ok(MapToDto(list!));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await listRepository.DeleteAsync(id, HttpContext.GetDeviceName());
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
                EntityType = "TodoList",
                EntityId = entityId,
                Result = result
            });
        }

        private static TodoListDto MapToDto(Core.Models.TodoList list)
        {
            return new TodoListDto
            {
                ListId = list.ListId,
                Name = list.Name,
                SortOrder = list.SortOrder,
                Version = list.Version,
                CreatedDate = list.CreatedDate,
                ModifiedDate = list.ModifiedDate,
                ClientModifiedDate = list.ClientModifiedDate
            };
        }
    }
}
