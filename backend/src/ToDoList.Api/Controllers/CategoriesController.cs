using Microsoft.AspNetCore.Mvc;
using ToDoList.Api.Middleware;
using ToDoList.Core.Dtos;
using ToDoList.Core.Models;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryRepository categoryRepository;
        private readonly SyncLogRepository syncLogRepository;

        public CategoriesController(CategoryRepository categoryRepository, SyncLogRepository syncLogRepository)
        {
            this.categoryRepository = categoryRepository;
            this.syncLogRepository = syncLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<Core.Models.Category> categories = await categoryRepository.GetAllAsync();
            IEnumerable<CategoryDto> dtos = categories.Select(c => MapToDto(c));

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            Core.Models.Category? category = await categoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
            {
                return NotFound();
            }

            return Ok(MapToDto(category));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Core.Models.Category category = await categoryRepository.InsertAsync(
                request.Name, request.Color, request.ClientModifiedDate ?? DateTime.UtcNow);
            CategoryDto dto = MapToDto(category);

            await LogWrite("create", category.CategoryId, "applied");
            return CreatedAtAction(nameof(GetById), new { id = dto.CategoryId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            (WriteOutcome outcome, Core.Models.Category? category) = await categoryRepository.UpdateAsync(
                id, request.Name, request.Color, request.ClientModifiedDate ?? DateTime.UtcNow);

            if (outcome == WriteOutcome.NotFound)
            {
                await LogWrite("update", id, "notfound");
                return NotFound();
            }

            await LogWrite("update", id, outcome == WriteOutcome.Applied ? "applied" : "stale");
            return Ok(MapToDto(category!));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await categoryRepository.DeleteAsync(id, HttpContext.GetDeviceName());
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
                EntityType = "Category",
                EntityId = entityId,
                Result = result
            });
        }

        private static CategoryDto MapToDto(Core.Models.Category category)
        {
            return new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Color = category.Color,
                Version = category.Version,
                CreatedDate = category.CreatedDate,
                ModifiedDate = category.ModifiedDate,
                ClientModifiedDate = category.ClientModifiedDate
            };
        }
    }
}
