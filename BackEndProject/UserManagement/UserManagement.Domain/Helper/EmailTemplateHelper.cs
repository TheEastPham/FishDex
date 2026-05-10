using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace UserManagement.Domain.Helper;

/// <summary>
/// Helper for loading and managing email templates from file system
/// </summary>
public class EmailTemplateHelper
{
    private readonly ILogger<EmailTemplateHelper> _logger;
    private readonly string _templateBasePath;
    private static readonly Dictionary<string, string> TemplateCache = new();
    private static readonly Dictionary<string, Dictionary<string, string>> SubjectCache = new();
    private static readonly object CacheLock = new();

    public EmailTemplateHelper(ILogger<EmailTemplateHelper> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _templateBasePath = Path.Combine(environment.ContentRootPath, "EmailTemplates");
        
        // Ensure templates directory exists
        EnsureTemplateDirectoryExists();
    }

    /// <summary>
    /// Get email template with token replacements
    /// </summary>
    /// <param name="templateName">Name of template file (without extension)</param>
    /// <param name="language">Language code (en, vi, etc.)</param>
    /// <param name="replacements">Dictionary of tokens to replace in template</param>
    /// <returns>Processed HTML template</returns>
    public async Task<string> GetTemplateAsync(string templateName, string language, Dictionary<string, string> replacements)
    {
        try
        {
            var template = await LoadTemplateAsync(templateName, language);
            return ReplaceTokens(template, replacements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading template {TemplateName} for language {Language}", 
                templateName, language);
            return GetFallbackTemplate(templateName, replacements);
        }
    }

    /// <summary>
    /// Get email subject for template
    /// </summary>
    public async Task<string> GetSubjectAsync(string templateName, string language)
    {
        try
        {
            var subjects = await LoadSubjectsAsync(language);
            
            if (subjects.TryGetValue(templateName, out var subject))
                return subject;

            // Fallback to English if not found
            if (language != "en")
            {
                _logger.LogWarning("Subject for {TemplateName} not found in {Language}, using English", 
                    templateName, language);
                return await GetSubjectAsync(templateName, "en");
            }

            return "Notification from FishLover";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subject for {TemplateName} in {Language}", 
                templateName, language);
            return "Notification from FishLover";
        }
    }

    /// <summary>
    /// Load template from file system with caching
    /// </summary>
    private async Task<string> LoadTemplateAsync(string templateName, string language)
    {
        var cacheKey = $"{language}_{templateName}";
        
        // Check cache first
        lock (CacheLock)
        {
            if (TemplateCache.TryGetValue(cacheKey, out var cachedTemplate))
                return cachedTemplate;
        }

        // Build file path
        var filePath = Path.Combine(_templateBasePath, language, $"{templateName}.html");

        // Check if file exists
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Template file not found: {FilePath}", filePath);
            
            // Fallback to English if language is not English
            if (language != "en")
            {
                _logger.LogInformation("Falling back to English template for {TemplateName}", templateName);
                return await LoadTemplateAsync(templateName, "en");
            }

            throw new FileNotFoundException($"Template {templateName}.html not found for language {language}", filePath);
        }

        // Read template from file
        var template = await File.ReadAllTextAsync(filePath);

        // Cache the template
        lock (CacheLock)
        {
            TemplateCache[cacheKey] = template;
        }

        _logger.LogDebug("Template {TemplateName} loaded successfully for language {Language}", 
            templateName, language);

        return template;
    }

    /// <summary>
    /// Load subjects from JSON file
    /// </summary>
    private async Task<Dictionary<string, string>> LoadSubjectsAsync(string language)
    {
        var cacheKey = $"subjects_{language}";
        
        // Check cache
        lock (CacheLock)
        {
            if (SubjectCache.TryGetValue(cacheKey, out var cachedSubjects))
                return cachedSubjects;
        }

        var filePath = Path.Combine(_templateBasePath, language, "subjects.json");

        if (!File.Exists(filePath))
        {
            if (language != "en")
            {
                return await LoadSubjectsAsync("en");
            }
            
            _logger.LogWarning("Subjects file not found: {FilePath}", filePath);
            return new Dictionary<string, string>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var subjects = JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                      ?? new Dictionary<string, string>();

        // Cache subjects
        lock (CacheLock)
        {
            SubjectCache[cacheKey] = subjects;
        }

        return subjects;
    }

    /// <summary>
    /// Replace {{tokens}} in template with actual values
    /// </summary>
    private string ReplaceTokens(string template, Dictionary<string, string> replacements)
    {
        if (replacements == null || !replacements.Any())
            return template;

        foreach (var (key, value) in replacements)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }

        return template;
    }

    /// <summary>
    /// Get minimal fallback template when file loading fails
    /// </summary>
    private string GetFallbackTemplate(string templateName, Dictionary<string, string> replacements)
    {
        var content = string.Join("\n", replacements.Select(r => 
            $"<p><strong>{r.Key}:</strong> {r.Value}</p>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f9f9f9; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>{templateName}</h2>
        {content}
        <p><small>This is a fallback template. Please contact administrator.</small></p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Ensure template directory structure exists
    /// </summary>
    private void EnsureTemplateDirectoryExists()
    {
        var languages = new[] { "en", "vi" };
        
        foreach (var lang in languages)
        {
            var langPath = Path.Combine(_templateBasePath, lang);
            if (!Directory.Exists(langPath))
            {
                _logger.LogWarning("Template directory not found: {Path}. Creating...", langPath);
                Directory.CreateDirectory(langPath);
            }
        }
    }

    /// <summary>
    /// Clear template cache (useful for development/hot reload)
    /// </summary>
    public void ClearCache()
    {
        lock (CacheLock)
        {
            TemplateCache.Clear();
            SubjectCache.Clear();
        }
        _logger.LogInformation("Email template cache cleared");
    }

    /// <summary>
    /// Create a new template file (for admin/management)
    /// </summary>
    public async Task<bool> CreateTemplateAsync(string templateName, string language, string content)
    {
        try
        {
            var filePath = Path.Combine(_templateBasePath, language, $"{templateName}.html");
            await File.WriteAllTextAsync(filePath, content);
            
            // Clear cache for this template
            var cacheKey = $"{language}_{templateName}";
            lock (CacheLock)
            {
                TemplateCache.Remove(cacheKey);
            }
            
            _logger.LogInformation("Template {TemplateName} created/updated for language {Language}", 
                templateName, language);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName} for language {Language}", 
                templateName, language);
            return false;
        }
    }

    /// <summary>
    /// Update subjects file (for admin/management)
    /// </summary>
    public async Task<bool> UpdateSubjectsAsync(string language, Dictionary<string, string> subjects)
    {
        try
        {
            var filePath = Path.Combine(_templateBasePath, language, "subjects.json");
            var json = JsonSerializer.Serialize(subjects, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            await File.WriteAllTextAsync(filePath, json);
            
            // Clear cache
            lock (CacheLock)
            {
                SubjectCache.Remove($"subjects_{language}");
            }
            
            _logger.LogInformation("Subjects updated for language {Language}", language);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subjects for language {Language}", language);
            return false;
        }
    }

    /// <summary>
    /// Get list of available templates for a language
    /// </summary>
    public async Task<List<string>> GetAvailableTemplatesAsync(string language)
    {
        var langPath = Path.Combine(_templateBasePath, language);
        
        if (!Directory.Exists(langPath))
            return new List<string>();

        var files = Directory.GetFiles(langPath, "*.html");
        return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    }
}
