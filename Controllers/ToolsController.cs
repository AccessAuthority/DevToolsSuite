using DevToolsSuite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DevToolsSuite.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Controllers
{
    public class ToolsController : Controller
    {
        private readonly IToolService _toolService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ToolsController> _logger;

        public ToolsController(IToolService toolService, IHttpContextAccessor httpContextAccessor, ILogger<ToolsController> logger)
        {
            _toolService = toolService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // === MVC VIEWS ===

        // Tool list view
        public IActionResult Index()
        {
            var tools = new[]
            {
                new { Name = "JSON Formatter", Slug = "json-formatter", Description = "Format and validate JSON data", Icon = "{}" },
                new { Name = "JWT Decoder", Slug = "jwt-decoder", Description = "Decode JWT tokens", Icon = "🔐" },
                new { Name = "Base64 Converter", Slug = "base64-converter", Description = "Encode/decode Base64", Icon = "64" },
                new { Name = "Regex Tester", Slug = "regex-tester", Description = "Test regular expressions", Icon = ".*" },
                new { Name = "YAML/JSON Converter", Slug = "yaml-json-converter", Description = "Convert between YAML and JSON", Icon = "🔄" },
                new { Name = "URL Encoder", Slug = "url-encoder", Description = "Encode/decode URL components", Icon = "🌐" },
                new { Name = "Timestamp Converter", Slug = "timestamp-converter", Description = "Convert timestamps", Icon = "⏰" },
                new { Name = "Text Diff", Slug = "text-diff", Description = "Compare text differences", Icon = "📊" },
                new { Name = "Markdown Preview", Slug = "markdown-preview", Description = "Preview markdown", Icon = "📝" },
                new { Name = "UUID Generator", Slug = "uuid-generator", Description = "Generate UUIDs", Icon = "#" }
            };

            return View(tools);
        }

        // Individual tool view
        [Route("tools/{toolSlug}")]
        public IActionResult Tool(string toolSlug)
        {
            // Validate tool slug
            var validTools = new[]
            {
                "json-formatter",
                "jwt-decoder",
                "base64-converter",
                "regex-tester",
                "yaml-json-converter",
                "url-encoder",
                "timestamp-converter",
                "text-diff",
                "markdown-preview",
                "uuid-generator"
            };

            if (!validTools.Contains(toolSlug.ToLowerInvariant()))
            {
                return RedirectToAction("Index");
            }

            ViewData["ToolSlug"] = toolSlug.ToLowerInvariant();
            ViewData["ToolName"] = ToTitleCase(toolSlug.Replace("-", " "));
            return View();
        }

        // === API ENDPOINTS ===

        // Process tool API - ENHANCED VERSION
        [HttpPost]
        [Route("api/tools/{toolSlug}")]
        public async Task<IActionResult> ProcessTool(string toolSlug, [FromBody] ToolRequest request)
        {
            ToolResult result = new ToolResult(false, string.Empty, null);
            var startTime = DateTime.UtcNow;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = _httpContextAccessor.HttpContext?.Session.Id ?? "anonymous";

            try
            {
                toolSlug = toolSlug.ToLowerInvariant();

                result = toolSlug switch
                {
                    "json-formatter" => await _toolService.ProcessJsonFormatterAsync(
                        request.Input ?? "",
                        request.Format ?? false,
                        request.Validate ?? false),

                    "jwt-decoder" => await _toolService.ProcessJwtDecoderAsync(request.Input ?? ""),

                    "base64-converter" => await _toolService.ProcessBase64Async(
                        request.Input ?? "",
                        request.Encode ?? true),

                    "regex-tester" => await _toolService.ProcessRegexTesterAsync(
                        request.Input ?? "",
                        request.Pattern ?? "",
                        request.Flags ?? ""),

                    "yaml-json-converter" => await _toolService.ProcessYamlJsonConverterAsync(
                        request.Input ?? "",
                        request.ToJson ?? true),

                    "url-encoder" => await _toolService.ProcessUrlEncoderAsync(
                        request.Input ?? "",
                        request.Encode ?? true),

                    "timestamp-converter" => await _toolService.ProcessTimestampConverterAsync(request.Input ?? ""),

                    "text-diff" => await _toolService.ProcessTextDiffAsync(
                        request.Input ?? "",
                        request.Input2 ?? ""),

                    "markdown-preview" => await _toolService.ProcessMarkdownPreviewAsync(request.Input ?? ""),

                    "uuid-generator" => await _toolService.ProcessUuidGeneratorAsync(request.Count ?? 1),

                    _ => new ToolResult(false, string.Empty, "Tool not found")
                };

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var userAgent = Request.Headers["User-Agent"].ToString();

                await _toolService.LogToolUsageAsync(toolSlug, userId, sessionId, processingTime, userAgent);

                if (result.Success && request.SaveResult == true && !string.IsNullOrWhiteSpace(userId))
                {
                    await _toolService.SaveToolResultAsync(userId, toolSlug, request.Input ?? "",
                        result.Output ?? string.Empty, result.AdditionalData);
                }

                // Enhanced response format (like API controller)
                return Json(new ApiResponse<object>
                {
                    Success = result.Success,
                    Data = new
                    {
                        output = result.Output ?? string.Empty,
                        additionalData = result.AdditionalData,
                        processingTimeMs = result.ProcessingTimeMs
                    },
                    Error = result.Success ? null : result.Error ?? "Unknown error",
                    Message = result.Success ? "Tool processed successfully" : null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tool {ToolSlug}", toolSlug);

                try
                {
                    await _toolService.LogToolUsageAsync(toolSlug, null, "error", 0, null);
                }
                catch { }

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "An error occurred while processing your request",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Get saved tools (paged) - ENHANCED VERSION
        [HttpGet]
        [Route("api/tools/saved")]
        public async Task<IActionResult> GetSavedTools([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Authentication required",
                    Timestamp = DateTime.UtcNow
                }));

            try
            {
                var savedTools = await _toolService.GetUserSavedToolsAsync(userId);
                var totalCount = savedTools.Count();

                var pagedTools = savedTools
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new ApiResponse<PagedResult<SavedTool>>
                {
                    Success = true,
                    Data = new PagedResult<SavedTool>
                    {
                        Items = pagedTools,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        //TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        //HasPrevious = page > 1,
                        //HasNext = page < (int)Math.Ceiling(totalCount / (double)pageSize)
                    },
                    Message = "Saved tools retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching saved tools for user {UserId}", userId);
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Error fetching saved tools",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Delete saved tool - ENHANCED VERSION
        [HttpDelete]
        [Route("api/tools/saved/{id}")]
        public async Task<IActionResult> DeleteSavedTool(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Authentication required",
                    Timestamp = DateTime.UtcNow
                }));

            try
            {
                var deleted = await _toolService.DeleteSavedToolAsync(id, userId);

                return Json(deleted
                    ? new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Tool session deleted successfully",
                        Timestamp = DateTime.UtcNow
                    }
                    : new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Tool session not found",
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting saved tool {ToolId} for user {UserId}", id, userId);
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Error deleting tool session",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Get tool statistics - NEW FROM API CONTROLLER
        [HttpGet]
        [Route("api/tools/stats")]
        public async Task<IActionResult> GetToolStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                // Validate date range
                if (startDate > endDate)
                    return BadRequest(Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Start date cannot be after end date",
                        Timestamp = DateTime.UtcNow
                    }));

                if ((endDate.Value - startDate.Value).TotalDays > 365)
                    return BadRequest(Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Date range cannot exceed 1 year",
                        Timestamp = DateTime.UtcNow
                    }));

                var stats = await _toolService.GetToolUsageStatsAsync(startDate.Value, endDate.Value);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        period = new { start = startDate.Value, end = endDate.Value },
                        stats = stats
                    },
                    Message = "Tool statistics retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tool stats");
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Error fetching tool statistics",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Health check - NEW FROM API CONTROLLER
        [HttpGet]
        [Route("api/tools/health")]
        public IActionResult HealthCheck()
        {
            return Json(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0"
                },
                Message = "Service is running normally",
                Timestamp = DateTime.UtcNow
            });
        }

        // === HELPER METHODS ===

        private string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                    words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
            return string.Join(" ", words);
        }

        private ApiResponse<object> CreateErrorResponse(string error, string? details = null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Error = error,
                Message = details,
                Timestamp = DateTime.UtcNow
            };
        }

        private string GetModelStateErrors()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }
    }

    // === REQUEST/RESPONSE MODELS ===

    // Original ToolRequest model
    public class ToolRequest
    {
        public string? Input { get; set; }
        public string? Input2 { get; set; }
        public string? Pattern { get; set; }
        public string? Flags { get; set; }
        public bool? Encode { get; set; }
        public bool? Format { get; set; }
        public bool? Validate { get; set; }
        public bool? ToJson { get; set; }
        public int? Count { get; set; }
        public bool? SaveResult { get; set; }
    }

    // API Response models from API controller
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        // Remove the setters - these are computed properties
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    // ToolProcessRequest model from API controller (for compatibility)
    public class ToolProcessRequest
    {
        [Required(ErrorMessage = "Input is required")]
        [StringLength(Services.ToolConstants.MaxInputLength, ErrorMessage = "Input cannot exceed {1} characters")]
        public string Input { get; set; } = string.Empty;

        [Required]
        public Dictionary<string, object> Options { get; set; } = new();

        public bool SaveResult { get; set; }

        public T GetOption<T>(string key, T defaultValue = default!)
        {
            if (Options.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)value.ToString()!;

                    if (value is T typedValue)
                        return typedValue;

                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}