namespace LiveResultManager.Core.Enums;

/// &lt;summary&gt;
/// IOF XML 3.0 standard competitor status values.
/// Based on the IOF Data Standard 3.0 specification.
/// &lt;/summary&gt;
public enum CompetitorStatus
{
    /// &lt;summary&gt;
    /// Finished and validated.
    /// &lt;/summary&gt;
    OK,
    
    /// &lt;summary&gt;
    /// Disqualified (e.g. for anti-doping violation).
    /// &lt;/summary&gt;
    Disqualified,
    
    /// &lt;summary&gt;
    /// Did not start (i.e. did not enter the start zone).
    /// &lt;/summary&gt;
    DidNotStart,
    
    /// &lt;summary&gt;
    /// Did not finish (i.e. started but did not reach the finish line).
    /// &lt;/summary&gt;
    DidNotFinish,
    
    /// &lt;summary&gt;
    /// Missing punch (i.e. one or more controls were not punched).
    /// &lt;/summary&gt;
    MissingPunch,
    
    /// &lt;summary&gt;
    /// Overtime (i.e. exceeded the maximum allowed time).
    /// &lt;/summary&gt;
    OverTime,
    
    /// &lt;summary&gt;
    /// Not competing (i.e. running outside competition).
    /// &lt;/summary&gt;
    NotCompeting,
    
    /// &lt;summary&gt;
    /// Cancelled (i.e. the competitor's results were cancelled by himself/herself after the event).
    /// &lt;/summary&gt;
    Cancelled,
    
    /// &lt;summary&gt;
    /// Inactive (i.e. the competitor chose to not participate in the race).
    /// &lt;/summary&gt;
    Inactive,
    
    /// &lt;summary&gt;
    /// Moved (i.e. the competitor was moved to another class).
    /// &lt;/summary&gt;
    Moved,
    
    /// &lt;summary&gt;
    /// Moved up (i.e. the competitor was moved to a higher class).
    /// &lt;/summary&gt;
    MovedUp,

    /// &lt;summary&gt;
    /// Unknown status.
    /// &lt;/summary&gt;
    Unknown
}
