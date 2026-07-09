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
    public class TodoListRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public TodoListRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        private const string SelectColumns =
            "list_id AS ListId, name AS Name, sort_order AS SortOrder, version AS Version, " +
            "created_date AS CreatedDate, modified_date AS ModifiedDate, " +
            "client_modified_date AS ClientModifiedDate, is_deleted AS IsDeleted, " +
            "deleted_date AS DeletedDate, deleted_by_device AS DeletedByDevice";

        public async Task<IEnumerable<TodoList>> GetAllAsync()
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoList>(
                $"SELECT {SelectColumns} FROM todo_list WHERE is_deleted = 0 ORDER BY sort_order, name");
        }

        public async Task<TodoList?> GetByIdAsync(long listId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<TodoList>(
                $"SELECT {SelectColumns} FROM todo_list WHERE list_id = @ListId",
                new { ListId = listId });
        }

        public async Task<TodoList> InsertAsync(string name, int sortOrder, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO todo_list (name, sort_order, client_modified_date) " +
                "VALUES (@Name, @SortOrder, @ClientModifiedDate); SELECT LAST_INSERT_ID();",
                new { Name = name, SortOrder = sortOrder, ClientModifiedDate = clientModifiedDate });

            return (await GetByIdAsync(id))!;
        }

        public async Task<(WriteOutcome Outcome, TodoList? List)> UpdateAsync(
            long listId, string name, int sortOrder, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_list SET name = @Name, sort_order = @SortOrder, " +
                "client_modified_date = @ClientModifiedDate, version = version + 1 " +
                "WHERE list_id = @ListId AND is_deleted = 0 " +
                "AND @ClientModifiedDate >= client_modified_date",
                new { ListId = listId, Name = name, SortOrder = sortOrder, ClientModifiedDate = clientModifiedDate });

            TodoList? current = await GetByIdAsync(listId);
            if (current == null)
            {
                return (WriteOutcome.NotFound, null);
            }

            return (rowsAffected > 0 ? WriteOutcome.Applied : WriteOutcome.Stale, current);
        }

        public async Task<bool> DeleteAsync(long listId, string? deletedByDevice)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_list SET is_deleted = 1, deleted_date = NOW(), " +
                "deleted_by_device = @Device, version = version + 1 " +
                "WHERE list_id = @ListId AND is_deleted = 0",
                new { ListId = listId, Device = deletedByDevice });

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<TodoList>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoList>(
                $"SELECT {SelectColumns} FROM todo_list " +
                "WHERE modified_date > @Since AND is_deleted = 0",
                new { Since = since });
        }

        public async Task<List<long>> GetDeletedIdsSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            IEnumerable<long> results = await conn.QueryAsync<long>(
                "SELECT list_id FROM todo_list WHERE is_deleted = 1 AND deleted_date > @Since",
                new { Since = since });

            return results.ToList();
        }

        public async Task<int> PurgeDeletedOlderThanAsync(int days)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.ExecuteAsync(
                "DELETE FROM todo_list WHERE is_deleted = 1 " +
                "AND deleted_date < DATE_SUB(NOW(), INTERVAL @Days DAY)",
                new { Days = days });
        }
    }
}
