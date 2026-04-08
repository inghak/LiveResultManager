using o_bergen.LiveResultManager.Application.DTOs;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps domain models to JSON DTOs for archiving
/// </summary>
public class SourceToJsonMapper
{
    /// <summary>
    /// Maps a list of RaceResults to ResultsJsonDto
    /// </summary>
    public ResultsJsonDto MapToDto(IReadOnlyList<RaceResult> results)
    {
        return new ResultsJsonDto
        {
            Version = "1.0",
            GeneratedAt = DateTime.Now,
            Results = results.Select(MapResultToDto).ToList()
        };
    }

    /// <summary>
    /// Maps a single RaceResult to ResultDto
    /// </summary>
    private ResultDto MapResultToDto(RaceResult result)
    {
        return new ResultDto
        {
            Id = result.Id,
            ECard = result.ECard,
            ECard2 = result.ECard2,
            FirstName = result.FirstName,
            LastName = result.LastName,
            Time = result.Time,
            Status = result.Status,
            StatusMessage = result.StatusMessage,
            Class = result.Class,
            Course = result.Course,
            Points = result.Points,
            TeamId = result.TeamId,
            TeamName = result.TeamName,
            SplitTimes = result.SplitTimes?.Select(st => new SplitTimeDto
            {
                Number = st.Number,
                Code = st.Code,
                Splittime = st.Splittime,
                Totaltime = st.Totaltime
            }).ToList()
        };
    }

    /// <summary>
    /// Maps ResultMetadata to MetadataJsonDto
    /// </summary>
    public MetadataJsonDto MapToDto(ResultMetadata metadata)
    {
        return new MetadataJsonDto
        {
            TransferDate = metadata.TransferDate,
            SourceName = metadata.SourceName,
            DestinationName = metadata.DestinationName,
            RecordsRead = metadata.RecordsRead,
            RecordsWritten = metadata.RecordsWritten,
            Success = metadata.Success,
            ErrorMessage = metadata.ErrorMessage,
            DurationSeconds = metadata.Duration?.TotalSeconds,
            ArchivePath = metadata.ArchivePath
        };
    }
}
