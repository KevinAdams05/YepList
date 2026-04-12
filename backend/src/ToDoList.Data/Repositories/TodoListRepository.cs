using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task<IEnumerable<TodoList>> GetAllAsync()
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoList>(
                "SELECT list_id AS ListId, name AS Name, sort_order AS SortOrder, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM todo_list ORDER BY sort_order, name");
        }

        public async Task<TodoList?> GetByIdAsync(long listId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<TodoList>(
                "SELECT list_id AS ListId, name AS Name, sort_order AS SortOrder, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM todo_list WHERE list_id = @ListId",
                new { ListId = listId });
        }

        public async Task<TodoList> InsertAsync(string name, int sortOrder)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO todo_list (name, sort_order) VALUES (@Name, @SortOrder); " +
                "SELECT LAST_INSERT_ID();",
                new { Name = name, SortOrder = sortOrder });

            return (await GetByIdAsync(id))!;
        }

        public async Task<TodoList?> UpdateAsync(long listId, string name, int sortOrder)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE todo_list SET name = @Name, sort_order = @SortOrder " +
                "WHERE list_id = @ListId",
                new { ListId = listId, Name = name, SortOrder = sortOrder });

            if (rowsAffected == 0)
            {
                return null;
            }

            return await GetByIdAsync(listId);
        }

        public async Task<bool> DeleteAsync(long listId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM todo_list WHERE list_id = @ListId",
                new { ListId = listId });

            if (rowsAffected > 0)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO deleted_entity (entity_type, entity_id) VALUES ('TodoList', @EntityId)",
                    new { EntityId = listId });
            }

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<TodoList>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<TodoList>(
                "SELECT list_id AS ListId, name AS Name, sort_order AS SortOrder, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM todo_list WHERE modified_date > @Since",
                new { Since = since });
        }
    }
}
