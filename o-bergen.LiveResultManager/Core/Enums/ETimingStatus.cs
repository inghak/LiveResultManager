namespace LiveResultManager.Core.Enums;

/// &lt;summary&gt;
/// Norwegian eTiming system database status codes.
/// These single-letter codes are used in the Access database.
/// &lt;/summary&gt;
public enum ETimingStatus
{
    /// &lt;summary&gt;
    /// A = Approved/OK - Competitor finished successfully.
    /// &lt;/summary&gt;
    A,
    
    /// &lt;summary&gt;
    /// B = Brutt (Norwegian: broken/abandoned) - Did not finish.
    /// &lt;/summary&gt;
    B,

    /// &lt;summary&gt;
    /// C = Did not start.
    /// &lt;/summary&gt;
    C,

    /// &lt;summary&gt;
    /// D = Disqualified.
    /// &lt;/summary&gt;
    D,

    /// &lt;summary&gt;
    /// S = Unknown status - Still in the woods ("i skogen").
    /// No direct IOF 3.0 equivalent, but flows to live results.
    /// &lt;/summary&gt;
    S,
    
    /// &lt;summary&gt;
    /// Unknown or unrecognized status.
    /// &lt;/summary&gt;
    Unknown
}
