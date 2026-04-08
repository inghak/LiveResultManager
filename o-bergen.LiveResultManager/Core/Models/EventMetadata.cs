namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Event metadata from Access Database
/// </summary>
public class EventMetadata
{
    public string Name { get; set; } = "Bedriftsløp";
    public string Date { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Organizer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<Course> Courses { get; set; } = new();
    public string EventType { get; set; } = "Normal";
    public string TerrainType { get; set; } = "Skog";
    public int Day { get; set; } = -1;
}

/// <summary>
/// Represents a course in the event
/// </summary>
public class Course
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public double Length { get; set; }
    public List<Control> Controls { get; set; } = new();
}

/// <summary>
/// Represents a control point on a course
/// </summary>
public class Control
{
    public int No { get; set; }
    public string Code { get; set; } = string.Empty;
}
