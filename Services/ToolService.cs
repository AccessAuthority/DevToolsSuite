using DevToolsSuite.Data;
using DevToolsSuite.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Ganss.Xss;

namespace DevToolsSuite.Services
{
    public class ToolService : IToolService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToolService> _logger;
        private readonly HtmlSanitizer _htmlSanitizer;

        public ToolService(AppDbContext context, ILogger<ToolService> logger)
        {
            _context = context;
            _logger = logger;
            _htmlSanitizer = new HtmlSanitizer();
            _htmlSanitizer.AllowedTags.Clear();
            _htmlSanitizer.AllowedTags.Add("strong");
            _htmlSanitizer.AllowedTags.Add("em");
            _htmlSanitizer.AllowedTags.Add("code");
            _htmlSanitizer.AllowedTags.Add("pre");
            _htmlSanitizer.AllowedTags.Add("h1");
            _htmlSanitizer.AllowedTags.Add("h2");
            _htmlSanitizer.AllowedTags.Add("h3");
            _htmlSanitizer.AllowedTags.Add("br");
            _htmlSanitizer.AllowedTags.Add("p");
        }

        public async Task<ToolResult> ProcessJsonFormatterAsync(string input, bool format, bool validate)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ToolResult.ErrorResult("Input cannot be empty");

                if (input.Length > ToolConstants.MaxInputLength)
                    return ToolResult.ErrorResult($"Input too long. Maximum {ToolConstants.MaxInputLength} characters allowed.");

                // Always parse to validate
                var parsed = JsonConvert.DeserializeObject(input);
                if (parsed == null)
                    return ToolResult.ErrorResult("Invalid JSON: Could not parse input");

