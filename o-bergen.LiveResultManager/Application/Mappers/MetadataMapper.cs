using o_bergen.LiveResultManager.Application.DTOs;
using o_bergen.LiveResultManager.Core.Models;
using CoreControl = o_bergen.LiveResultManager.Core.Models.Control;

namespace o_bergen.LiveResultManager.Application.Mappers;

/// <summary>
/// Maps metadata between domain and DTO models
/// </summary>
public class MetadataMapper
{
    /// <summary>
    /// Maps MetadataJsonDto to ResultMetadata
    /// </summary>
    public ResultMetadata MapFromDto(MetadataJsonDto dto)
    {
        return new ResultMetadata
        {
            EventMetadata = dto.Event != null ? MapEventMetadataFromDto(dto.Event) : null,
            TransferDate = dto.TransferDate,
            SourceName = dto.SourceName,
            DestinationName = dto.DestinationName,
            RecordsRead = dto.RecordsRead,
            RecordsWritten = dto.RecordsWritten,
            RecordsDeleted = dto.RecordsDeleted,
            Success = dto.Success,
            ErrorMessage = dto.ErrorMessage,
            Duration = dto.DurationSeconds.HasValue 
                ? TimeSpan.FromSeconds(dto.DurationSeconds.Value) 
                : null,
            ArchivePath = dto.ArchivePath
        };
    }

    /// <summary>
    /// Maps ResultMetadata to MetadataJsonDto
    /// </summary>
    public MetadataJsonDto MapToDto(ResultMetadata metadata)
    {
        return new MetadataJsonDto
        {
            Event = metadata.EventMetadata != null ? MapEventMetadataToDto(metadata.EventMetadata) : null,
            TransferDate = metadata.TransferDate,
            SourceName = metadata.SourceName,
            DestinationName = metadata.DestinationName,
            RecordsRead = metadata.RecordsRead,
            RecordsWritten = metadata.RecordsWritten,
            RecordsDeleted = metadata.RecordsDeleted,
            Success = metadata.Success,
            ErrorMessage = metadata.ErrorMessage,
            DurationSeconds = metadata.Duration?.TotalSeconds,
            ArchivePath = metadata.ArchivePath
        };
    }

    private EventMetadata MapEventMetadataFromDto(EventMetadataDto dto)
    {
        return new EventMetadata
        {
            Name = dto.Name,
            Date = dto.Date,
            Organizer = dto.Organizer,
            Location = dto.Location,
            EventType = dto.EventType,
            TerrainType = dto.TerrainType,
            Courses = dto.Courses.Select(c => new Course
            {
                Name = c.Name,
                Level = c.Level,
                Length = c.Length,
                Controls = c.Controls.Select(ctrl => new CoreControl
                {
                    No = ctrl.No,
                    Code = ctrl.Code
                }).ToList()
            }).ToList()
        };
    }

    private EventMetadataDto MapEventMetadataToDto(EventMetadata metadata)
    {
        return new EventMetadataDto
        {
            Name = metadata.Name,
            Date = metadata.Date,
            Organizer = metadata.Organizer,
            Location = metadata.Location,
            EventType = metadata.EventType,
            TerrainType = metadata.TerrainType,
            Courses = metadata.Courses.Select(c => new CourseDto
            {
                Name = c.Name,
                Level = c.Level,
                Length = c.Length,
                Controls = c.Controls.Select(ctrl => new ControlDto
                {
                    No = ctrl.No,
                    Code = ctrl.Code
                }).ToList()
            }).ToList()
        };
    }
}
