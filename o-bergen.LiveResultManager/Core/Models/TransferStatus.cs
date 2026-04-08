namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Represents the current status of a transfer operation
/// </summary>
public enum TransferStatus
{
    /// <summary>
    /// No transfer is currently running
    /// </summary>
    Idle,

    /// <summary>
    /// Transfer is currently in progress
    /// </summary>
    Running,

    /// <summary>
    /// Last transfer completed successfully
    /// </summary>
    Success,

    /// <summary>
    /// Last transfer failed with an error
    /// </summary>
    Error,

    /// <summary>
    /// Transfer was cancelled by user
    /// </summary>
    Cancelled
}
