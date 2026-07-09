using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ToDoList.Core.Interfaces;
using ToDoList.Core.Models;

namespace ToDoList.Data.Repositories
{
    public class TodoItemRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public TodoItemRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        private const string SelectColumns =
            "item_id AS ItemId, list_id AS ListId, category_id AS CategoryId, " +
            "title AS Title, notes AS Notes, is_completed AS IsCompleted, " +
            "due_date AS DueDate, sort_order AS SortOrder, version AS Version, " +
            "created_date AS CreatedDate, modified_date AS ModifiedDate, " +
            "client_modified_date AS ClientModifiedDate, is_deleted AS IsDeleted, " +
            "deleted_date AS DeletedDate, deleted_by_device AS DeletedByDevice";

        public async Task<IEnumerable<TodoItem>> GetByListIdAsync(long listId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item " +
                "WHERE list_id = @ListId AND is_deleted = 0 ORDER BY sort_order, title",
                new { ListId = listId });
        }

        // Returns the row regardless of is_deleted — callers need the current
        // server state to classify a write outcome (applied/stale/notfound).
        public async Task<TodoItem?> GetByIdAsync(long itemId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item WHERE item_id = @ItemId",
                new { ItemId = itemId });
        }

        public async Task<TodoItem> InsertAsync(long listId, string title, string? notes,
            long? categoryId, DateTime? dueDate, int sortOrder, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO todo_item (list_id, title, notes, category_id, due_date, sort_order, client_modified_date) " +
                "VALUES (@ListId, @Title, @Notes, @CategoryId, @DueDate, @SortOrder, @ClientModifiedDate); " +
                "SELECT LAST_INSERT_ID();",
                new
                {
                    ListId = listId,
                    Title = title,
                    Notes = notes,
                    CategoryId = categoryId,
                    DueDate = dueDate,
                    SortOrder = sortOrder,
                    ClientModifiedDate = clientModifiedDate
                });

            return (await GetByIdAsync(id))!;
        }

        // Newest-edit-wins: the write applies only if its client edit time is
        // at least as new as the stored one. A stale edit pushed late is
        // ignored, so it can't clobber a newer edit from another device.
        public async Task<(WriteOutcome Outcome, TodoItem? Item)> UpdateAsync(long itemId,
            string title, string? notes, long? categoryId, bool isCompleted,
            DateTime? dueDate, int sortOrder, DateTime clientModifiedDate, long? listId = null)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            string sql = "UPDATE todo_item SET title = @Title, notes = @Notes, " +
                "category_id = @CategoryId, is_completed = @IsCompleted, " +
                "due_date = @DueDate, sort_order = @SortOrder, " +
                "client_modified_date = @ClientModifiedDate, version = version + 1";

            if (listId.HasValue)
            {
                sql += ", list_id = @ListId";
            }

            sql += " WHERE item_id = @ItemId AND is_deleted = 0 " +
                "AND @ClientModifiedDate >= client_modified_date";

            int rowsAffected = await conn.ExecuteAsync(sql,
                new
                {
                    ItemId = itemId,
                    Title = title,
                    Notes = notes,
                    CategoryId = categoryId,
                    IsCompleted = isCompleted,
                    DueDate = dueDate,
                    SortOrder = sortOrder,
                    ClientModifiedDate = clientModifiedDate,
                    ListId = listId
                });

            return await ClassifyAsync(itemId, rowsAffected);
        }

        public async Task<(WriteOutcome Outcome, TodoItem? Item)> ToggleCompleteAsync(
            long itemId, bool isCompleted, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_item SET is_completed = @IsCompleted, " +
                "client_modified_date = @ClientModifiedDate, version = version + 1 " +
                "WHERE item_id = @ItemId AND is_deleted = 0 " +
                "AND @ClientModifiedDate >= client_modified_date",
                new { ItemId = itemId, IsCompleted = isCompleted, ClientModifiedDate = clientModifiedDate });

            return await ClassifyAsync(itemId, rowsAffected);
        }

        // Soft delete: the row is retained and flagged, recording which device
        // deleted it and when, so the deletion can propagate to other devices
        // and be audited.
        public async Task<bool> DeleteAsync(long itemId, string? deletedByDevice)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_item SET is_deleted = 1, deleted_date = NOW(), " +
                "deleted_by_device = @Device, version = version + 1 " +
                "WHERE item_id = @ItemId AND is_deleted = 0",
                new { ItemId = itemId, Device = deletedByDevice });

            return rowsAffected > 0;
        }

        public async Task ReorderAsync(IEnumerable<(long ItemId, int SortOrder)> items)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            foreach (var item in items)
            {
                await conn.ExecuteAsync(
                    "UPDATE todo_item SET sort_order = @SortOrder WHERE item_id = @ItemId AND is_deleted = 0",
                    new { item.ItemId, item.SortOrder });
            }
        }

        public async Task<IEnumerable<TodoItem>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item " +
                "WHERE modified_date > @Since AND is_deleted = 0",
                new { Since = since });
        }

        public async Task<List<long>> GetDeletedIdsSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            IEnumerable<long> results = await conn.QueryAsync<long>(
                "SELECT item_id FROM todo_item WHERE is_deleted = 1 AND deleted_date > @Since",
                new { Since = since });

            return results.ToList();
        }

        // Final purge of long-since soft-deleted rows (retention policy).
        public async Task<int> PurgeDeletedOlderThanAsync(int days)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.ExecuteAsync(
                "DELETE FROM todo_item WHERE is_deleted = 1 " +
                "AND deleted_date < DATE_SUB(NOW(), INTERVAL @Days DAY)",
                new { Days = days });
        }

        private async Task<(WriteOutcome, TodoItem?)> ClassifyAsync(long itemId, int rowsAffected)
        {
            TodoItem? current = await GetByIdAsync(itemId);
            if (current == null)
            {
                return (WriteOutcome.NotFound, null);
            }

            return (rowsAffected > 0 ? WriteOutcome.Applied : WriteOutcome.Stale, current);
        }
    }
}
