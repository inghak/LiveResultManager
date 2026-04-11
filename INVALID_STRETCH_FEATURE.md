# Invalid Stretch Management Feature

## Overview

The Invalid Stretch feature allows race organizers to mark specific control-to-control stretches as invalid for a particular event. When a stretch is marked invalid, the time between those controls is automatically deducted from the final result before sending to Supabase.

## Use Case

Sometimes during a race, a particular stretch between two controls becomes invalid due to:
- Dangerous/blocked paths
- Incorrectly placed controls
- Course changes made during the event

Rather than manually adjusting all results in the timing system, this feature allows you to configure the invalid stretch once, and all affected results are automatically adjusted.

## How It Works

### 1. Configuration Storage

Invalid stretches are stored in a JSON file:
```
C:\ResultsArchive\invalid-stretches.json
```

The file contains:
```json
{
  "Stretches": [
    {
      "Id": "guid-here",
      "EventId": "VBIK Orientering 2026_2026-04-08",
      "EventName": "VBIK Orientering 2026",
      "EventDate": "2026-04-08",
      "FromControlCode": "64",
      "ToControlCode": "58",
      "Description": "Dangerous path closed",
      "CreatedAt": "2026-04-08T18:30:00"
    }
  ]
}
```

### 2. Stretch Matching Logic

When processing results, the system:
1. Checks if the event has any invalid stretches configured
2. For each result, examines the split times to find consecutive controls
3. Matches in **both directions**: 64→58 OR 58→64
4. Calculates the split time between the two controls
5. Deducts that time from the final result time

### 3. Example

**Original Result:**
- Runner: John Doe
- Course: Controls [Start, 45, 64, 58, 92, Finish]
- Time from 64 to 58: 180 seconds
- Final Time: 3600 seconds (1:00:00)

**Invalid Stretch:** 64→58

**Adjusted Result:**
- Final Time: 3420 seconds (57:00) = 3600 - 180
- Status Message: "Adjusted: 64→58: -180s"

### 4. Course Independence

Stretches are defined at the **event level**, not course level. This means:
- The stretch applies to ALL courses in the event that contain those consecutive controls
- Different courses may have the controls in different orders (64→58 or 58→64)
- If a course doesn't have those controls consecutively, no adjustment is made

## User Interface

### Access

Click the **"Manage Invalid Stretches"** button on the main form.

### Dialog Features

**Event Information**
- Shows current event name and date (if metadata is loaded)
- If no event is loaded, warning is displayed

**Current Stretches**
- Lists all invalid stretches for the current event
- Format: `[Event Name] FromControl ↔ ToControl - Description`

**Add New Stretch**
- From Control: Control code (e.g., "64")
- To Control: Control code (e.g., "58")
- Description: Optional reason (e.g., "Path closed due to safety")
- Click "Add Stretch" to save

**Remove Stretch**
- Select a stretch from the list
- Click "Remove Selected"
- Confirm deletion

### Validation

The UI enforces:
- Both From and To controls must be specified
- From and To controls must be different
- Event metadata must be loaded to add stretches

## Technical Architecture

### Core Components

**Models:**
- `InvalidStretch.cs` - Domain model for a single stretch
- `InvalidStretchConfiguration.cs` - Configuration file structure

**Service:**
- `IInvalidStretchService.cs` - Interface
- `InvalidStretchService.cs` - Implementation
  - Loads/saves JSON configuration
  - Finds matching stretches
  - Calculates time adjustments
  - Generates adjustment descriptions

**Integration:**
- `ResultTransferService.cs` - Modified to apply adjustments before sending to Supabase
- `Form1.cs` - Added button and event metadata tracking
- `InvalidStretchManagementForm.cs` - UI dialog

### Data Flow

