# Sploype CSV Encoding Configuration

## Overview

The Sploype CSV file format is used by World of O WinSplits software. This application supports two encoding modes for maximum compatibility.

## Configuration

Set the encoding mode in `appsettings.json`:

```json
{
  "Archive": {
    "BasePath": "C:\\ResultsArchive",
    "KeepDays": 90,
    "SploypeCsvEncoding": "windows-1252"
  }
}
```

## Encoding Modes

### 1. `"windows-1252"` (Default - Legacy Mode)

**Use this for**: World of O WinSplits compatibility (current requirement)

- **Encoding**: Windows-1252 (ANSI / Western European)
- **BOM**: No BOM
- **Norwegian characters**: æøå encoded as single bytes
- **File size**: More compact than UTF-8
- **Compatibility**: Works with legacy WinSplits software

**Example:**
```json
"SploypeCsvEncoding": "windows-1252"
```

### 2. `"utf-8-bom"` (Modern Mode)

**Use this for**: Future compatibility when World of O supports UTF-8

- **Encoding**: UTF-8 with Byte Order Mark (BOM)
- **BOM**: `\uFEFF` at file start
- **Norwegian characters**: æøå encoded as multi-byte UTF-8
- **File size**: Slightly larger than Windows-1252
- **Compatibility**: Modern standard, better international support

**Example:**
```json
"SploypeCsvEncoding": "utf-8-bom"
```

## Migration Path

### Current State (2025)
World of O WinSplits expects **Windows-1252** encoding. Use:
```json
"SploypeCsvEncoding": "windows-1252"
```

### Future State
When World of O adds UTF-8 support, switch to:
```json
"SploypeCsvEncoding": "utf-8-bom"
```

## Testing

Use the `EncodingValidationTest` class to verify encoding functionality:

```csharp
if (EncodingValidationTest.ValidateEncoding(out string message))
{
    MessageBox.Show(message, "Encoding Test Passed");
}
```

## Technical Details

### Windows-1252 Output
- File saved with code page 1252
- No BOM in file
- Norwegian characters (æøå ÆØÅ) use single bytes:
  - æ = 0xE6
  - ø = 0xF8
  - å = 0xE5
  - Æ = 0xC6
  - Ø = 0xD8
  - Å = 0xC5

### UTF-8 with BOM Output
- File saved with UTF-8 encoding
- BOM (0xEF 0xBB 0xBF) at start
- Norwegian characters use 2 bytes:
  - æ = 0xC3 0xA6
  - ø = 0xC3 0xB8
  - å = 0xC3 0xA5
  - etc.

## Affected Components

The following components respect the encoding setting:

1. **SploypeCsvMapper** - Adds BOM if configured
2. **SploypeCsvArchive** - Uses correct encoding for local files
3. **SupabaseStorageArchive** - Uses correct encoding for cloud uploads

## Troubleshooting

### Symptoms: æøå appears as garbage in WinSplits
**Solution**: Ensure `"SploypeCsvEncoding": "windows-1252"` in appsettings.json

### Symptoms: Need UTF-8 for modern tools
**Solution**: Change to `"SploypeCsvEncoding": "utf-8-bom"`

### Symptoms: Encoding not registered error
**Solution**: Verify `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` is called in `Program.cs`
