using Microsoft.AspNetCore.Mvc;
using ToDoList.Api.Middleware;
using ToDoList.Core.Dtos;
using ToDoList.Core.Models;
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
        private readonly SyncLogRepository syncLogRepository;

        public SyncController(
            TodoListRepository listRepository,
            TodoItemRepository itemRepository,
            CategoryRepository categoryRepository,
            SyncLogRepository syncLogRepository)
        {
            this.listRepository = listRepository;
            this.itemRepository = itemRepository;
            this.categoryRepository = categoryRepository;
            this.syncLogRepository = syncLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Sync([FromQuery] DateTime? since)
        {
            DateTime sinceDate = since ?? DateTime.MinValue;

            IEnumerable<Core.Models.TodoList> lists = await listRepository.GetModifiedSinceAsync(sinceDate);
            IEnumerable<Core.Models.TodoItem> items = await itemRepository.GetModifiedSinceAsync(sinceDate);
            IEnumerable<Core.Models.Category> categories = await categoryRepository.GetModifiedSinceAsync(sinceDate);

            // Deletions now come from the in-place is_deleted flag rather than
            // the legacy deleted_entity tombstone table.
            List<long> deletedListIds = await listRepository.GetDeletedIdsSinceAsync(sinceDate);
            List<long> deletedItemIds = await itemRepository.GetDeletedIdsSinceAsync(sinceDate);
            List<long> deletedCategoryIds = await categoryRepository.GetDeletedIdsSinceAsync(sinceDate);

            SyncResponseDto response = new SyncResponseDto
            {
                ServerTime = DateTime.UtcNow,
                Lists = lists.Select(l => new TodoListDto
                {
                    ListId = l.ListId,
                    Name = l.Name,
                    SortOrder = l.SortOrder,
                    Version = l.Version,
                    CreatedDate = l.CreatedDate,
                    ModifiedDate = l.ModifiedDate,
                    ClientModifiedDate = l.ClientModifiedDate
                }).ToList(),
                Categories = categories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Color = c.Color,
                    Version = c.Version,
                    CreatedDate = c.CreatedDate,
                    ModifiedDate = c.ModifiedDate,
                    ClientModifiedDate = c.ClientModifiedDate
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
                    Version = i.Version,
                    CreatedDate = i.CreatedDate,
                    ModifiedDate = i.ModifiedDate,
                    ClientModifiedDate = i.ClientModifiedDate
                }).ToList(),
                DeletedListIds = deletedListIds,
                DeletedItemIds = deletedItemIds,
                DeletedCategoryIds = deletedCategoryIds
            };

            int deletedCount = deletedListIds.Count + deletedItemIds.Count + deletedCategoryIds.Count;
            await syncLogRepository.InsertAsync(new SyncLogEntry
            {
                DeviceId = HttpContext.GetDeviceId(),
                DeviceName = HttpContext.GetDeviceName(),
                Action = "pull",
                SinceValue = since,
                ListsCount = response.Lists.Count,
                ItemsCount = response.Items.Count,
                CategoriesCount = response.Categories.Count,
                DeletedCount = deletedCount,
                Result = "ok"
            });

            return Ok(response);
        }
    }
}
