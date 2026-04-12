using Microsoft.AspNetCore.Mvc;
using ToDoList.Core.Dtos;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/lists")]
    public class TodoListsController : ControllerBase
    {
        private readonly TodoListRepository listRepository;

        public TodoListsController(TodoListRepository listRepository)
        {
            this.listRepository = listRepository;
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
            if (list == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(list));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTodoListRequest request)
        {
            Core.Models.TodoList list = await listRepository.InsertAsync(request.Name, request.SortOrder);
            TodoListDto dto = MapToDto(list);

            return CreatedAtAction(nameof(GetById), new { id = dto.ListId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateTodoListRequest request)
        {
            Core.Models.TodoList? list = await listRepository.UpdateAsync(id, request.Name, request.SortOrder);
            if (list == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(list));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await listRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static TodoListDto MapToDto(Core.Models.TodoList list)
        {
            return new TodoListDto
            {
                ListId = list.ListId,
                Name = list.Name,
                SortOrder = list.SortOrder,
                CreatedDate = list.CreatedDate,
                ModifiedDate = list.ModifiedDate
            };
        }
    }
}
