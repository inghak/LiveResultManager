using System.Text;

namespace o_bergen.LiveResultManager;

/// <summary>
/// Simple validation test for Windows-1252 encoding support
/// Run this from Form1 or create a button to execute ValidateEncoding()
/// </summary>
public static class EncodingValidationTest
{
    /// <summary>
    /// Validates that Windows-1252 encoding is available and works correctly
    /// Returns true if all tests pass, false otherwise
    /// </summary>
    public static bool ValidateEncoding(out string message)
    {
        var sb = new StringBuilder();
        bool allTestsPassed = true;

        try
        {
            // Test 1: Check if Windows-1252 encoding is available
            sb.AppendLine("Test 1: Windows-1252 encoding availability");
            Encoding encoding1252;
            try
            {
                encoding1252 = Encoding.GetEncoding(1252);
                sb.AppendLine("✓ Windows-1252 encoding is available");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"✗ FAILED: Cannot get Windows-1252 encoding: {ex.Message}");
                allTestsPassed = false;
                message = sb.ToString();
                return false;
            }

            // Test 2: Verify Norwegian characters encode correctly
            sb.AppendLine("\nTest 2: Norwegian character encoding");
            string testString = "æøå ÆØÅ";
            byte[] encodedBytes = encoding1252.GetBytes(testString);
            string decodedString = encoding1252.GetString(encodedBytes);

            if (decodedString == testString)
            {
                sb.AppendLine($"✓ Norwegian characters encode/decode correctly: '{testString}'");
            }
            else
            {
                sb.AppendLine($"✗ FAILED: String mismatch. Original: '{testString}', Decoded: '{decodedString}'");
                allTestsPassed = false;
            }

            // Test 3: Verify byte values for Norwegian characters
            sb.AppendLine("\nTest 3: Verify specific byte values (Windows-1252)");
            var charTests = new Dictionary<char, byte>
            {
                { 'æ', 0xE6 },
                { 'ø', 0xF8 },
                { 'å', 0xE5 },
                { 'Æ', 0xC6 },
                { 'Ø', 0xD8 },
                { 'Å', 0xC5 }
            };

            foreach (var (character, expectedByte) in charTests)
            {
                byte[] bytes = encoding1252.GetBytes(character.ToString());
                if (bytes.Length == 1 && bytes[0] == expectedByte)
                {
                    sb.AppendLine($"✓ '{character}' = 0x{bytes[0]:X2} (expected 0x{expectedByte:X2})");
                }
                else
                {
                    sb.AppendLine($"✗ FAILED: '{character}' = 0x{bytes[0]:X2} (expected 0x{expectedByte:X2})");
                    allTestsPassed = false;
                }
            }

            // Test 4: StringContent with Windows-1252
            sb.AppendLine("\nTest 4: StringContent with Windows-1252 encoding");
            try
            {
                string csvContent = "Etternavn,Fornavn,Klubb,Starttid\nHansen,Ole,IL Heming                             ,17:00\n";
                using var content = new StringContent(csvContent, encoding1252, "text/csv");
                var contentType = content.Headers.ContentType?.ToString();
                
                if (contentType != null && contentType.Contains("windows-1252"))
                {
                    sb.AppendLine($"✓ StringContent created successfully with Content-Type: {contentType}");
                }
                else
                {
                    sb.AppendLine($"⚠ WARNING: Content-Type may not include charset: {contentType}");
                    // This is not necessarily a failure - HttpClient may add it later
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"✗ FAILED: Cannot create StringContent with Windows-1252: {ex.Message}");
                allTestsPassed = false;
            }

            // Test 5: Compare UTF-8 vs Windows-1252 byte size
            sb.AppendLine("\nTest 5: UTF-8 vs Windows-1252 byte size comparison");
            string norwegianText = "Orientering med æøå i Oslo";
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(norwegianText);
            byte[] win1252Bytes = encoding1252.GetBytes(norwegianText);
            
            sb.AppendLine($"  Text: '{norwegianText}'");
            sb.AppendLine($"  UTF-8 size: {utf8Bytes.Length} bytes");
            sb.AppendLine($"  Windows-1252 size: {win1252Bytes.Length} bytes");
            
            if (win1252Bytes.Length < utf8Bytes.Length)
            {
                sb.AppendLine($"✓ Windows-1252 is more compact ({utf8Bytes.Length - win1252Bytes.Length} bytes saved)");
            }
            else
            {
                sb.AppendLine($"⚠ WARNING: Expected Windows-1252 to be smaller than UTF-8");
            }

            // Summary
            sb.AppendLine("\n" + new string('=', 50));
            if (allTestsPassed)
            {
                sb.AppendLine("✓ ALL TESTS PASSED - Windows-1252 encoding is working correctly!");
            }
            else
            {
                sb.AppendLine("✗ SOME TESTS FAILED - Check errors above");
            }
            sb.AppendLine(new string('=', 50));
        }
        catch (Exception ex)
        {
            sb.AppendLine($"\n✗ UNEXPECTED ERROR: {ex.Message}");
            sb.AppendLine(ex.StackTrace ?? "");
            allTestsPassed = false;
        }

        message = sb.ToString();
        return allTestsPassed;
    }
}
