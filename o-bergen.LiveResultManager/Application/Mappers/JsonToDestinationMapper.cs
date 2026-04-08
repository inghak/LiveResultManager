using o_bergen.LiveResultManager.Application.DTOs;
using o_bergen.LiveResultManager.Core.Models;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps JSON DTOs back to domain models
/// </summary>
public class JsonToDestinationMapper
{
    /// <summary>
    /// Maps ResultsJsonDto to a list of RaceResults
    /// </summary>
    public List<RaceResult> MapFromDto(ResultsJsonDto dto)
    {
        if (dto?.Results == null)
            return new List<RaceResult>();

        return dto.Results.Select(MapResultFromDto).ToList();
    }

    /// <summary>
    /// Maps a single ResultDto to RaceResult
    /// </summary>
    private RaceResult MapResultFromDto(ResultDto dto)
    {
        return new RaceResult
        {
            Id = dto.Id ?? string.Empty,
            ECard = dto.ECard ?? string.Empty,
            ECard2 = dto.ECard2,
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            Time = dto.Time,
            Status = dto.Status ?? string.Empty,
            StatusMessage = dto.StatusMessage,
            Class = dto.Class ?? string.Empty,
            Course = dto.Course ?? string.Empty,
            Points = dto.Points ?? string.Empty,
            TeamId = dto.TeamId,
            TeamName = dto.TeamName,
            SplitTimes = dto.SplitTimes?.Select(st => new SplitTime
            {
                Number = st.Number,
                Code = st.Code ?? string.Empty,
                Splittime = st.Splittime,
                Totaltime = st.Totaltime
            }).ToList() ?? new List<SplitTime>()
        };
    }
}
