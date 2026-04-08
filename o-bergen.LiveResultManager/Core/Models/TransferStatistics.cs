namespace o_bergen.LiveResultManager.Core.Models;

/// <summary>
/// Statistics for result transfer operations
/// </summary>
public class TransferStatistics
{
    private int _successCount;
    private int _errorCount;
    private int _totalRecordsTransferred;
    private int _lastReadCount;
    private int _lastWrittenCount;

    /// <summary>
    /// Number of successful transfers
    /// </summary>
    public int SuccessCount => _successCount;

    /// <summary>
    /// Number of failed transfers
    /// </summary>
    public int ErrorCount => _errorCount;

    /// <summary>
    /// Total number of records transferred
    /// </summary>
    public int TotalRecordsTransferred => _totalRecordsTransferred;

    /// <summary>
    /// Number of records read in last operation
    /// </summary>
    public int LastReadCount => _lastReadCount;

    /// <summary>
    /// Number of records written in last operation
    /// </summary>
    public int LastWrittenCount => _lastWrittenCount;

    /// <summary>
    /// Success rate as a percentage (0-100)
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = _successCount + _errorCount;
            return total == 0 ? 100 : (_successCount * 100.0) / total;
        }
    }

    /// <summary>
    /// Timestamp of the last successful transfer
    /// </summary>
    public DateTime? LastTransferTime { get; private set; }

    /// <summary>
    /// Records a successful transfer
    /// </summary>
    public void RecordSuccess(int recordsTransferred)
    {
        _successCount++;
        _totalRecordsTransferred += recordsTransferred;
        LastTransferTime = DateTime.Now;
    }

    /// <summary>
    /// Records a failed transfer
    /// </summary>
    public void RecordError()
    {
        _errorCount++;
    }

    /// <summary>
    /// Records a read operation from source
    /// </summary>
    public void RecordRead(int count)
    {
        _lastReadCount = count;
    }

    /// <summary>
    /// Records a write operation to destination
    /// </summary>
    public void RecordWritten(int count)
    {
        _lastWrittenCount = count;
    }

    /// <summary>
    /// Resets all statistics
    /// </summary>
    public void Reset()
    {
        _successCount = 0;
        _errorCount = 0;
        _totalRecordsTransferred = 0;
        _lastReadCount = 0;
        _lastWrittenCount = 0;
        LastTransferTime = null;
    }
}
