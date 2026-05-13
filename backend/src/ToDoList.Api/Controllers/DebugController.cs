using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ToDoList.Api.Controllers
{
    [ApiController]
    [Route("api/debug")]
    [EnableRateLimiting("debug")]
    public class DebugController : ControllerBase
    {
        // 10 MB cap on the local debug log. When exceeded, the file is
        // rotated to debug.log.1 (single previous copy retained).
        private const long MaxLogSizeBytes = 10 * 1024 * 1024;

        private static readonly string logFilePath =
            Path.Combine(AppContext.BaseDirectory, "debug.log");
        private static readonly string logRotatedPath =
            Path.Combine(AppContext.BaseDirectory, "debug.log.1");
        private static readonly object fileLock = new();

        private readonly ILogger<DebugController> logger;
        private readonly bool enabled;

        public DebugController(ILogger<DebugController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.enabled = configuration.GetValue("DebugLogging:Enabled", true);
        }

        [HttpPost("log")]
        public IActionResult Log([FromBody] DebugLogRequest? request)
        {
            if (!enabled)
            {
                return NotFound();
            }
            if (request is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            WriteLogEntry(request);
            return Ok();
        }

        [HttpPost("log/batch")]
        public IActionResult LogBatch([FromBody] List<DebugLogRequest>? requests)
        {
            if (!enabled)
            {
                return NotFound();
            }
            if (requests is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (requests.Count > 100)
            {
                return BadRequest("Batch size cannot exceed 100 entries.");
            }

            foreach (DebugLogRequest request in requests)
            {
                WriteLogEntry(request);
            }
            return Ok();
        }

        private void WriteLogEntry(DebugLogRequest request)
        {
            string level = Sanitize(request.Level ?? "DEBUG").ToUpperInvariant();
            string tag = Sanitize(request.Tag ?? "Remote");
            string message = Sanitize(request.Message ?? "");
            string device = Sanitize(request.Device ?? "unknown");
            string timestamp = Sanitize(request.Timestamp ?? DateTime.UtcNow.ToString("o"));

            // Structured logging — fields are positional parameters, not
            // part of the template, so attacker-supplied text can't forge
            // log lines in structured sinks.
            logger.LogInformation(
                "Remote client log: level={Level} device={Device} tag={Tag} timestamp={Timestamp} message={Message}",
                level, device, tag, timestamp, message);

            string line = $"[{level}] [{device}] [{tag}] {timestamp} - {message}";
            AppendWithRotation(line);
        }

        // Strip CR, LF, and other control characters so attacker-supplied
        // text can't forge log lines or inject ANSI escape sequences that
        // wreck terminal viewers (tail, less, cat).
        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder sb = new(input.Length);
            foreach (char c in input)
            {
                if (c == '\t' || (c >= 0x20 && c != 0x7F))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static void AppendWithRotation(string line)
        {
            lock (fileLock)
            {
                FileInfo info = new(logFilePath);
                if (info.Exists && info.Length > MaxLogSizeBytes)
                {
                    try
                    {
                        if (System.IO.File.Exists(logRotatedPath))
                        {
                            System.IO.File.Delete(logRotatedPath);
                        }
                        System.IO.File.Move(logFilePath, logRotatedPath);
                    }
                    catch
                    {
                        // If rotation fails (permissions, race, etc.) fall
                        // through and append anyway rather than dropping
                        // the entry.
                    }
                }

                System.IO.File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
    }

    public class DebugLogRequest
    {
        [MaxLength(20)]
        public string? Level { get; set; }

        [MaxLength(100)]
        public string? Tag { get; set; }

        [MaxLength(10000)]
        public string? Message { get; set; }

        [MaxLength(200)]
        public string? Device { get; set; }

        [MaxLength(50)]
        public string? Timestamp { get; set; }
    }
}