                string result;
                if (format)
                {
                    result = JsonConvert.SerializeObject(parsed, Formatting.Indented);
                    var validationStatus = validate ? "Valid and formatted JSON" : "Formatted JSON";
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, $"{validationStatus} - {result.Length} characters", processingTime);
                }
                else
                {
                    result = JsonConvert.SerializeObject(parsed);
                    var validationStatus = validate ? "Valid JSON" : "JSON parsed successfully";
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, validationStatus, processingTime);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON formatting failed for input: {Input}", input[..Math.Min(100, input.Length)]);
                return ToolResult.ErrorResult($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in JSON formatter");
                return ToolResult.ErrorResult("JSON processing error");
            }
        }

        public async Task<ToolResult> ProcessJwtDecoderAsync(string token)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return ToolResult.ErrorResult("JWT token cannot be empty");

                token = token.Trim();
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return ToolResult.ErrorResult("Invalid JWT format: expected 3 parts separated by dots");

                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                    return ToolResult.ErrorResult("Invalid JWT token format");

                var jwtToken = handler.ReadJwtToken(token);

                var header = JsonConvert.SerializeObject(jwtToken.Header, Formatting.Indented);
                var payload = JsonConvert.SerializeObject(jwtToken.Payload, Formatting.Indented);

                var output = $"HEADER:\n{header}\n\nPAYLOAD:\n{payload}";

                // Check expiration
                var expClaim = jwtToken.Payload.Claims.FirstOrDefault(c => c.Type == "exp");
                var isExpired = false;
                DateTimeOffset? expDate = null;

                if (expClaim != null && long.TryParse(expClaim.Value, out var expTimestamp))
                {
                    expDate = DateTimeOffset.FromUnixTimeSeconds(expTimestamp);
                    isExpired = expDate < DateTimeOffset.UtcNow;
                }

                var additionalData = $"Algorithm: {jwtToken.Header["alg"]}, " +
                                   $"Type: {jwtToken.Header["typ"]}, " +
                                   $"Expires: {(expDate?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never")}, " +
                                   $"Valid: {(isExpired ? "No (Expired)" : "Yes")}";

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return ToolResult.SuccessResult(output, additionalData, processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT decoding failed for token: {Token}", token?[..Math.Min(50, token?.Length ?? 0)]);
                return ToolResult.ErrorResult($"JWT decoding error: {ex.Message}");
            }
        }

        public async Task<ToolResult> ProcessBase64Async(string input, bool encode)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ToolResult.ErrorResult("Input cannot be empty");

                if (input.Length > ToolConstants.MaxInputLength)
                    return ToolResult.ErrorResult($"Input too long. Maximum {ToolConstants.MaxInputLength} characters allowed.");

                if (encode)
                {
                    var bytes = Encoding.UTF8.GetBytes(input);
                    var result = Convert.ToBase64String(bytes);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, $"Encoded {input.Length} characters to {result.Length} base64 characters", processingTime);
                }
                else
                {
                    var base64 = input.Trim();
                    if (base64.Length % 4 != 0)
                        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4), '=');

                    base64 = base64.Replace('_', '/').Replace('-', '+');

                    var bytes = Convert.FromBase64String(base64);
                    var result = Encoding.UTF8.GetString(bytes);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, $"Decoded {input.Length} base64 characters to {result.Length} characters", processingTime);
                }
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Base64 conversion failed for input: {Input}", input[..Math.Min(100, input.Length)]);
                return ToolResult.ErrorResult("Invalid Base64 format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Base64 converter");
                return ToolResult.ErrorResult("Base64 processing error");
            }
        }

        public async Task<ToolResult> ProcessRegexTesterAsync(string input, string pattern, string flags)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    return ToolResult.ErrorResult("Regex pattern cannot be empty");

                if (pattern.Length > 1000)
                    return ToolResult.ErrorResult("Regex pattern too long. Maximum 1000 characters allowed.");

                var regexOptions = RegexOptions.None;
                if (flags.Contains("i", StringComparison.OrdinalIgnoreCase)) regexOptions |= RegexOptions.IgnoreCase;
                if (flags.Contains("m", StringComparison.OrdinalIgnoreCase)) regexOptions |= RegexOptions.Multiline;
                if (flags.Contains("s", StringComparison.OrdinalIgnoreCase)) regexOptions |= RegexOptions.Singleline;

                var regex = new Regex(pattern, regexOptions, TimeSpan.FromMilliseconds(ToolConstants.RegexTimeoutMs));
                var matches = regex.Matches(input);

                var output = new StringBuilder();
                output.AppendLine($"Pattern: /{pattern}/");
                output.AppendLine($"Flags: {flags}");
                output.AppendLine($"Input length: {input.Length} characters");
                output.AppendLine($"Found {matches.Count} matches:");
                output.AppendLine();

                foreach (Match match in matches.Cast<Match>())
                {
                    output.AppendLine($"Match at position {match.Index}: '{WebUtility.HtmlEncode(match.Value)}'");
                    if (match.Groups.Count > 1)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            output.AppendLine($"  Group {i}: '{WebUtility.HtmlEncode(match.Groups[i].Value)}'");
                        }
                    }
                    output.AppendLine();
                }

                if (matches.Count == 0)
                {
                    output.AppendLine("No matches found.");
                }

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return ToolResult.SuccessResult(output.ToString(), additionalData: null, processingTime);
            }
            catch (RegexMatchTimeoutException)
            {
                return ToolResult.ErrorResult("Regex evaluation timed out. Pattern may be too complex.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
                return ToolResult.ErrorResult($"Invalid regex pattern: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in regex tester");
                return ToolResult.ErrorResult("Regex processing error");
            }
        }

        public async Task<ToolResult> ProcessYamlJsonConverterAsync(string input, bool toJson)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ToolResult.ErrorResult("Input cannot be empty");

                if (input.Length > ToolConstants.MaxInputLength)
                    return ToolResult.ErrorResult($"Input too long. Maximum {ToolConstants.MaxInputLength} characters allowed.");

                if (toJson)
                {
                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize(new StringReader(input));

                    var serializer = new SerializerBuilder().JsonCompatible().Build();
                    var json = serializer.Serialize(yamlObject);

                    // Pretty print the JSON
                    var formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(formattedJson, "Converted YAML to JSON", processingTime);
                }
                else
                {
                    var jsonObject = JsonConvert.DeserializeObject(input);
                    if (jsonObject == null)
                        return ToolResult.ErrorResult("Invalid JSON input");

                    var serializer = new SerializerBuilder()
                        .WithIndentedSequences()
                        .Build();
                    var yaml = serializer.Serialize(jsonObject);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(yaml, "Converted JSON to YAML", processingTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YAML/JSON conversion failed for input: {Input}", input[..Math.Min(100, input.Length)]);
                return ToolResult.ErrorResult($"Conversion error: {ex.Message}");
            }
        }

        public async Task<ToolResult> ProcessUrlEncoderAsync(string input, bool encode)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ToolResult.ErrorResult("Input cannot be empty");

                if (input.Length > ToolConstants.MaxInputLength)
                    return ToolResult.ErrorResult($"Input too long. Maximum {ToolConstants.MaxInputLength} characters allowed.");

                if (encode)
                {
                    var result = Uri.EscapeDataString(input);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, $"Encoded {input.Length} characters to {result.Length} URL-safe characters", processingTime);
                }
                else
                {
                    var result = Uri.UnescapeDataString(input);
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(result, $"Decoded {input.Length} URL-encoded characters to {result.Length} characters", processingTime);
                }
            }
            catch (UriFormatException ex)
            {
                _logger.LogWarning(ex, "URL encoding/decoding failed for input: {Input}", input[..Math.Min(100, input.Length)]);
                return ToolResult.ErrorResult("Invalid URL-encoded string");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in URL encoder");
                return ToolResult.ErrorResult("URL processing error");
            }
        }

        public async Task<ToolResult> ProcessTimestampConverterAsync(string input)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    var now = DateTimeOffset.UtcNow;
                    var output = $"Current timestamp:\n" +
                               $"Seconds: {now.ToUnixTimeSeconds()}\n" +
                               $"Milliseconds: {now.ToUnixTimeMilliseconds()}\n\n" +
                               $"UTC: {now:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Local: {now.LocalDateTime:yyyy-MM-dd HH:mm:ss}";
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(output, additionalData: null, processingTime);
                }

                if (long.TryParse(input, out var timestamp))
                {
                    DateTimeOffset date;
                    if (input.Length <= 10) // seconds
                        date = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                    else // milliseconds
                        date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);

                    var output = $"UTC: {date:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Local: {date.LocalDateTime:yyyy-MM-dd HH:mm:ss}\n\n" +
                               $"Timestamp: {timestamp}";
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(output, additionalData: null, processingTime);
                }
                else if (DateTime.TryParse(input, out var parsedDate))
                {
                    var date = new DateTimeOffset(parsedDate);
                    var output = $"Seconds: {date.ToUnixTimeSeconds()}\n" +
                               $"Milliseconds: {date.ToUnixTimeMilliseconds()}\n\n" +
                               $"Input: {input}";
                    var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return ToolResult.SuccessResult(output, additionalData: null, processingTime);
                }

                return ToolResult.ErrorResult("Invalid timestamp or date format. Use Unix timestamp (seconds/milliseconds) or ISO date string.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Timestamp conversion failed for input: {Input}", input);
                return ToolResult.ErrorResult("Timestamp out of valid range");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in timestamp converter");
                return ToolResult.ErrorResult("Timestamp processing error");
            }
        }

        public async Task<ToolResult> ProcessTextDiffAsync(string left, string right)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
                    return ToolResult.ErrorResult("Please enter text in at least one side");

                // Use line-by-line comparison to handle large texts efficiently
                using var leftReader = new StringReader(left);
                using var rightReader = new StringReader(right);

                var output = new StringBuilder();
                int lineNum = 0, differences = 0;
                string? leftLine, rightLine;

                while (true)
                {
                    leftLine = leftReader.ReadLine();
                    rightLine = rightReader.ReadLine();

                    if (leftLine == null && rightLine == null) break;

                    lineNum++;
                    if (leftLine != rightLine)
                    {
                        differences++;
                        output.AppendLine($"Line {lineNum}:");
                        if (leftLine != null)
                            output.AppendLine($"- {WebUtility.HtmlEncode(leftLine)}");
                        if (rightLine != null)
                            output.AppendLine($"+ {WebUtility.HtmlEncode(rightLine)}");
                        output.AppendLine();
                    }
                }

                if (differences == 0)
                    output.AppendLine("No differences found.");

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return ToolResult.SuccessResult(output.ToString(), $"Found {differences} differences across {lineNum} lines", processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Text diff failed");
                return ToolResult.ErrorResult("Text comparison error");
            }
        }

        public async Task<ToolResult> ProcessMarkdownPreviewAsync(string markdown)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(markdown))
                    return ToolResult.ErrorResult("Markdown input cannot be empty");

                if (markdown.Length > ToolConstants.MaxInputLength)
                    return ToolResult.ErrorResult($"Input too long. Maximum {ToolConstants.MaxInputLength} characters allowed.");

                // Enhanced markdown to HTML conversion
                var html = new StringBuilder();
                var lines = markdown.Replace("\r\n", "\n").Split('\n');
                var inCodeBlock = false;
                var inList = false;
                var listType = ""; // "ul" or "ol"

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Code blocks
                    if (trimmedLine.StartsWith("```"))
                    {
                        if (inCodeBlock)
                        {
                            html.AppendLine("</code></pre>");
                        }
                        else
                        {
                            // Get language if specified
                            var language = trimmedLine.Length > 3 ? trimmedLine[3..].Trim() : "";
                            html.AppendLine(string.IsNullOrEmpty(language)
                                ? "<pre><code>"
                                : $"<pre><code class=\"language-{WebUtility.HtmlEncode(language)}\">");
                        }
                        inCodeBlock = !inCodeBlock;
                        continue;
                    }

                    if (inCodeBlock)
                    {
                        html.AppendLine(WebUtility.HtmlEncode(line));
                        continue;
                    }

                    // Headers
                    if (trimmedLine.StartsWith("###### "))
                    {
                        html.AppendLine($"<h6>{WebUtility.HtmlEncode(trimmedLine[7..])}</h6>");
                    }
                    else if (trimmedLine.StartsWith("##### "))
                    {
                        html.AppendLine($"<h5>{WebUtility.HtmlEncode(trimmedLine[6..])}</h5>");
                    }
                    else if (trimmedLine.StartsWith("#### "))
                    {
                        html.AppendLine($"<h4>{WebUtility.HtmlEncode(trimmedLine[5..])}</h4>");
                    }
                    else if (trimmedLine.StartsWith("### "))
                    {
                        html.AppendLine($"<h3>{WebUtility.HtmlEncode(trimmedLine[4..])}</h3>");
                    }
                    else if (trimmedLine.StartsWith("## "))
                    {
                        html.AppendLine($"<h2>{WebUtility.HtmlEncode(trimmedLine[3..])}</h2>");
                    }
                    else if (trimmedLine.StartsWith("# "))
                    {
                        html.AppendLine($"<h1>{WebUtility.HtmlEncode(trimmedLine[2..])}</h1>");
                    }
                    // Lists
                    else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                    {
                        if (!inList)
                        {
                            html.AppendLine("<ul>");
                            inList = true;
                        }
                        html.AppendLine($"<li>{ProcessMarkdownSpans(WebUtility.HtmlEncode(trimmedLine[2..]))}</li>");
                    }
                    else if (trimmedLine.Length > 2 && char.IsDigit(trimmedLine[0]) && trimmedLine[1] == '.')
                    {
                        if (!inList || listType != "ol")
                        {
                            if (inList) html.AppendLine("</ul>");
                            html.AppendLine("<ol>");
                            inList = true;
                            listType = "ol";
                        }
                        html.AppendLine($"<li>{ProcessMarkdownSpans(WebUtility.HtmlEncode(trimmedLine[2..]))}</li>");
                    }
                    else
                    {
                        if (inList)
                        {
                            html.AppendLine(listType == "ol" ? "</ol>" : "</ul>");
                            inList = false;
                            listType = "";
                        }

                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            // Blockquotes
                            if (trimmedLine.StartsWith("> "))
                            {
                                html.AppendLine($"<blockquote>{ProcessMarkdownSpans(WebUtility.HtmlEncode(trimmedLine[2..]))}</blockquote>");
                            }
                            else
                            {
                                var processedLine = ProcessMarkdownSpans(WebUtility.HtmlEncode(line));
                                html.AppendLine($"<p>{processedLine}</p>");
                            }
                        }
                        else
                        {
                            html.AppendLine("<br>");
                        }
                    }
                }

                // Close any open blocks
                if (inList)
                {
                    html.AppendLine(listType == "ol" ? "</ol>" : "</ul>");
                }
                if (inCodeBlock)
                {
                    html.AppendLine("</code></pre>");
                }

                // Sanitize the HTML to prevent XSS
                var safeHtml = _htmlSanitizer.Sanitize(html.ToString());

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return ToolResult.SuccessResult(safeHtml, $"Converted {markdown.Length} characters of markdown", processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Markdown processing failed");
                return ToolResult.ErrorResult("Markdown processing error");
            }
        }

        private string ProcessMarkdownSpans(string line)
        {
            // Fixed regex patterns for markdown spans
            if (string.IsNullOrEmpty(line)) return line;

            // Process bold: **text**
            line = Regex.Replace(line, @"\*\*([^\*]+?)\*\*", "<strong>$1</strong>");
            // Process italic: *text* or _text_
            line = Regex.Replace(line, @"\*([^\*]+?)\*", "<em>$1</em>");
            line = Regex.Replace(line, @"_([^_]+?)_", "<em>$1</em>");
            // Process inline code: `code`
            line = Regex.Replace(line, @"`([^`]+?)`", "<code>$1</code>");

            return line;
        }

        public async Task<ToolResult> ProcessUuidGeneratorAsync(int count)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // FIXED: Changed from 50 to 100 to match frontend
                if (count < 1 || count > 100)
                    return ToolResult.ErrorResult($"Count must be between 1 and 100");

                var uuids = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    uuids.Add(Guid.NewGuid().ToString().ToUpperInvariant());
                }

                var output = string.Join("\n", uuids);
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return ToolResult.SuccessResult(output, $"Generated {count} UUIDs", processingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UUID generation failed for count: {Count}", count);
                return ToolResult.ErrorResult("UUID generation error");
            }
        }

        public async Task LogToolUsageAsync(string toolName, string? userId, string sessionId, int processingTime, string? userAgent)
        {
            try
            {
                var usage = new ToolUsage
                {
                    ToolName = toolName,
                    UserId = userId,
                    SessionId = sessionId,
                    ProcessingTimeMs = processingTime,
                    UserAgent = userAgent,
                    UsedAt = DateTime.UtcNow
                };

                _context.ToolUsages.Add(usage);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging tool usage for {ToolName}", toolName);
            }
        }

        public async Task<IEnumerable<SavedTool>> GetUserSavedToolsAsync(string userId)
        {
            try
            {
                return await _context.SavedTools
                    .Where(st => st.UserId == userId)
                    .OrderByDescending(st => st.LastAccessed)
                    .Take(50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching saved tools for user {UserId}", userId);
                return Enumerable.Empty<SavedTool>();
            }
        }

        public async Task<SavedTool> SaveToolResultAsync(string userId, string toolName, string input, string output, string? additionalData = null)
        {
            try
            {
                var savedTool = new SavedTool
                {
                    UserId = userId,
                    ToolName = toolName,
                    InputData = input.Length > 1000 ? input[..1000] + "..." : input, // Truncate long inputs
                    OutputData = output.Length > 5000 ? output[..5000] + "..." : output, // Truncate long outputs
                    AdditionalData = additionalData,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow
                };

                _context.SavedTools.Add(savedTool);
                await _context.SaveChangesAsync();

                return savedTool;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tool result for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteSavedToolAsync(int id, string userId)
        {
            try
            {
                var savedTool = await _context.SavedTools
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                if (savedTool != null)
                {
                    _context.SavedTools.Remove(savedTool);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting saved tool {ToolId} for user {UserId}", id, userId);
                return false;
            }
        }

        public async Task<IEnumerable<ToolUsageStats>> GetToolUsageStatsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var stats = await _context.ToolUsages
                    .Where(u => u.UsedAt >= startDate && u.UsedAt <= endDate)
                    .GroupBy(u => u.ToolName)
                    .Select(g => new ToolUsageStats
                    {
                        ToolName = g.Key,
                        UsageCount = g.Count(),
                        UniqueUsers = g.Select(u => u.UserId).Distinct().Count(u => u != null),
                        LastUsed = g.Max(u => u.UsedAt),
                        AvgProcessingTime = g.Average(u => u.ProcessingTimeMs),
                        ErrorCount = 0 // This would need error tracking in the ToolUsage entity
                    })
                    .ToListAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tool usage stats from {StartDate} to {EndDate}", startDate, endDate);
                return Enumerable.Empty<ToolUsageStats>();
            }
        }
    }
}