```
1. User marks stretch 64→58 as invalid via UI
2. Configuration saved to invalid-stretches.json
3. When transfer runs:
   a. Read results from Access DB
   b. Load event metadata
   c. Check for invalid stretches (EventName + EventDate)
   d. For each result:
      - Find if SplitTimes contain 64→58 or 58→64 consecutively
      - Calculate split time between them
      - Deduct from final time
      - Add status message
   e. Send adjusted results to Supabase
```

### Key Methods

**InvalidStretchService:**
```csharp
// Calculate adjustment for a result
int CalculateTimeAdjustment(RaceResult result, string eventId)

// Get human-readable description
string GetAdjustmentDescription(RaceResult result, string eventId)

// CRUD operations
void AddStretch(InvalidStretch stretch)
bool RemoveStretch(string stretchId)
List<InvalidStretch> GetStretchesForEvent(string eventId)
```

**ResultTransferService:**
```csharp
// In ExecuteTransferAsync, before WriteResultsAsync:
var stretches = _invalidStretchService.GetStretchesForEvent(eventId);
if (stretches.Count > 0) {
    foreach (var result in results) {
        var adjustment = _invalidStretchService.CalculateTimeAdjustment(result, eventId);
        // Apply adjustment to result.Time
        // Add description to result.StatusMessage
    }
}
```

## Configuration

The feature is automatically enabled if `IInvalidStretchService` is available in the DI container.

**DependencyInjection.cs:**
```csharp
services.AddSingleton<IInvalidStretchService>(sp =>
{
    var basePath = archiveConfig.BasePath;
    return new InvalidStretchService(basePath);
});
```

## Testing

### Manual Test Procedure

1. **Start the application**
2. **Run a transfer** to load event metadata
3. **Click "Manage Invalid Stretches"**
4. **Add a stretch:**
   - From: 64
   - To: 58
   - Description: "Test stretch"
5. **Trigger another transfer**
6. **Verify in Supabase:**
   - Check that results with 64→58 have reduced times
   - Check that StatusMessage contains adjustment info

### Test Data

Use the included `iofres.xml` which contains results with control sequence including 64→58:
- D16A class has: ...47, 64, 58, 56...
- H70B class has: ...43, 64, 47, 44, 58... (no consecutive 64→58)

Expected behavior:
- D16A results should be adjusted (64→58 are consecutive)
- H70B results should NOT be adjusted (64 and 58 not consecutive)

## Limitations & Future Enhancements

**Current Limitations:**
1. UI only shows stretches for current event (must have metadata loaded)
2. No bulk import/export of stretches
3. No historical tracking of adjustments

**Future Enhancements:**
1. Import stretches from CSV/Excel
2. Apply stretches retroactively to existing Supabase data
3. Report showing which results were adjusted
4. Undo/redo capability
5. Event template for common invalid stretches

## Troubleshooting

**Problem:** "Cannot add stretch: No event metadata loaded"
- **Solution:** Run a transfer first to load event metadata

**Problem:** Stretches not being applied to results
- **Solution:** Check that:
  - The controls are consecutive in the SplitTimes
  - The EventId matches (check event name and date exactly)
  - The service is properly injected (check DI configuration)

**Problem:** JSON file not found
- **Solution:** The file is created automatically on first save
  - Default location: `C:\ResultsArchive\invalid-stretches.json`
  - Can be changed in appsettings.json (Archive.BasePath)

## Related Files

- `o-bergen.LiveResultManager\Core\Models\InvalidStretch.cs`
- `o-bergen.LiveResultManager\Core\Models\InvalidStretchConfiguration.cs`
- `o-bergen.LiveResultManager\Core\Interfaces\IInvalidStretchService.cs`
- `o-bergen.LiveResultManager\Core\Services\InvalidStretchService.cs`
- `o-bergen.LiveResultManager\Core\Services\ResultTransferService.cs`
- `o-bergen.LiveResultManager\UI\InvalidStretchManagementForm.cs`
- `o-bergen.LiveResultManager\Form1.cs`
- `o-bergen.LiveResultManager\Configuration\DependencyInjection.cs`
