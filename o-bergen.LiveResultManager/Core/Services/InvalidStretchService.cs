using System.Text.Json;
using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Core.Services;

/// <summary>
/// Service for managing invalid stretches and calculating time adjustments
/// </summary>
public class InvalidStretchService : IInvalidStretchService
{
    private readonly string _configFilePath;
    private InvalidStretchConfiguration _configuration;
    private readonly object _lock = new();

    public InvalidStretchService(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentNullException(nameof(basePath));

        _configFilePath = Path.Combine(basePath, "invalid-stretches.json");
        _configuration = new InvalidStretchConfiguration();
        
        LoadConfiguration();
    }

    public List<InvalidStretch> GetStretchesForEvent(string eventId)
    {
        lock (_lock)
        {
            return _configuration.Stretches
                .Where(s => s.EventId == eventId)
                .ToList();
        }
    }

    public List<InvalidStretch> GetAllStretches()
    {
        lock (_lock)
        {
            return _configuration.Stretches.ToList();
        }
    }

    public void AddStretch(InvalidStretch stretch)
    {
        if (stretch == null)
            throw new ArgumentNullException(nameof(stretch));

        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(stretch.Id))
                stretch.Id = Guid.NewGuid().ToString();

            _configuration.Stretches.Add(stretch);
        }
    }

    public bool RemoveStretch(string stretchId)
    {
        if (string.IsNullOrWhiteSpace(stretchId))
            return false;

        lock (_lock)
        {
            var stretch = _configuration.Stretches.FirstOrDefault(s => s.Id == stretchId);
            if (stretch == null)
                return false;

            _configuration.Stretches.Remove(stretch);
            return true;
        }
    }

    public bool UpdateStretch(InvalidStretch stretch)
    {
        if (stretch == null || string.IsNullOrWhiteSpace(stretch.Id))
            return false;

        lock (_lock)
        {
            var index = _configuration.Stretches.FindIndex(s => s.Id == stretch.Id);
            if (index < 0)
                return false;

            _configuration.Stretches[index] = stretch;
            return true;
        }
    }

    public int CalculateTimeAdjustment(RaceResult result, string eventId)
    {
        if (result?.SplitTimes == null || result.SplitTimes.Count == 0)
            return 0;

        var stretches = GetStretchesForEvent(eventId);
        if (stretches.Count == 0)
            return 0;

        int totalAdjustment = 0;

        foreach (var stretch in stretches)
        {
            var adjustment = FindAndCalculateStretchTime(result.SplitTimes, 
                stretch.FromControlCode, stretch.ToControlCode);
            totalAdjustment += adjustment;
        }

        return totalAdjustment;
    }

    public string GetAdjustmentDescription(RaceResult result, string eventId)
    {
        if (result?.SplitTimes == null || result.SplitTimes.Count == 0)
            return string.Empty;

        var stretches = GetStretchesForEvent(eventId);
        if (stretches.Count == 0)
            return string.Empty;

        var adjustments = new List<string>();

        foreach (var stretch in stretches)
        {
            var adjustment = FindAndCalculateStretchTime(result.SplitTimes, 
                stretch.FromControlCode, stretch.ToControlCode);
            
            if (adjustment > 0)
            {
                adjustments.Add($"{stretch.FromControlCode}→{stretch.ToControlCode}: -{adjustment}s");
            }
        }

        return adjustments.Count > 0 
            ? $"Adjusted: {string.Join(", ", adjustments)}" 
            : string.Empty;
    }

    public async Task SaveAsync()
    {
        try
        {
            InvalidStretchConfiguration configToSave;
            lock (_lock)
            {
                configToSave = _configuration;
            }

            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Don't escape Norwegian characters
            });

            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save invalid stretch configuration: {ex.Message}", ex);
        }
    }

    public async Task ReloadAsync()
    {
        await Task.Run(() => LoadConfiguration());
    }

    private void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _configuration = new InvalidStretchConfiguration();
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var loaded = JsonSerializer.Deserialize<InvalidStretchConfiguration>(json);
            
            lock (_lock)
            {
                _configuration = loaded ?? new InvalidStretchConfiguration();
            }
        }
        catch
        {
            _configuration = new InvalidStretchConfiguration();
        }
    }

    /// <summary>
    /// Find consecutive controls in either direction and calculate the split time between them
    /// </summary>
    private int FindAndCalculateStretchTime(List<SplitTime> splitTimes, string control1, string control2)
    {
        if (splitTimes.Count < 2)
            return 0;

        // Debug: Log split time codes for troubleshooting
        System.Diagnostics.Debug.WriteLine($"Looking for stretch {control1}→{control2} in: {string.Join(", ", splitTimes.Select(st => st.Code))}");

        for (int i = 0; i < splitTimes.Count - 1; i++)
        {
            var current = splitTimes[i];
            var next = splitTimes[i + 1];

            // Check both directions: control1→control2 OR control2→control1
            if ((current.Code == control1 && next.Code == control2) ||
                (current.Code == control2 && next.Code == control1))
            {
                // Calculate the split time between the two controls
                // Splittime in next control is the time from previous control to this control
                System.Diagnostics.Debug.WriteLine($"Found match: {current.Code}→{next.Code}, adjustment: {next.Splittime}s");
                return next.Splittime;
            }
        }

        System.Diagnostics.Debug.WriteLine($"No match found for stretch {control1}→{control2}");
        return 0;
    }
}
