using Supabase;

namespace o_bergen.LiveResultManager.Infrastructure.Destinations;

/// <summary>
/// Lightweight Supabase query service used for cross-race lookups,
/// e.g. finding all competition dates a participant ID appears in.
/// </summary>
public class SupabaseLookupService
{
    private readonly string _url;
    private readonly string _apiKey;
    private Client? _client;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_url) && !string.IsNullOrWhiteSpace(_apiKey);

    public SupabaseLookupService(string url, string apiKey)
    {
        _url = url ?? string.Empty;
        _apiKey = apiKey ?? string.Empty;
    }

    /// <summary>
    /// Returns a mapping of id → sorted list of competition_dates found in live_results.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> FetchCompetitionsByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var id in ids.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await _client!
                    .From<LiveResult>()
                    .Select("id,competition_date")
                    .Where(x => x.Id == id)
                    .Get();

                var dates = response.Models
                    .Select(r => r.CompetitionDate)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                result[id] = dates;
            }
            catch
            {
                result[id] = new List<string>();
            }
        }

        return result;
    }

    /// <summary>
    /// Updates all live_results rows for oldId to use newId instead.
    /// Since (id, competition_date) is the composite PK we must delete old + insert new per row.
    /// </summary>
    public async Task<int> ReassignIdAsync(string oldId, string newId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        // Fetch all rows for the old ID
        var response = await _client!
            .From<LiveResult>()
            .Where(x => x.Id == oldId)
            .Get();

        if (response.Models.Count == 0)
            return 0;

        int count = 0;
        foreach (var row in response.Models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Delete old row
            await _client!
                .From<LiveResult>()
                .Where(x => x.Id == oldId && x.CompetitionDate == row.CompetitionDate)
                .Delete();

            // Insert with new ID (upsert in case newId already has that date)
            row.Id = newId;
            await _client!
                .From<LiveResult>()
                .OnConflict("id,competition_date")
                .Upsert(row);

            count++;
        }

        return count;
    }

    /// <summary>
    /// Swaps IDs in Supabase so that keepId retains its number but gets removeId's race entries.
    /// Uses tempId as a safe intermediate. Old keepId entries (now under tempId) are merged in
    /// only where keepId does not already have that competition_date.
    /// </summary>
    public async Task SwapReassignIdsAsync(string keepId, string removeId, string tempId, CancellationToken cancellationToken = default)
    {
        // Phase 1: keepId → tempId
        await ReassignIdAsync(keepId, tempId, cancellationToken);

        // Phase 2: removeId → keepId (slot is now free)
        await ReassignIdAsync(removeId, keepId, cancellationToken);

        // Phase 3: Merge tempId entries into keepId where date not already taken
        await EnsureInitializedAsync();

        var tempResponse = await _client!
            .From<LiveResult>()
            .Where(x => x.Id == tempId)
            .Get();

        var keepResponse = await _client!
            .From<LiveResult>()
            .Where(x => x.Id == keepId)
            .Get();

        var keepDates = keepResponse.Models
            .Select(r => r.CompetitionDate)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var row in tempResponse.Models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _client!
                .From<LiveResult>()
                .Where(x => x.Id == tempId && x.CompetitionDate == row.CompetitionDate)
                .Delete();

            if (!keepDates.Contains(row.CompetitionDate))
            {
                row.Id = keepId;
                await _client!
                    .From<LiveResult>()
                    .OnConflict("id,competition_date")
                    .Upsert(row);
            }
        }
    }

    /// <summary>
    /// Returns the subset of the given IDs that have an entry in the runners table
    /// (i.e. the participant has created an account on the web portal).
    /// </summary>
    public async Task<HashSet<string>> FetchRunnerIdStatusAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in ids.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await _client!
                    .From<Runner>()
                    .Select("runner_id")
                    .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Get();

                if (response.Models.Count > 0)
                    found.Add(id);
            }
            catch
            {
                // Ignore per-id failures; just treat as no account
            }
        }

        return found;
    }

    /// <summary>
    /// Returns the subset of the given IDs that appear as runner_id in the profiles table.
    /// A profile runner_id means the person has explicitly chosen this ID as their canonical
    /// identity on the web portal — the strongest signal for which ID to consolidate around.
    /// </summary>
    public async Task<HashSet<string>> FetchProfileRunnerIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in ids.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await _client!
                    .From<Profile>()
                    .Select("runner_id")
                    .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Get();

                if (response.Models.Count > 0)
                    found.Add(id);
            }
            catch
            {
                // Ignore per-id failures
            }
        }

        return found;
    }

    /// <summary>
    /// Moves all references to oldRunnerId → newRunnerId in both runners and profiles tables.
    /// runners: if newRunnerId already has a row, delete the oldRunnerId row; otherwise update it.
    /// profiles: always update runner_id from oldRunnerId to newRunnerId so web accounts follow the kept ID.
    /// </summary>
    public async Task UpdateRunnerIdAsync(
        string oldRunnerId,
        string newRunnerId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        cancellationToken.ThrowIfCancellationRequested();

        // runners table: newRunnerId may already exist (both duplicates were in the registry)
        var existing = await _client!
            .From<Runner>()
            .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, newRunnerId)
            .Get();

        if (existing.Models.Count > 0)
        {
            // newRunnerId already registered — delete the redundant oldRunnerId row
            await _client!
                .From<Runner>()
                .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, oldRunnerId)
                .Delete();
        }
        else
        {
            // newRunnerId not yet in registry — move oldRunnerId row to newRunnerId
            await _client!
                .From<Runner>()
                .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, oldRunnerId)
                .Set(r => r.RunnerId!, newRunnerId)
                .Update();
        }

        // profiles table: update any web account that pointed to oldRunnerId
        await _client!
            .From<Profile>()
            .Filter("runner_id", Supabase.Postgrest.Constants.Operator.Equals, oldRunnerId)
            .Set(p => p.RunnerId!, newRunnerId)
            .Update();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_client != null)
            return;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _client = new Client(_url, _apiKey, options);
        await _client.InitializeAsync();
    }
}
