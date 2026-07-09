using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ToDoList.Core.Interfaces;
using ToDoList.Core.Models;

namespace ToDoList.Data.Repositories
{
    public class SyncLogRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public SyncLogRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        // Best-effort audit write: a logging failure must never break a real
        // sync/mutation, so any error is swallowed.
        public async Task InsertAsync(SyncLogEntry entry)
        {
            try
            {
                using IDbConnection conn = connectionFactory.CreateConnection();
                conn.Open();
                await conn.ExecuteAsync(
                    "INSERT INTO sync_log (device_id, device_name, action, entity_type, entity_id, " +
                    "since_value, lists_count, items_count, categories_count, deleted_count, result, detail) " +
                    "VALUES (@DeviceId, @DeviceName, @Action, @EntityType, @EntityId, " +
                    "@SinceValue, @ListsCount, @ItemsCount, @CategoriesCount, @DeletedCount, @Result, @Detail)",
                    entry);
            }
            catch
            {
                // Intentionally ignored — audit logging is non-critical.
            }
        }

        // Filtered read for the log viewer. Filters are optional; null means
        // "no filter on this field".
        public async Task<IEnumerable<SyncLogEntry>> QueryAsync(
            string? deviceId, string? action, DateTime? from, DateTime? to, int limit)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();

            StringBuilder sql = new(
                "SELECT id AS Id, created_at AS CreatedAt, device_id AS DeviceId, device_name AS DeviceName, " +
                "action AS Action, entity_type AS EntityType, entity_id AS EntityId, since_value AS SinceValue, " +
                "lists_count AS ListsCount, items_count AS ItemsCount, categories_count AS CategoriesCount, " +
                "deleted_count AS DeletedCount, result AS Result, detail AS Detail FROM sync_log WHERE 1 = 1");

            if (!string.IsNullOrEmpty(deviceId))
            {
                sql.Append(" AND device_id = @DeviceId");
            }
            if (!string.IsNullOrEmpty(action))
            {
                sql.Append(" AND action = @Action");
            }
            if (from.HasValue)
            {
                sql.Append(" AND created_at >= @From");
            }
            if (to.HasValue)
            {
                sql.Append(" AND created_at <= @To");
            }
            sql.Append(" ORDER BY id DESC LIMIT @Limit");

            return await conn.QueryAsync<SyncLogEntry>(sql.ToString(),
                new { DeviceId = deviceId, Action = action, From = from, To = to, Limit = limit });
        }

        public async Task<int> PurgeOlderThanAsync(int days)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.ExecuteAsync(
                "DELETE FROM sync_log WHERE created_at < DATE_SUB(NOW(), INTERVAL @Days DAY)",
                new { Days = days });
        }
    }
}
