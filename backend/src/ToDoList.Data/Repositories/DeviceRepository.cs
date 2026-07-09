using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ToDoList.Core.Interfaces;
using ToDoList.Core.Models;

namespace ToDoList.Data.Repositories
{
    public class DeviceRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public DeviceRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        // Records first contact and refreshes last_seen / friendly name on
        // every request that carries a device id.
        public async Task UpsertAsync(string deviceId, string name, string? platform)
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            await conn.ExecuteAsync(
                "INSERT INTO device (device_id, name, platform) VALUES (@DeviceId, @Name, @Platform) " +
                "ON DUPLICATE KEY UPDATE name = @Name, platform = @Platform, last_seen = NOW()",
                new { DeviceId = deviceId, Name = name, Platform = platform });
        }

        public async Task<IEnumerable<Device>> GetAllAsync()
        {
            using IDbConnection conn = connectionFactory.CreateConnection();
            conn.Open();
            return await conn.QueryAsync<Device>(
                "SELECT device_id AS DeviceId, name AS Name, platform AS Platform, " +
                "first_seen AS FirstSeen, last_seen AS LastSeen FROM device ORDER BY last_seen DESC");
        }
    }
}
