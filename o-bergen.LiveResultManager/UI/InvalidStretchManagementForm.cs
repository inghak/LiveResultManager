using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Models;
using System.Windows.Forms;

namespace o_bergen.LiveResultManager.UI;

/// <summary>
/// Form for managing invalid stretches for a specific event
/// </summary>
public partial class InvalidStretchManagementForm : Form
{
    private readonly IInvalidStretchService _stretchService;
    private readonly EventMetadata? _currentEvent;
    private ListBox lstStretches = null!;
    private TextBox txtFromControl = null!;
    private TextBox txtToControl = null!;
    private TextBox txtDescription = null!;
    private Button btnAdd = null!;
    private Button btnRemove = null!;
    private Button btnClose = null!;
    private Label lblEventInfo = null!;
    private Label lblFromControl = null!;
    private Label lblToControl = null!;
    private Label lblDescription = null!;

    public InvalidStretchManagementForm(IInvalidStretchService stretchService, EventMetadata? currentEvent)
    {
        _stretchService = stretchService ?? throw new ArgumentNullException(nameof(stretchService));
        _currentEvent = currentEvent;

        InitializeComponents();
        LoadStretches();
    }

    private void InitializeComponents()
    {
        Text = "Invalid Stretch Management";
        Width = 600;
        Height = 500;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        // Event Info Label
        lblEventInfo = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(550, 40),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        if (_currentEvent != null)
        {
            lblEventInfo.Text = $"Event: {_currentEvent.Name}\nDate: {_currentEvent.Date}";
        }
        else
        {
            lblEventInfo.Text = "No event metadata loaded.\nStretches will not be applied until event is set.";
            lblEventInfo.ForeColor = Color.DarkRed;
        }

        // Stretches List
        var lblStretches = new Label
        {
            Text = "Current Invalid Stretches:",
            Location = new Point(20, 80),
            Size = new Size(200, 20)
        };

        lstStretches = new ListBox
        {
            Location = new Point(20, 105),
            Size = new Size(550, 150)
        };
        lstStretches.SelectedIndexChanged += (s, e) => UpdateButtonStates();

        // Add Stretch Section
        var grpAdd = new GroupBox
        {
            Text = "Add New Invalid Stretch",
            Location = new Point(20, 270),
            Size = new Size(550, 120)
        };

        lblFromControl = new Label
        {
            Text = "From Control:",
            Location = new Point(10, 25),
            Size = new Size(100, 20)
        };

        txtFromControl = new TextBox
        {
            Location = new Point(110, 23),
            Size = new Size(80, 20)
        };

        lblToControl = new Label
        {
            Text = "To Control:",
            Location = new Point(210, 25),
            Size = new Size(100, 20)
        };

        txtToControl = new TextBox
        {
            Location = new Point(310, 23),
            Size = new Size(80, 20)
        };

        lblDescription = new Label
        {
            Text = "Description (optional):",
            Location = new Point(10, 55),
            Size = new Size(130, 20)
        };

        txtDescription = new TextBox
        {
            Location = new Point(145, 53),
            Size = new Size(390, 20)
        };

        btnAdd = new Button
        {
            Text = "Add Stretch",
            Location = new Point(410, 85),
            Size = new Size(125, 25)
        };
        btnAdd.Click += BtnAdd_Click;

        grpAdd.Controls.AddRange(new System.Windows.Forms.Control[]
        {
            lblFromControl, txtFromControl,
            lblToControl, txtToControl,
            lblDescription, txtDescription,
            btnAdd
        });

        // Remove Button
        btnRemove = new Button
        {
            Text = "Remove Selected",
            Location = new Point(20, 405),
            Size = new Size(125, 30),
            Enabled = false
        };
        btnRemove.Click += BtnRemove_Click;

        // Close Button
        btnClose = new Button
        {
            Text = "Close",
            Location = new Point(445, 405),
            Size = new Size(125, 30),
            DialogResult = DialogResult.OK
        };

        Controls.AddRange(new System.Windows.Forms.Control[]
        {
            lblEventInfo,
            lblStretches,
            lstStretches,
            grpAdd,
            btnRemove,
            btnClose
        });

        AcceptButton = btnAdd;
    }

    private void LoadStretches()
    {
        lstStretches.Items.Clear();

        if (_currentEvent == null)
        {
            lstStretches.Items.Add("(No event loaded - showing all stretches)");
            var allStretches = _stretchService.GetAllStretches();
            foreach (var stretch in allStretches)
            {
                lstStretches.Items.Add(new StretchListItem(stretch));
            }
        }
        else
        {
            var eventId = InvalidStretch.CreateEventId(_currentEvent.Name, _currentEvent.Date);
            var stretches = _stretchService.GetStretchesForEvent(eventId);

            if (stretches.Count == 0)
            {
                lstStretches.Items.Add("(No invalid stretches defined for this event)");
            }
            else
            {
                foreach (var stretch in stretches)
                {
                    lstStretches.Items.Add(new StretchListItem(stretch));
                }
            }
        }

        UpdateButtonStates();
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (_currentEvent == null)
        {
            MessageBox.Show(
                "Cannot add stretch: No event metadata loaded.\n" +
                "Please ensure the event is running and metadata is available.",
                "Event Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var fromControl = txtFromControl.Text.Trim();
        var toControl = txtToControl.Text.Trim();

        if (string.IsNullOrWhiteSpace(fromControl) || string.IsNullOrWhiteSpace(toControl))
        {
            MessageBox.Show(
                "Please enter both From and To control codes.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (fromControl == toControl)
        {
            MessageBox.Show(
                "From and To controls must be different.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var stretch = new InvalidStretch
        {
            EventId = InvalidStretch.CreateEventId(_currentEvent.Name, _currentEvent.Date),
            EventName = _currentEvent.Name,
            EventDate = _currentEvent.Date,
            FromControlCode = fromControl,
            ToControlCode = toControl,
            Description = txtDescription.Text.Trim()
        };

        _stretchService.AddStretch(stretch);

        Task.Run(async () => await _stretchService.SaveAsync()).Wait();

        MessageBox.Show(
            $"Invalid stretch {fromControl} ↔ {toControl} added successfully.",
            "Success",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        txtFromControl.Clear();
        txtToControl.Clear();
        txtDescription.Clear();
        LoadStretches();
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (lstStretches.SelectedItem is not StretchListItem item)
            return;

        var result = MessageBox.Show(
            $"Are you sure you want to remove the stretch:\n{item.Stretch}?",
            "Confirm Removal",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _stretchService.RemoveStretch(item.Stretch.Id);
            Task.Run(async () => await _stretchService.SaveAsync()).Wait();

            MessageBox.Show(
                "Stretch removed successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            LoadStretches();
        }
    }

    private void UpdateButtonStates()
    {
        btnRemove.Enabled = lstStretches.SelectedItem is StretchListItem;
    }

    private class StretchListItem
    {
        public InvalidStretch Stretch { get; }

        public StretchListItem(InvalidStretch stretch)
        {
            Stretch = stretch;
        }

        public override string ToString()
        {
            var eventInfo = string.IsNullOrEmpty(Stretch.EventName) 
                ? "" 
                : $"[{Stretch.EventName}] ";
            
            return $"{eventInfo}{Stretch.FromControlCode} ↔ {Stretch.ToControlCode}" +
                   (string.IsNullOrWhiteSpace(Stretch.Description) ? "" : $" - {Stretch.Description}");
        }
    }
}
