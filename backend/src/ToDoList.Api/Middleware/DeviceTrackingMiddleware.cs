using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Middleware
{
    /// <summary>
    /// Reads device identity headers (X-Device-Id / X-Device-Name /
    /// X-Device-Platform) on each request, stashes them on HttpContext.Items
    /// for controllers, and upserts the device registry so the log viewer can
    /// show friendly names.
    /// </summary>
    public class DeviceTrackingMiddleware
    {
        public const string DeviceIdItem = "DeviceId";
        public const string DeviceNameItem = "DeviceName";
        public const string DevicePlatformItem = "DevicePlatform";

        private readonly RequestDelegate next;

        public DeviceTrackingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, DeviceRepository deviceRepository)
        {
            string? deviceId = Clean(context.Request.Headers["X-Device-Id"], 64);
            string? deviceName = Clean(context.Request.Headers["X-Device-Name"], 100);
            string? platform = Clean(context.Request.Headers["X-Device-Platform"], 50);

            if (!string.IsNullOrEmpty(deviceId))
            {
                context.Items[DeviceIdItem] = deviceId;
                context.Items[DeviceNameItem] = deviceName ?? deviceId;
                context.Items[DevicePlatformItem] = platform;

                try
                {
                    await deviceRepository.UpsertAsync(deviceId, deviceName ?? deviceId, platform);
                }
                catch
                {
                    // Device tracking is non-critical — never fail the request.
                }
            }

            await next(context);
        }

        // Trim, strip control characters, and cap length so a header can't
        // forge log lines or overflow a column.
        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            System.Text.StringBuilder sb = new(value.Length);
            foreach (char c in value)
            {
                if (c >= 0x20 && c != 0x7F)
                {
                    sb.Append(c);
                }
            }

            string cleaned = sb.ToString().Trim();
            if (cleaned.Length > maxLength)
            {
                cleaned = cleaned.Substring(0, maxLength);
            }
            return cleaned.Length == 0 ? null : cleaned;
        }
    }
}
