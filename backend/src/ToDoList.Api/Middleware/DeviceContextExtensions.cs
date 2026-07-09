using Microsoft.AspNetCore.Http;

namespace ToDoList.Api.Middleware
{
    /// <summary>
    /// Convenience accessors for the device identity stashed on HttpContext by
    /// <see cref="DeviceTrackingMiddleware"/>.
    /// </summary>
    public static class DeviceContextExtensions
    {
        public static string? GetDeviceId(this HttpContext context)
        {
            return context.Items.TryGetValue(DeviceTrackingMiddleware.DeviceIdItem, out object? value)
                ? value as string
                : null;
        }

        public static string? GetDeviceName(this HttpContext context)
        {
            return context.Items.TryGetValue(DeviceTrackingMiddleware.DeviceNameItem, out object? value)
                ? value as string
                : null;
        }
    }
}
