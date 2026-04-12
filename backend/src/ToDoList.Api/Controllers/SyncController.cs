using Microsoft.AspNetCore.Mvc;
using ToDoList.Core.Dtos;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/sync")]
    public class SyncController : ControllerBase
    {
        private readonly TodoListRepository listRepository;
        private readonly TodoItemRepository itemRepository;
        private readonly CategoryRepository categoryRepository;
        private readonly DeletedEntityRepository deletedEntityRepository;

        public SyncController(
            TodoListRepository listRepository,
            TodoItemRepository itemRepository,
            CategoryRepository categoryRepository,
            DeletedEntityRepository deletedEntityRepository)
        {
            this.listRepository = listRepository;
            this.itemRepository = itemRepository;
            this.categoryRepository = categoryRepository;
            this.deletedEntityRepository = deletedEntityRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Sync([FromQuery] DateTime? since)
        {
            DateTime sinceDate = since ?? DateTime.MinValue;

            IEnumerable<Core.Models.TodoList> lists = await listRepository.GetModifiedSinceAsync(sinceDate);
            IEnumerable<Core.Models.TodoItem> items = await itemRepository.GetModifiedSinceAsync(sinceDate);
            IEnumerable<Core.Models.Category> categories = await categoryRepository.GetModifiedSinceAsync(sinceDate);

            List<long> deletedListIds = await deletedEntityRepository.GetDeletedIdsSinceAsync(sinceDate, "TodoList");
            List<long> deletedItemIds = await deletedEntityRepository.GetDeletedIdsSinceAsync(sinceDate, "TodoItem");
            List<long> deletedCategoryIds = await deletedEntityRepository.GetDeletedIdsSinceAsync(sinceDate, "Category");

            SyncResponseDto response = new SyncResponseDto
            {
                ServerTime = DateTime.UtcNow,
                Lists = lists.Select(l => new TodoListDto
                {
                    ListId = l.ListId,
                    Name = l.Name,
                    SortOrder = l.SortOrder,
                    CreatedDate = l.CreatedDate,
                    ModifiedDate = l.ModifiedDate
                }).ToList(),
                Categories = categories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Color = c.Color,
                    CreatedDate = c.CreatedDate,
                    ModifiedDate = c.ModifiedDate
                }).ToList(),
                Items = items.Select(i => new TodoItemDto
                {
                    ItemId = i.ItemId,
                    ListId = i.ListId,
                    CategoryId = i.CategoryId,
                    Title = i.Title,
                    Notes = i.Notes,
                    IsCompleted = i.IsCompleted,
                    DueDate = i.DueDate,
                    SortOrder = i.SortOrder,
                    CreatedDate = i.CreatedDate,
                    ModifiedDate = i.ModifiedDate
                }).ToList(),
                DeletedListIds = deletedListIds,
                DeletedItemIds = deletedItemIds,
                DeletedCategoryIds = deletedCategoryIds
            };

            return Ok(response);
        }
    }
}
