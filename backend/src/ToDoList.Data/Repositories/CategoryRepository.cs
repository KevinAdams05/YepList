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
    public class CategoryRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public CategoryRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        private const string SelectColumns =
            "category_id AS CategoryId, name AS Name, color AS Color, version AS Version, " +
            "created_date AS CreatedDate, modified_date AS ModifiedDate, " +
            "client_modified_date AS ClientModifiedDate, is_deleted AS IsDeleted, " +
            "deleted_date AS DeletedDate, deleted_by_device AS DeletedByDevice";

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<Category>(
                $"SELECT {SelectColumns} FROM category WHERE is_deleted = 0 ORDER BY name");
        }

        public async Task<Category?> GetByIdAsync(long categoryId)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QuerySingleOrDefaultAsync<Category>(
                $"SELECT {SelectColumns} FROM category WHERE category_id = @CategoryId",
                new { CategoryId = categoryId });
        }

        public async Task<Category> InsertAsync(string name, string? color, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            long id = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO category (name, color, client_modified_date) " +
                "VALUES (@Name, @Color, @ClientModifiedDate); SELECT LAST_INSERT_ID();",
                new { Name = name, Color = color, ClientModifiedDate = clientModifiedDate });

            return (await GetByIdAsync(id))!;
        }

        public async Task<(WriteOutcome Outcome, Category? Category)> UpdateAsync(
            long categoryId, string name, string? color, DateTime clientModifiedDate)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE category SET name = @Name, color = @Color, " +
                "client_modified_date = @ClientModifiedDate, version = version + 1 " +
                "WHERE category_id = @CategoryId AND is_deleted = 0 " +
                "AND @ClientModifiedDate >= client_modified_date",
                new { CategoryId = categoryId, Name = name, Color = color, ClientModifiedDate = clientModifiedDate });

            Category? current = await GetByIdAsync(categoryId);
            if (current == null)
            {
                return (WriteOutcome.NotFound, null);
            }

            return (rowsAffected > 0 ? WriteOutcome.Applied : WriteOutcome.Stale, current);
        }

        public async Task<bool> DeleteAsync(long categoryId, string? deletedByDevice)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            int rowsAffected = await conn.ExecuteAsync(
                "UPDATE category SET is_deleted = 1, deleted_date = NOW(), " +
                "deleted_by_device = @Device, version = version + 1 " +
                "WHERE category_id = @CategoryId AND is_deleted = 0",
                new { CategoryId = categoryId, Device = deletedByDevice });

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Category>> GetModifiedSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            return await conn.QueryAsync<Category>(
                $"SELECT {SelectColumns} FROM category " +
                "WHERE modified_date > @Since AND is_deleted = 0",
                new { Since = since });
        }

        public async Task<List<long>> GetDeletedIdsSinceAsync(DateTime since)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            IEnumerable<long> results = await conn.QueryAsync<long>(
                "SELECT category_id FROM category WHERE is_deleted = 1 AND deleted_date > @Since",
                new { Since = since });

            return results.ToList();
        }

        public async Task<int> PurgeDeletedOlderThanAsync(int days)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.ExecuteAsync(
                "DELETE FROM category WHERE is_deleted = 1 " +
                "AND deleted_date < DATE_SUB(NOW(), INTERVAL @Days DAY)",
                new { Days = days });
        }
    }
}
