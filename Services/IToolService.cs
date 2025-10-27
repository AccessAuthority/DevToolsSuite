using DevToolsSuite.Models;
using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Services
{
    public interface IToolService
    {
        // Tool processing methods
        Task<ToolResult> ProcessJsonFormatterAsync(string input, bool format, bool validate);
        Task<ToolResult> ProcessJwtDecoderAsync(string token);
        Task<ToolResult> ProcessBase64Async(string input, bool encode);
        Task<ToolResult> ProcessRegexTesterAsync(string input, string pattern, string flags);
        Task<ToolResult> ProcessYamlJsonConverterAsync(string input, bool toJson);
        Task<ToolResult> ProcessUrlEncoderAsync(string input, bool encode);
        Task<ToolResult> ProcessTimestampConverterAsync(string input);
        Task<ToolResult> ProcessTextDiffAsync(string left, string right);
        Task<ToolResult> ProcessMarkdownPreviewAsync(string markdown);
        Task<ToolResult> ProcessUuidGeneratorAsync(int count);

        // Utility methods
        Task LogToolUsageAsync(string toolName, string? userId, string sessionId, int processingTime, string? userAgent);
        Task<IEnumerable<SavedTool>> GetUserSavedToolsAsync(string userId);
        Task<SavedTool> SaveToolResultAsync(string userId, string toolName, string input, string output, string? additionalData = null);
        Task<bool> DeleteSavedToolAsync(int id, string userId);
        Task<IEnumerable<ToolUsageStats>> GetToolUsageStatsAsync(DateTime startDate, DateTime endDate);
    }

    public record ToolResult(bool Success, string Output, string? Error = null, string? AdditionalData = null)
    {
        public int ProcessingTimeMs { get; init; }
        public DateTime ProcessedAt { get; } = DateTime.UtcNow;
        
        public static ToolResult SuccessResult(string output, string? additionalData = null, int processingTimeMs = 0) 
            => new(true, output, null, additionalData) { ProcessingTimeMs = processingTimeMs };
        
        public static ToolResult ErrorResult(string error, string? output = null) 
            => new(false, output ?? string.Empty, error);
    }

    public class ToolUsageStats
    {
        public string ToolName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int UniqueUsers { get; set; }
        public DateTime LastUsed { get; set; }
        public double AvgProcessingTime { get; set; }
        public int ErrorCount { get; set; }
        public double SuccessRate => UsageCount > 0 ? (UsageCount - ErrorCount) / (double)UsageCount * 100 : 0;
    }

    // Constants for tool configuration
    public static class ToolConstants
    {
        public const int MaxInputLength = 50000;
        public const int MaxUuidCount = 100;
        public const int RegexTimeoutMs = 5000;
        
        public static class ToolNames
        {
            public const string JsonFormatter = "json-formatter";
            public const string JwtDecoder = "jwt-decoder";
            public const string Base64Converter = "base64-converter";
            public const string RegexTester = "regex-tester";
            public const string YamlJsonConverter = "yaml-json-converter";
            public const string UrlEncoder = "url-encoder";
            public const string TimestampConverter = "timestamp-converter";
            public const string TextDiff = "text-diff";
            public const string MarkdownPreview = "markdown-preview";
            public const string UuidGenerator = "uuid-generator";
        }
    }
}