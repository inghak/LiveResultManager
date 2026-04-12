using o_bergen.LiveResultManager.Application.DTOs;
using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;
using o_bergen.LiveResultManager.Core.Services;
using o_bergen.LiveResultManager.UI;

namespace o_bergen.LiveResultManager;

/// <summary>
/// Main form for Live Result Manager
/// Provides UI for controlling and monitoring result transfers
/// </summary>
public partial class Form1 : Form
{
    private readonly ResultTransferService _transferService;
    private readonly ConfigurationDto _configuration;
    private readonly IResultSource _resultSource;
    private readonly IResultDestination _resultDestination;
    private readonly IInvalidStretchService? _invalidStretchService;
    private readonly TransferStatistics _statistics;
    private System.Threading.Timer? _pollingTimer;
    private bool _isRunning;
    private bool _statusBlinkState;
    private CancellationTokenSource? _cancellationTokenSource;
    private EventMetadata? _currentEventMetadata;

    public Form1(
        ResultTransferService transferService,
        ConfigurationDto configuration,
        IResultSource resultSource,
        IResultDestination resultDestination,
        IInvalidStretchService? invalidStretchService = null)
    {
        InitializeComponent();

        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _resultSource = resultSource ?? throw new ArgumentNullException(nameof(resultSource));
        _resultDestination = resultDestination ?? throw new ArgumentNullException(nameof(resultDestination));
        _invalidStretchService = invalidStretchService;
        _statistics = new TransferStatistics();

        // Subscribe to transfer service events
        _transferService.LogMessage += OnLogMessage;
        _transferService.StatusChanged += OnStatusChanged;
        _transferService.ProgressChanged += OnProgressChanged;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        // Initialize UI with configuration
        txtAccessDbPath.Text = _configuration.AccessDb.Path;
        numInterval.Value = _configuration.Transfer.IntervalSeconds;

        // Test connections on startup
        _ = TestConnectionsAsync();

        // Load event metadata and invalid stretches
        _ = LoadEventMetadataAsync();

        Log("Application started. Ready to transfer results.", LogLevel.Info);
        Log($"Source: {_resultSource.SourceName}", LogLevel.Info);
        Log($"Destination: {_resultDestination.DestinationName}", LogLevel.Info);
    }

