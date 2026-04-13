using Microsoft.AspNetCore.Mvc;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> logger;
        private static readonly string logFilePath = Path.Combine(AppContext.BaseDirectory, "debug.log");
        private static readonly object fileLock = new();

        public DebugController(ILogger<DebugController> logger)
        {
            this.logger = logger;
        }

        [HttpPost("log")]
        public IActionResult Log([FromBody] DebugLogRequest request)
        {
            WriteLogEntry(request);
            return Ok();
        }

        [HttpPost("log/batch")]
        public IActionResult LogBatch([FromBody] List<DebugLogRequest> requests)
        {
            foreach (DebugLogRequest request in requests)
            {
                WriteLogEntry(request);
            }
            return Ok();
        }

        private void WriteLogEntry(DebugLogRequest request)
        {
            string level = (request.Level ?? "DEBUG").ToUpperInvariant();
            string tag = request.Tag ?? "Remote";
            string message = request.Message ?? "";
            string device = request.Device ?? "unknown";
            string timestamp = request.Timestamp ?? DateTime.UtcNow.ToString("o");

            string line = $"[{level}] [{device}] [{tag}] {timestamp} - {message}";

            logger.LogInformation(line);

            lock (fileLock)
            {
                System.IO.File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
    }

    public class DebugLogRequest
    {
        public string? Level { get; set; }
        public string? Tag { get; set; }
        public string? Message { get; set; }
        public string? Device { get; set; }
        public string? Timestamp { get; set; }
    }
}
