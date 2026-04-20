using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Supabase;

namespace OBergen.LiveResults
{
    /// <summary>
    /// Service for å laste opp live resultater til Supabase
    /// Overvåker results.json og oppdaterer database automatisk
    /// </summary>
    public class LiveResultsService
    {
        private Client? _supabase;
        private IConfiguration _config;
        private string _competitionDate;
        private string _resultsJsonPath;
        private int _watchInterval;
        private FileSystemWatcher? _fileWatcher;
        private DateTime _lastProcessedTime = DateTime.MinValue;

        public LiveResultsService()
        {
            // Last konfigurasjon fra appsettings.json
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            _competitionDate = _config["Supabase:CompetitionDate"] ?? DateTime.Now.ToString("yyyy-MM-dd");
            _resultsJsonPath = _config["EmitSystem:ResultsJsonPath"] ?? "results.json";
            _watchInterval = int.Parse(_config["EmitSystem:WatchInterval"] ?? "60000");
        }

        /// <summary>
        /// Initialiser tilkobling til Supabase
        /// </summary>
        public async Task InitializeAsync()
        {
            var url = _config["Supabase:Url"];
            var serviceRoleKey = _config["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(serviceRoleKey))
            {
                throw new InvalidOperationException(
                    "Supabase URL og Service Role Key må være satt i appsettings.json"
                );
            }

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false // Vi trenger ikke realtime på server-siden
            };

            _supabase = new Client(url, serviceRoleKey, options);
            await _supabase.InitializeAsync();

            Console.WriteLine($"✅ Tilkoblet Supabase for {_competitionDate}");
        }

        /// <summary>
        /// Last opp et enkelt resultat (INSERT eller UPDATE)
        /// </summary>
        public async Task<LiveResult?> UpsertResultAsync(EmitResult emitResult)
        {
            if (_supabase == null)
            {
                throw new InvalidOperationException("Supabase ikke initialisert. Kall InitializeAsync() først.");
            }

            var liveResult = MapToLiveResult(emitResult);

            try
            {
                // Upsert: oppdaterer hvis finnes, oppretter hvis ikke
                // Bruker composite key (id, competition_date) for å skille mellom forskjellige løp
                var response = await _supabase
                    .From<LiveResult>()
                    .OnConflict("id,competition_date")
                    .Upsert(liveResult);

                if (response.Models.Count > 0)
                {
                    var uploaded = response.Models[0];
                    Console.WriteLine(
                        $"✅ {uploaded.FirstName} {uploaded.LastName} ({uploaded.Class}) - " +
                        $"{(uploaded.TimeSeconds.HasValue ? FormatTime(uploaded.TimeSeconds.Value) : uploaded.Status)}"
                    );
                    return uploaded;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Feil ved upload: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Last opp flere resultater i batch (effektivt)
        /// </summary>
        public async Task<int> BatchUpsertResultsAsync(List<EmitResult> emitResults)
        {
            if (_supabase == null)
            {
                throw new InvalidOperationException("Supabase ikke initialisert. Kall InitializeAsync() først.");
            }

            var liveResults = emitResults.Select(MapToLiveResult).ToList();
            int successCount = 0;

            try
            {
                var response = await _supabase
                    .From<LiveResult>()
                    .OnConflict("id,competition_date")
                    .Upsert(liveResults);

                successCount = response.Models.Count;
                Console.WriteLine($"✅ Lastet opp {successCount} av {emitResults.Count} resultater");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Feil ved batch upload: {ex.Message}");
            }

            return successCount;
        }

        /// <summary>
        /// Last opp alle resultater fra results.json
        /// </summary>
        public async Task UploadAllResultsAsync()
        {
            if (!File.Exists(_resultsJsonPath))
            {
                Console.WriteLine($"⚠️ Finner ikke {_resultsJsonPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_resultsJsonPath);
                var emitResults = JsonConvert.DeserializeObject<List<EmitResult>>(json);

                if (emitResults == null || emitResults.Count == 0)
                {
                    Console.WriteLine("⚠️ Ingen resultater funnet i JSON-filen");
                    return;
                }

                Console.WriteLine($"📁 Leste {emitResults.Count} resultater fra {_resultsJsonPath}");
                await BatchUpsertResultsAsync(emitResults);
                _lastProcessedTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Feil ved lesing av results.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Start kontinuerlig overvåking av results.json
        /// </summary>
        public async Task StartWatchingResultsFileAsync()
        {
            Console.WriteLine($"👀 Overvåker {_resultsJsonPath}...");
            Console.WriteLine($"🔄 Sjekker hver {_watchInterval / 1000} sekund");

            // Initial upload
            await UploadAllResultsAsync();

            // Timer-basert polling (mer robust enn FileSystemWatcher)
            var timer = new System.Timers.Timer(_watchInterval);
            timer.Elapsed += async (sender, e) =>
            {
                if (File.Exists(_resultsJsonPath))
                {
                    var lastModified = File.GetLastWriteTime(_resultsJsonPath);
                    if (lastModified > _lastProcessedTime)
                    {
                        Console.WriteLine($"🔔 Fil oppdatert: {lastModified:HH:mm:ss}");
                        await UploadAllResultsAsync();
                    }
                }
            };
            timer.Start();

            // Kjør kontinuerlig
            await Task.Delay(Timeout.Infinite);
        }

        /// <summary>
        /// Konverter fra Emit-format til Supabase LiveResult-format
        /// </summary>
        private LiveResult MapToLiveResult(EmitResult emit)
        {
            return new LiveResult
            {
                CompetitionDate = _competitionDate,
                Ecard = emit.Ecard,
                Ecard2 = emit.Ecard2,
                FirstName = emit.FirstName,
                LastName = emit.LastName,
                StartTime = emit.StartTime,
                Class = emit.Class,
                Course = emit.Course,
                TimeSeconds = emit.TimeSeconds,
                Status = emit.Status,
                StatusMessage = emit.StatusMessage,
                Points = emit.Points,
                TeamId = emit.TeamId,
                TeamName = emit.TeamName,
                SplitTimes = emit.SplitTimes?.Select(st => new SplitTime
                {
                    ControlCode = st.ControlCode,
                    PunchTime = st.PunchTime,
                    SplitSeconds = st.SplitSeconds,
                    CumulativeSeconds = st.CumulativeSeconds,
                    Position = st.Position,
                    Behind = st.Behind
                }).ToList(),
                FinishedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Formater tid som MM:SS
        /// </summary>
        private string FormatTime(int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            return $"{minutes}:{secs:D2}";
        }
    }
}