    private async void btnStart_Click(object sender, EventArgs e)
    {
        if (_isRunning)
            return;

        // Validate configuration
        if (string.IsNullOrWhiteSpace(txtAccessDbPath.Text))
        {
            MessageBox.Show("Please specify Access Database path.", "Configuration Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Update UI
        btnStart.Enabled = false;
        btnStop.Enabled = true;
        UpdateStatus(TransferStatus.Running);

        Log("Transfer started.", LogLevel.Success);

        // Start polling timer
        var interval = (int)numInterval.Value * 1000; // Convert to milliseconds
        _pollingTimer = new System.Threading.Timer(async _ => await ExecuteTransferAsync(), null, 0, interval);
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        if (!_isRunning)
            return;

        StopTransfer();
        Log("Transfer stopped by user.", LogLevel.Warning);
    }

    private void btnClearLog_Click(object sender, EventArgs e)
    {
        txtLog.Clear();
        Log("Log cleared.", LogLevel.Info);
    }

    private async void btnManageStretches_Click(object sender, EventArgs e)
    {
        if (_invalidStretchService == null)
        {
            MessageBox.Show(
                "Invalid stretch service is not available.",
                "Feature Unavailable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Load event metadata if not already loaded
        if (_currentEventMetadata == null)
        {
            await LoadEventMetadataAsync();
        }

        using var form = new InvalidStretchManagementForm(_invalidStretchService, _currentEventMetadata);
        form.ShowDialog(this);

        // Refresh invalid stretches display after dialog closes
        UpdateInvalidStretchesDisplay();

        Log("Invalid stretch management dialog closed.", LogLevel.Info);
    }

    private void btnBrowseDb_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Access Database Files|*.mdb;*.accdb|All Files|*.*",
            Title = "Select Access Database",
            InitialDirectory = Path.GetDirectoryName(txtAccessDbPath.Text) ?? @"C:\"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtAccessDbPath.Text = dialog.FileName;
            Log($"Database path changed to: {dialog.FileName}", LogLevel.Info);
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_isRunning)
        {
            var result = MessageBox.Show(
                "Transfer is still running. Do you want to stop it and exit?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            StopTransfer();
        }

        // Cleanup
        _pollingTimer?.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    private async Task ExecuteTransferAsync()
    {
        if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
            return;

        try
        {
            var metadata = await _transferService.ExecuteTransferAsync(_cancellationTokenSource?.Token ?? default);

            // Store event metadata for invalid stretch management
            if (metadata.EventMetadata != null)
            {
                _currentEventMetadata = metadata.EventMetadata;
            }

            // Update statistics
            if (metadata.Success)
            {
                _statistics.RecordRead(metadata.RecordsRead);
                _statistics.RecordWritten(metadata.RecordsWritten);
                _statistics.RecordSuccess(metadata.RecordsWritten);

                // Record invalid stretch adjustments if any
                if (metadata.RecordsAdjustedForInvalidStretch > 0)
                {
                    _statistics.RecordAdjustedForInvalidStretch(metadata.RecordsAdjustedForInvalidStretch);
                }
            }
            else
            {
                _statistics.RecordError();
            }

            // Update UI on main thread
            this.Invoke(() => UpdateStatisticsUI());
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            Log($"Transfer error: {ex.Message}", LogLevel.Error);
            _statistics.RecordError();
            this.Invoke(() => UpdateStatisticsUI());
        }
    }

    private async Task LoadEventMetadataAsync()
    {
        try
        {
            Log("Loading event metadata from database...", LogLevel.Info);

            // Check if source is AccessDB
            if (_resultSource is Infrastructure.Sources.AccessDbResultSource accessDbSource)
            {
                _currentEventMetadata = await accessDbSource.FetchMetadataAsync(CancellationToken.None);
                Log($"Event metadata loaded: {_currentEventMetadata.Name} on {_currentEventMetadata.Date:yyyy-MM-dd}", LogLevel.Success);

                // Load and display invalid stretches for this event
                UpdateInvalidStretchesDisplay();
            }
            else
            {
                Log("Event metadata loading is only supported for AccessDB source.", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to load event metadata: {ex.Message}", LogLevel.Error);
            MessageBox.Show(
                $"Failed to load event metadata: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task TestConnectionsAsync()
    {
        try
        {
            var (sourceOk, destinationOk) = await _transferService.TestConnectionsAsync();

            // Update Supabase status
            lblSupabaseStatus.Text = destinationOk
                ? "Supabase: ● Connected"
                : "Supabase: ● Not Connected";

            lblSupabaseStatus.ForeColor = destinationOk ? Color.Green : Color.Red;

            if (!sourceOk)
            {
                Log("Warning: Source connection test failed. Using fallback/mock source.", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"Connection test error: {ex.Message}", LogLevel.Error);
        }
    }

    private void StopTransfer()
    {
        _isRunning = false;
        _pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _cancellationTokenSource?.Cancel();

        // Update UI
        btnStart.Enabled = true;
        btnStop.Enabled = false;
        UpdateStatus(TransferStatus.Idle);
    }

    private void OnLogMessage(object? sender, string message)
    {
        Log(message, LogLevel.Info);
    }

    private void OnStatusChanged(object? sender, TransferStatus status)
    {
        this.Invoke(() => UpdateStatus(status));
    }

    private void OnProgressChanged(object? sender, TransferProgressEventArgs e)
    {
        // Progress updates no longer needed (no progress bar)
    }

    private void UpdateStatus(TransferStatus status)
    {
        var (text, badgeText, color) = status switch
        {
            TransferStatus.Idle => ("Ready to start transfer", "● IDLE", Color.Gray),
            TransferStatus.Running => ("Transfer in progress...", "● RUNNING", Color.DodgerBlue),
            TransferStatus.Success => ("Last transfer successful", "● SUCCESS", Color.LimeGreen),
            TransferStatus.Error => ("Transfer failed", "● ERROR", Color.Red),
            TransferStatus.Cancelled => ("Transfer cancelled", "● CANCELLED", Color.Orange),
            _ => ("Unknown status", "● UNKNOWN", Color.Black)
        };

        lblStatus.Text = text;
        lblStatusBadge.Text = badgeText;
        lblStatusBadge.BackColor = color;
    }

    private void timerStatusBlink_Tick(object? sender, EventArgs e)
    {
        if (!_isRunning)
            return;

        // Gentle pulsing effect for running status
        _statusBlinkState = !_statusBlinkState;
        lblStatusBadge.BackColor = _statusBlinkState 
            ? Color.DodgerBlue 
            : Color.FromArgb(100, 135, 206, 235); // Lighter blue
    }

    private void UpdateStatisticsUI()
    {
        lblRecordsRead.Text = $"Records Read: {_statistics.TotalRecordsTransferred}";
        lblRecordsWritten.Text = $"Records Written: {_statistics.TotalRecordsTransferred}";
        lblSuccessRate.Text = $"Success Rate: {_statistics.SuccessRate:F1}%";
        lblLastRead.Text = $"Last read: {_statistics.LastReadCount} records";
        lblLastWritten.Text = $"Last written: {_statistics.LastWrittenCount} records";

        // Update invalid stretch adjustments if any
        if (_statistics.AdjustedForInvalidStretchCount > 0)
        {
            lblAdjustedStretches.Text = $"Adjusted for invalid stretches: {_statistics.AdjustedForInvalidStretchCount} results";
            lblAdjustedStretches.ForeColor = Color.DarkOrange;
        }
        else
        {
            lblAdjustedStretches.Text = "Adjusted for invalid stretches: 0 results";
            lblAdjustedStretches.ForeColor = Color.Gray;
        }
    }

    private void UpdateInvalidStretchesDisplay()
    {
        if (_invalidStretchService == null || _currentEventMetadata == null)
        {
            lblInvalidStretches.Text = "Invalid stretches: None configured";
            lblInvalidStretches.ForeColor = Color.Gray;
            return;
        }

        var eventId = $"{_currentEventMetadata.Name}_{_currentEventMetadata.Date:yyyy-MM-dd}";
        var stretches = _invalidStretchService.GetStretchesForEvent(eventId);

        if (stretches.Count == 0)
        {
            lblInvalidStretches.Text = "Invalid stretches: None configured";
            lblInvalidStretches.ForeColor = Color.Gray;
        }
        else
        {
            var stretchList = string.Join(", ", stretches.Select(s => $"{s.FromControlCode}↔{s.ToControlCode}"));
            lblInvalidStretches.Text = $"Invalid stretches: {stretchList}";
            lblInvalidStretches.ForeColor = Color.DarkRed;

            Log($"Active invalid stretches: {stretchList}", LogLevel.Info);
        }
    }

    private void Log(string message, LogLevel level)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => Log(message, level));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = level switch
        {
            LogLevel.Success => "[SUCCESS]",
            LogLevel.Error => "[ERROR]",
            LogLevel.Warning => "[WARNING]",
            LogLevel.Info => "[INFO]",
            _ => "[LOG]"
        };

        var color = level switch
        {
            LogLevel.Success => Color.Green,
            LogLevel.Error => Color.Red,
            LogLevel.Warning => Color.Orange,
            LogLevel.Info => Color.Black,
            _ => Color.Black
        };

        var logLine = $"[{timestamp}] {prefix} {message}";
        txtLog.AppendText(logLine + Environment.NewLine);

        // Auto-scroll to bottom
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
    }

    private enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
}

