using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ToDoList.Windows.Models;

namespace ToDoList.Windows.ApiClient
{
    public class TodoApiClient
    {
        private readonly HttpClient httpClient;
        private DateTime lastSyncTime = DateTime.MinValue;

        public TodoApiClient(string baseUrl, string deviceId, string deviceName)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
            };

            // Identify this device on every request so the server can attribute
            // sync activity and deletions to it (see DeviceTrackingMiddleware).
            httpClient.DefaultRequestHeaders.Add("X-Device-Id", deviceId);
            httpClient.DefaultRequestHeaders.Add("X-Device-Name", deviceName);
            httpClient.DefaultRequestHeaders.Add("X-Device-Platform", "Windows");
        }

        public string BaseUrl => httpClient.BaseAddress?.ToString() ?? "";

        // ── Lists ─────────────────────────────────────────────

        public async Task<List<TodoList>> GetListsAsync()
        {
            List<TodoList>? result = await httpClient.GetFromJsonAsync<List<TodoList>>("api/lists");

            return result ?? new List<TodoList>();
        }

        public async Task<TodoList> CreateListAsync(string name, int sortOrder = 0)
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/lists",
                new { name, sortOrder, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TodoList>())!;
        }

        public async Task<TodoList> UpdateListAsync(long listId, string name, int sortOrder = 0)
        {
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"api/lists/{listId}",
                new { name, sortOrder, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TodoList>())!;
        }

        public async Task DeleteListAsync(long listId)
        {
            HttpResponseMessage response = await httpClient.DeleteAsync($"api/lists/{listId}");
            response.EnsureSuccessStatusCode();
        }

        // ── Categories ────────────────────────────────────────

        public async Task<List<Category>> GetCategoriesAsync()
        {
            List<Category>? result = await httpClient.GetFromJsonAsync<List<Category>>("api/categories");

            return result ?? new List<Category>();
        }

        public async Task<Category> CreateCategoryAsync(string name, string? color)
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/categories",
                new { name, color, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<Category>())!;
        }

        public async Task<Category> UpdateCategoryAsync(long categoryId, string name, string? color)
        {
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"api/categories/{categoryId}",
                new { name, color, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<Category>())!;
        }

        public async Task DeleteCategoryAsync(long categoryId)
        {
            HttpResponseMessage response = await httpClient.DeleteAsync($"api/categories/{categoryId}");
            response.EnsureSuccessStatusCode();
        }

        // ── Items ─────────────────────────────────────────────

        public async Task<List<TodoItem>> GetItemsByListAsync(long listId)
        {
            List<TodoItem>? result = await httpClient.GetFromJsonAsync<List<TodoItem>>($"api/lists/{listId}/items");

            return result ?? new List<TodoItem>();
        }

        public async Task<TodoItem> CreateItemAsync(long listId, string title, string? notes,
            long? categoryId, DateTime? dueDate, int sortOrder = 0)
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync($"api/lists/{listId}/items",
                new { title, notes, categoryId, dueDate, sortOrder, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
        }

        public async Task<TodoItem> UpdateItemAsync(long itemId, string title, string? notes,
            long? categoryId, bool isCompleted, DateTime? dueDate, int sortOrder = 0,
            long? listId = null)
        {
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"api/items/{itemId}",
                new { title, notes, categoryId, listId, isCompleted, dueDate, sortOrder, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
        }

        public async Task<TodoItem> ToggleCompleteAsync(long itemId, bool isCompleted)
        {
            HttpResponseMessage response = await httpClient.PatchAsJsonAsync($"api/items/{itemId}/complete",
                new { isCompleted, clientModifiedDate = DateTime.UtcNow });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
        }

        public async Task DeleteItemAsync(long itemId)
        {
            HttpResponseMessage response = await httpClient.DeleteAsync($"api/items/{itemId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task ReorderItemsAsync(long listId, List<(long ItemId, int SortOrder)> items)
        {
            var body = new
            {
                items = items.Select(i => new { itemId = i.ItemId, sortOrder = i.SortOrder }).ToList()
            };
            HttpResponseMessage response = await httpClient.PutAsJsonAsync(
                $"api/lists/{listId}/items/reorder", body);
            response.EnsureSuccessStatusCode();
        }

        // ── Sync ──────────────────────────────────────────────

        public async Task<SyncResponse> SyncAsync()
        {
            string sinceParam = lastSyncTime == DateTime.MinValue
                ? ""
                : $"?since={lastSyncTime:O}";

            SyncResponse? result = await httpClient.GetFromJsonAsync<SyncResponse>($"api/sync{sinceParam}");
            if (result != null)
            {
                lastSyncTime = result.ServerTime;
            }

            return result ?? new SyncResponse();
        }

        public void ResetSyncTime()
        {
            lastSyncTime = DateTime.MinValue;
        }
    }
}
