using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WorkerServiceApp.Services
{
    public class PollerService
    {
        private readonly string _connectionString;
        private string outputDirectory;
        private string raceDirectory = "";
        private readonly string todayDirectory = "c:\\Results\\Today";

        private readonly ILogger<PollerService> _logger;
        private int year = -1;
        private int day = -1;
        private string arrDate = "";
        private string arrPlace = "";

        public PollerService(string connectionString, string outputDirectory, ILogger<PollerService> logger)
        {
            this.outputDirectory = "c:\\Diverse\\LiveDeployer\\Data";
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null.");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        }



        public async Task ExecuteQueryAsync()
        {
            var metadata = await FetchMetadataAsync();
            _logger.LogInformation("Fetched metadata: {metadata}", JsonSerializer.Serialize(metadata));
            day = metadata.Day;
            if (day == -1)
            {
                _logger.LogError("Day not found in metadata. Cannot proceed with fetching results.");
                return;
            }
            if (string.IsNullOrEmpty(metadata.Date) || string.IsNullOrEmpty(metadata.Year))
            {
                _logger.LogError("Date or Year not found in metadata. Cannot proceed with fetching results.");
                return;
            }
            var metadataJson = await CreateMetadataJsonAsync(metadata);
            raceDirectory = $"{outputDirectory}\\{metadata.Year}\\{metadata.Date}" ?? throw new ArgumentNullException(nameof(outputDirectory), "Output directory cannot be null.");
            //create directory if it does not already exist
            if (!Directory.Exists(raceDirectory))
            {
                Directory.CreateDirectory(raceDirectory);
            }
            // Hent resultater
            var results = await FetchResultsAsync();
            _logger.LogInformation("Number of results: {count}", results.Count);
            results = await FetchAndAddSplitTimesAsync(results);
            await WriteMetadataAsync(metadataJson, raceDirectory);
            await WriteMetadataAsync(metadataJson, todayDirectory);
            // Skriv resultater til filer
            await WriteResultsAsync(results, raceDirectory);
            await WriteResultsAsync(results, todayDirectory);
            _logger.LogInformation("Results successfully written to files, count {count}.", results.Count);

            _logger.LogInformation("Deploying results to internet...");
            await DeployResultsAsync(metadata);
            _logger.LogInformation("Done deploying.");
        }

        public async Task<string> CreateMetadataJsonAsync(RaceMetadata raceMetadata)
        {
            var metadata = new EventMetadataDto
            {
                Name = "Bedriftsløp",
                Date = raceMetadata.Date,
                Organizer = raceMetadata.Organizer,
                Location = raceMetadata.Place,
                Courses = raceMetadata.Courses.ToList(),
                EventType = "Normal",
                TerrainType = "Skog"
            };


            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(metadata, options);
            return json;
        }
        public async Task<List<ParticipantResult>> FetchResultsAsync()
        {
            var results = new List<ParticipantResult>();

            using (var connection = new OdbcConnection(_connectionString))
            {
                await connection.OpenAsync();
                var dayAsString = day.ToString().Trim();
                _logger.LogInformation("Fetching results for day: {day}", day);
                using (var command = new OdbcCommand($"select n.id, n.ecard, n.ecard2, n.ename, n.name, n.times, n.status, n.statusmsg, n.class, n.cource, n.points, n.team, t.name as teamname from Name n, Team t where n.team=t.code and n.status in ('A','D','B','S')", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var result = new ParticipantResult
                            {
                                Id = reader["id"].ToString().Trim(),
                                ECard = reader["ecard"].ToString().Trim(),
                                ECard2 = reader["ecard2"].ToString().Trim(),
                                FirstName = reader["ename"].ToString().Trim(),
                                LastName = reader["name"].ToString().Trim(),
                                Time = reader["times"].ToString().Trim(),
                                Status = reader["status"].ToString().Trim(),
                                StatusMessage = reader["statusmsg"].ToString().Trim(),
                                Class = reader["class"].ToString().Trim(),
                                Course = reader["cource"].ToString().Trim(),
                                Points = reader["points"].ToString().Trim(),
                                TeamId = reader["team"].ToString().Trim(),
                                TeamName = reader["teamname"].ToString().Trim()
                            };
                            results.Add(result);
                        }
                    }
                }
            }

            return results;
        }

        private async Task<List<ParticipantResult>> FetchAndAddSplitTimesAsync(IEnumerable<ParticipantResult> results)
        {
            var resultsWithSplits = new List<ParticipantResult>();

            using (var connection = new OdbcConnection(_connectionString))
            {
                await connection.OpenAsync();

                var presplits = new Dictionary<int, List<SplitTime>>();
                //reading all splits into memory before adding to results
                using (var command = new OdbcCommand($"SELECT ecardno, nr, control, times FROM ecard", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var split = new SplitTime
                            {
                                Number = Convert.ToInt32(reader["nr"]),
                                Code = reader["control"].ToString().Trim(),
                                Splittime = -1,
                                Totaltime = Convert.ToInt32(reader["times"]),
                            };
                            var ecardno = Convert.ToInt32(reader["ecardno"]);
                            if (!presplits.ContainsKey(ecardno))
                                presplits[ecardno] = new List<SplitTime>();
                            presplits[ecardno].Add(split);
                        }
                    }
                }
                foreach (var result in results)
                {
                    //Hvis leiebrikke er registrert i det egne leiebrikkefeltet (lyseblått) 
                    // uten å slette det vanlige (gult), så plukker vi den brikken for dette løpet
                    var ecardNo = int.TryParse(result.ECard2, out var e2) ? e2 : -1;
                    if (ecardNo == -1) //plukk det vanlige
                    {
                        ecardNo = int.TryParse(result.ECard, out var e) ? e : -1;
                    }
                    if (ecardNo != -1 && presplits.ContainsKey(ecardNo))
                    {
                        // Assign all splits for this participant's ecard
                        var splits = presplits[ecardNo].OrderBy(s => s.Number).ToList();
                        // remove split with code 250:
                        splits.RemoveAll(s => s.Code == "250");
                        //calculate split time as the time from previous code :

                        if (splits.Count > 0)
                        {
                            splits[0].Splittime = splits[0].Totaltime;
                            for (int i = 1; i < splits.Count; i++)
                            {
                                splits[i].Splittime = splits[i].Totaltime - splits[i - 1].Totaltime;
                            }
                        }

                        result.SplitTimes = splits;
                    }
                    resultsWithSplits.Add(result);
                }

            }

            return resultsWithSplits;
        }


        public async Task<RaceMetadata> FetchMetadataAsync()
        {
            var metadata = new RaceMetadata();

            using (var connection = new OdbcConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new OdbcCommand($"SELECT SUB, firststart, Organizator, eventplace FROM arr", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            day = Convert.ToInt32(reader["SUB"].ToString().Trim());
                            arrDate = reader["firststart"].ToString().Trim();
                            arrPlace = reader["eventplace"].ToString().Trim();
                            metadata.Organizer = reader["Organizator"].ToString().Trim();
                        }
                    }

                }
                List<CourseDto> courses = new List<CourseDto>();
                using (var command = new OdbcCommand($"SELECT code, name, length FROM cource", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var course = new CourseDto
                            {
                                Name = reader["name"].ToString().Trim(),
                                Length = double.Parse(reader["length"].ToString().Trim()),
                                Level = reader["code"].ToString().Trim(),
                            };
                            courses.Add(course);
                        }
                    }
                }

                using (var command = new OdbcCommand($"SELECT courceno, controlno, code FROM controls", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            //find the course in the list of courses
                            var courseNo = reader["courceno"].ToString().Trim();
                            var controlCode = reader["controlno"].ToString().Trim();
                            var code = reader["code"].ToString().Trim();

                            var control = new ControlDto
                            {
                                No = int.Parse(controlCode),
                                Code = code
                            };
                            var course = courses.FirstOrDefault(c => c.Level == courseNo);
                            if (course != null)
                            {
                                // Add control to the course
                                if (!course.Controls.Any(c => c.Code == control.Code))
                                {
                                    course.Controls.Add(control);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Course with code {code} not found in courses list.", courseNo);

                            }
                        }
                    }
                    var datetime = DateTime.ParseExact(arrDate, "dd.MM.yyyy HH:mm:ss", null);
                    if (datetime.Year > 0)
                    {
                        year = datetime.Year;
                    }
                    else
                    {
                        year = DateTime.Now.Year;
                    }
                    metadata.Year = year.ToString();
                    metadata.Date = datetime.ToString("yyyy-MM-dd");
                    metadata.Place = arrPlace;
                    metadata.Courses = courses;
                    metadata.Day = day;
                }

                return metadata;
            }
        }

        public async Task WriteMetadataAsync(string metadatajson, string directory)
        {
            var jsonPath = Path.Combine(directory, "metadata.json");

            // Slett eksisterende filer for å overskrive
            if (File.Exists(jsonPath)) File.Delete(jsonPath);

            await File.WriteAllTextAsync(jsonPath, metadatajson);
        }


        public async Task WriteResultsAsync(List<ParticipantResult> results, string directory)
        {
            var csvPath = Path.Combine(directory, "results.csv");
            var jsonPath = Path.Combine(directory, "results.json");
            var htmlPath = Path.Combine(directory, "results.html");

            // Slett eksisterende filer for å overskrive
            if (File.Exists(csvPath)) File.Delete(csvPath);
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(htmlPath)) File.Delete(htmlPath);

            // Skriv CSV
            using (var csvWriter = new StreamWriter(csvPath, false))
            {
                csvWriter.WriteLine("Id,ECard,FirstName,LastName,Time,Status,StatusMessage,Class,Course,Points,TeamId,TeamName");
                foreach (var result in results)
                {
                    csvWriter.WriteLine($"{result.Id},{result.ECard},{result.FirstName},{result.LastName},{result.Time},{result.Status},{result.StatusMessage},{result.Class},{result.Course},{result.Points},{result.TeamId},{result.TeamName}");
                }
            }

            // Skriv JSON

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(results, options);
            await File.WriteAllTextAsync(jsonPath, json);
            /*
                        // Skriv HTML gruppert etter klasse
                        var groupedByClass = results.GroupBy(r => r.Class);
                        var htmlBuilder = new StringBuilder();
                        htmlBuilder.AppendLine("<html>");
                        htmlBuilder.AppendLine("<head><title>Results by Class</title></head>");
                        htmlBuilder.AppendLine("<body>");
                        htmlBuilder.AppendLine("<h1>Results by Class</h1>");

                        foreach (var group in groupedByClass)
                        {
                            // Sorter gruppen: Først etter status (A øverst, D og B nederst), deretter etter tid (raskest først)
                            var sortedGroup = group
                                .OrderBy(r => r.Status == "D" || r.Status == "B") // Sett D og B nederst
                                .ThenBy(r =>
                                {
                                    // Forsøk å parse tiden til TimeSpan, bruk MaxValue hvis parsing feiler
                                    if (TimeSpan.TryParse(r.Time, out var time))
                                    {
                                        return time;
                                    }
                                    return TimeSpan.MaxValue;
                                });

                            htmlBuilder.AppendLine($"<h2>Class: {group.Key}</h2>");
                            htmlBuilder.AppendLine($"<p>Course: {group.First().Course}</p>");
                            htmlBuilder.AppendLine($"<p>Participants: {group.Count()}</p>"); // Antall deltakere per klasse
                            htmlBuilder.AppendLine("<table border='1'>");
                            htmlBuilder.AppendLine("<thead><tr><th>Position</th><th>FirstName</th><th>LastName</th><th>Time</th><th>Points</th></tr></thead>");
                            htmlBuilder.AppendLine("<tbody>");

                            int position = 1; // Start plassering
                            foreach (var result in sortedGroup)
                            {
                                // Tilpass tid-feltet for status D og B
                                var timeDisplay = result.Status switch
                                {
                                    "D" => "Disk",
                                    "B" => "Brutt",
                                    _ => result.Time
                                };

                                // Plassering er kun for deltakere med gyldig status
                                var positionDisplay = (result.Status == "D" || result.Status == "B") ? "" : position++.ToString();

                                htmlBuilder.AppendLine($"<tr><td>{positionDisplay}</td><td>{result.FirstName}</td><td>{result.LastName}</td><td>{timeDisplay}</td><td>{result.Points}</td></tr>");
                            }
                            htmlBuilder.AppendLine("</tbody>");
                            htmlBuilder.AppendLine("</table>");
                        }

                        htmlBuilder.AppendLine("</body>");
                        htmlBuilder.AppendLine("</html>");

                        await File.WriteAllTextAsync(htmlPath, htmlBuilder.ToString());
            */
            _logger.LogInformation("Data written to CSV: {path}", csvPath);
            _logger.LogInformation("Data written to JSON: {path}", jsonPath);
            //_logger.LogInformation("Data written to HTML: {path}", htmlPath);
        }


        private async Task DeployResultsAsync(RaceMetadata raceMetadata)
        {
            // Set up relative paths
            var path =  $"data\\{raceMetadata.Year}\\{raceMetadata.Date}";
            var metadataPath = Path.Combine(path, "metadata.json");
            var resultsPath = Path.Combine(path, "results.json");

            // The command to run
            //python live_update.py data\2025\2025-08-20\metadata.json data\2025\2025-08-20\results.json
            var command = $"python live_update.py {metadataPath} {resultsPath}";
            _logger.LogInformation("Deploying results with command: {command}", command);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = @"c:\Diverse\LiveDeployer"
            };

            if (!File.Exists(Path.Combine(@"c:\Diverse\LiveDeployer", metadataPath)))
                _logger.LogWarning("Metadata file not found: {file}", Path.Combine(@"c:\Diverse\LiveDeployer", metadataPath));
            if (!File.Exists(Path.Combine(@"c:\Diverse\LiveDeployer", resultsPath)))
                _logger.LogWarning("Results file not found: {file}", Path.Combine(@"c:\Diverse\LiveDeployer", resultsPath));

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error deploying results.\nCommand: {command}\nExitCode: {exitCode}\nOutput: {output}\nError: {error}",
                        command, process.ExitCode, output, error);
                }
                else
                {
                    _logger.LogInformation("Results deployed successfully: {output}", output);
                }
            }
        }

        public class RaceMetadata
        {
            public string Year { get; set; }
            public string Date { get; set; }
            public string Place { get; set; }
            public string Organizer { get; set; }
            public IEnumerable<CourseDto> Courses { get; set; }
            public int Day { get; set; } = -1;
        }
        public class ParticipantResult
        {
            public string Id { get; set; }
            public string ECard { get; set; }
            public string ECard2 { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Time { get; set; }
            public string Status { get; set; }
            public string StatusMessage { get; set; }
            public string Class { get; set; }
            public string Course { get; set; }
            public string Points { get; set; }
            public string TeamId { get; set; }
            public string TeamName { get; set; }
            public IEnumerable<SplitTime> SplitTimes { get; set; } = new List<SplitTime>();
        }

        public class SplitTime
        {
            public int Number { get; set; }
            public string Code { get; set; }
            public int Splittime { get; set; } //in seconds
            public int Totaltime { get; set; } //in seconds
        }
        public class EventMetadataDto
        {
            public string Name { get; set; }
            public string Date { get; set; }
            public string Organizer { get; set; }
            public string Location { get; set; }
            public List<CourseDto> Courses { get; set; }
            public string EventType { get; set; }
            public string TerrainType { get; set; }
        }
        public class CourseDto
        {
            public string Name { get; set; }
            public double Length { get; set; }
            public string Level { get; set; }
            public List<ControlDto> Controls { get; set; } = new List<ControlDto>();
        }
    }
    public class ControlDto
    {
        public int No { get; set; }
        public string Code { get; set; }
    }
}   