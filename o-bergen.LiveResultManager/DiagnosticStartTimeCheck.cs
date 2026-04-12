using System.Data.Odbc;

namespace o_bergen.LiveResultManager.Diagnostics;

/// <summary>
/// Diagnostic tool to examine the format of the 'start' field in the Name table
/// </summary>
public class DiagnosticStartTimeCheck
{
    public static async Task CheckStartTimeFormat(string dbPath)
    {
        var connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={dbPath};";
        
        Console.WriteLine("=== Diagnostic: Checking 'start' field format in Name table ===\n");
        
        using var connection = new OdbcConnection(connectionString);
        await connection.OpenAsync();

        // Query a few records to examine the start field
        var query = @"
            SELECT TOP 10
                n.id, 
                n.ename, 
                n.name, 
                n.start
            FROM Name n 
            WHERE n.status IN ('A','D','B','S','OK')
            ORDER BY n.id";

        using var command = new OdbcCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        int recordCount = 0;
        while (await reader.ReadAsync())
        {
            recordCount++;
            var id = reader["id"].ToString()?.Trim();
            var firstName = reader["ename"].ToString()?.Trim();
            var lastName = reader["name"].ToString()?.Trim();
            
            var startOrdinal = reader.GetOrdinal("start");
            
            Console.WriteLine($"Record #{recordCount}: {firstName} {lastName} (ID: {id})");
            
            if (reader.IsDBNull(startOrdinal))
            {
                Console.WriteLine("  - Start field: NULL");
            }
            else
            {
                var rawValue = reader.GetValue(startOrdinal);
                var type = rawValue?.GetType();
                
                Console.WriteLine($"  - Type: {type?.FullName}");
                Console.WriteLine($"  - Raw Value: {rawValue}");
                Console.WriteLine($"  - ToString(): {rawValue?.ToString()}");
                
                // Try different interpretations
                if (rawValue is DateTime dt)
                {
                    Console.WriteLine($"  - As DateTime: {dt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  - Time only: {dt:HH:mm}");
                    Console.WriteLine($"  - TimeOfDay: {dt.TimeOfDay}");
                }
                else if (rawValue is double d)
                {
                    Console.WriteLine($"  - As Double: {d}");
                    // Try OLE Automation date
                    try
                    {
                        var oleDate = DateTime.FromOADate(d);
                        Console.WriteLine($"  - As OLE Date: {oleDate:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"  - Time only: {oleDate:HH:mm}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  - OLE Date conversion failed: {ex.Message}");
                    }
                }
                else if (rawValue is string s)
                {
                    Console.WriteLine($"  - String Length: {s.Length}");
                    Console.WriteLine($"  - Trimmed: '{s.Trim()}'");
                }
                else if (rawValue is int || rawValue is long || rawValue is short)
                {
                    Console.WriteLine($"  - As Integer: {rawValue}");
                }
            }
            
            Console.WriteLine();
        }
        
        if (recordCount == 0)
        {
            Console.WriteLine("No records found in Name table with matching status.");
        }
        
        Console.WriteLine($"\nTotal records examined: {recordCount}");
        Console.WriteLine("=== End Diagnostic ===");
    }
}
