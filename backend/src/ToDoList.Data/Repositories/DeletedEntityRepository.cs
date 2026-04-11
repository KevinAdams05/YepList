using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ToDoList.Core.Interfaces;
using ToDoList.Core.Models;

namespace ToDoList.Data.Repositories
{
    public class DeletedEntityRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public DeletedEntityRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<DeletedEntity>> GetDeletedSinceAsync(DateTime since)
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.QueryAsync<DeletedEntity>(
                "SELECT id AS Id, entity_type AS EntityType, entity_id AS EntityId, " +
                "deleted_date AS DeletedDate " +
                "FROM deleted_entity WHERE deleted_date > @Since",
                new { Since = since });
        }

        public async Task<List<long>> GetDeletedIdsSinceAsync(DateTime since, string entityType)
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            var results = await conn.QueryAsync<long>(
                "SELECT entity_id FROM deleted_entity " +
                "WHERE deleted_date > @Since AND entity_type = @EntityType",
                new { Since = since, EntityType = entityType });
            return results.ToList();
        }

        public async Task PurgeOlderThanAsync(int days)
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            await conn.ExecuteAsync(
                "DELETE FROM deleted_entity WHERE deleted_date < DATE_SUB(NOW(), INTERVAL @Days DAY)",
                new { Days = days });
        }
    }
}
