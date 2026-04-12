using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ToDoList.Core.Interfaces;
using ToDoList.Core.Models;

namespace ToDoList.Data.Repositories
{
    public class CategoryRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public CategoryRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<Category>(
                "SELECT category_id AS CategoryId, name AS Name, color AS Color, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM category ORDER BY name");
        }

        public async Task<Category?> GetByIdAsync(long categoryId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<Category>(
                "SELECT category_id AS CategoryId, name AS Name, color AS Color, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM category WHERE category_id = @CategoryId",
                new { CategoryId = categoryId });
        }

        public async Task<Category> InsertAsync(string name, string? color)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO category (name, color) VALUES (@Name, @Color); " +
                "SELECT LAST_INSERT_ID();",
                new { Name = name, Color = color });

            return (await GetByIdAsync(id))!;
        }

        public async Task<Category?> UpdateAsync(long categoryId, string name, string? color)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE category SET name = @Name, color = @Color " +
                "WHERE category_id = @CategoryId",
                new { CategoryId = categoryId, Name = name, Color = color });

            if (rowsAffected == 0)
            {
                return null;
            }

            return await GetByIdAsync(categoryId);
        }

        public async Task<bool> DeleteAsync(long categoryId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM category WHERE category_id = @CategoryId",
                new { CategoryId = categoryId });

            if (rowsAffected > 0)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO deleted_entity (entity_type, entity_id) VALUES ('Category', @EntityId)",
                    new { EntityId = categoryId });
            }

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Category>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<Category>(
                "SELECT category_id AS CategoryId, name AS Name, color AS Color, " +
                "created_date AS CreatedDate, modified_date AS ModifiedDate " +
                "FROM category WHERE modified_date > @Since",
                new { Since = since });
        }
    }
}
