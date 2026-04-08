using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Infrastructure.Sources;

/// <summary>
/// Mock result source for testing without a real database
/// </summary>
public class MockResultSource : IResultSource
{
    private readonly List<RaceResult> _mockResults;

    public string SourceName => "Mock Source (Testing)";

    public MockResultSource()
    {
        _mockResults = GenerateMockResults();
    }

    public Task<IReadOnlyList<RaceResult>> ReadResultsAsync(CancellationToken cancellationToken = default)
    {
        // Simulate some delay
        return Task.FromResult<IReadOnlyList<RaceResult>>(_mockResults);
    }

    public Task<DateTime?> GetLastModifiedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DateTime?>(DateTime.Now.AddMinutes(-5));
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static List<RaceResult> GenerateMockResults()
    {
        return new List<RaceResult>
        {
            new RaceResult
            {
                Id = "1",
                ECard = "123456",
                FirstName = "Ola",
                LastName = "Nordmann",
                Class = "H21",
                Course = "Lang",
                Status = "A",
                Time = "30:50",
                Points = "100",
                TeamId = "T1",
                TeamName = "Team Alpha",
                SplitTimes = new List<SplitTime>
                {
                    new SplitTime { Number = 1, Code = "101", Totaltime = 300, Splittime = 300 },
                    new SplitTime { Number = 2, Code = "102", Totaltime = 650, Splittime = 350 },
                    new SplitTime { Number = 3, Code = "103", Totaltime = 1100, Splittime = 450 }
                }
            },
            new RaceResult
            {
                Id = "2",
                ECard = "234567",
                FirstName = "Kari",
                LastName = "Hansen",
                Class = "D21",
                Course = "Mellomlang",
                Status = "A",
                Time = "27:00",
                Points = "95",
                TeamId = "T2",
                TeamName = "Team Beta",
                SplitTimes = new List<SplitTime>
                {
                    new SplitTime { Number = 1, Code = "201", Totaltime = 280, Splittime = 280 },
                    new SplitTime { Number = 2, Code = "202", Totaltime = 600, Splittime = 320 }
                }
            },
            new RaceResult
            {
                Id = "3",
                ECard = "345678",
                FirstName = "Per",
                LastName = "Olsen",
                Class = "H40",
                Course = "Kort",
                Status = "D",
                StatusMessage = "Mispunch",
                Points = "0",
                TeamId = "T1",
                TeamName = "Team Alpha"
            }
        };
    }
}
