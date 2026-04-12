using Microsoft.AspNetCore.Mvc;
using ToDoList.Core.Dtos;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryRepository categoryRepository;

        public CategoriesController(CategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
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
            if (category == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(category));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            Core.Models.Category category = await categoryRepository.InsertAsync(request.Name, request.Color);
            CategoryDto dto = MapToDto(category);

            return CreatedAtAction(nameof(GetById), new { id = dto.CategoryId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateCategoryRequest request)
        {
            Core.Models.Category? category = await categoryRepository.UpdateAsync(id, request.Name, request.Color);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(category));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await categoryRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static CategoryDto MapToDto(Core.Models.Category category)
        {
            return new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Color = category.Color,
                CreatedDate = category.CreatedDate,
                ModifiedDate = category.ModifiedDate
            };
        }
    }
}
