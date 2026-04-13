using System;
using System.Collections.Generic;
using System.Data;
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
            "due_date AS DueDate, sort_order AS SortOrder, " +
            "created_date AS CreatedDate, modified_date AS ModifiedDate";

        public async Task<IEnumerable<TodoItem>> GetByListIdAsync(long listId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item " +
                "WHERE list_id = @ListId ORDER BY sort_order, title",
                new { ListId = listId });
        }

        public async Task<TodoItem?> GetByIdAsync(long itemId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item WHERE item_id = @ItemId",
                new { ItemId = itemId });
        }

        public async Task<TodoItem> InsertAsync(long listId, string title, string? notes,
            long? categoryId, DateTime? dueDate, int sortOrder)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO todo_item (list_id, title, notes, category_id, due_date, sort_order) " +
                "VALUES (@ListId, @Title, @Notes, @CategoryId, @DueDate, @SortOrder); " +
                "SELECT LAST_INSERT_ID();",
                new
                {
                    ListId = listId,
                    Title = title,
                    Notes = notes,
                    CategoryId = categoryId,
                    DueDate = dueDate,
                    SortOrder = sortOrder
                });

            return (await GetByIdAsync(id))!;
        }

        public async Task<TodoItem?> UpdateAsync(long itemId, string title, string? notes,
            long? categoryId, bool isCompleted, DateTime? dueDate, int sortOrder)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_item SET title = @Title, notes = @Notes, " +
                "category_id = @CategoryId, is_completed = @IsCompleted, " +
                "due_date = @DueDate, sort_order = @SortOrder " +
                "WHERE item_id = @ItemId",
                new
                {
                    ItemId = itemId,
                    Title = title,
                    Notes = notes,
                    CategoryId = categoryId,
                    IsCompleted = isCompleted,
                    DueDate = dueDate,
                    SortOrder = sortOrder
                });

            if (rowsAffected == 0)
            {
                return null;
            }

            return await GetByIdAsync(itemId);
        }

        public async Task<TodoItem?> ToggleCompleteAsync(long itemId, bool isCompleted)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_item SET is_completed = @IsCompleted WHERE item_id = @ItemId",
                new { ItemId = itemId, IsCompleted = isCompleted });

            if (rowsAffected == 0)
            {
                return null;
            }

            return await GetByIdAsync(itemId);
        }

        public async Task<bool> DeleteAsync(long itemId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM todo_item WHERE item_id = @ItemId",
                new { ItemId = itemId });

            if (rowsAffected > 0)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO deleted_entity (entity_type, entity_id) VALUES ('TodoItem', @EntityId)",
                    new { EntityId = itemId });
            }

            return rowsAffected > 0;
        }

        public async Task ReorderAsync(IEnumerable<(long ItemId, int SortOrder)> items)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            foreach (var item in items)
            {
                await conn.ExecuteAsync(
                    "UPDATE todo_item SET sort_order = @SortOrder WHERE item_id = @ItemId",
                    new { item.ItemId, item.SortOrder });
            }
        }

        public async Task<IEnumerable<TodoItem>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoItem>(
                $"SELECT {SelectColumns} FROM todo_item WHERE modified_date > @Since",
                new { Since = since });
        }
    }
}
