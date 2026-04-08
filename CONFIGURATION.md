# Configuration Guide

## Setting Up Secrets

This application requires sensitive credentials that should **NEVER** be committed to git.

### First-Time Setup

1. **Copy the example configuration:**
   ```powershell
   Copy-Item o-bergen.LiveResultManager\appsettings.example.json o-bergen.LiveResultManager\appsettings.json
   ```

2. **Edit `appsettings.json` with your actual credentials:**
   - Replace `YOUR_SUPABASE_API_KEY_HERE` with your actual Supabase API key
   - Replace `https://your-project.supabase.co` with your actual Supabase URL
   - Update database paths as needed

3. **IMPORTANT:** The `appsettings.json` file is now in `.gitignore` and will NOT be committed to git.

### Configuration Structure

```json
{
  "Supabase": {
    "Url": "https://YOUR-PROJECT.supabase.co",
    "ApiKey": "YOUR_ACTUAL_API_KEY",
    "TableName": "live_results"
  }
}
```

### Where to Find Credentials

**Supabase:**
1. Go to https://supabase.com
2. Open your project
3. Go to Settings > API
4. Copy the URL and API key (use the `service_role` key for server-side operations)

### Security Best Practices

✅ **DO:**
- Keep your `appsettings.json` file local only
- Use `appsettings.example.json` to show structure without secrets
- Rotate API keys if they are ever exposed
- Use environment variables in production

❌ **DON'T:**
- Commit `appsettings.json` to git
- Share your API keys in chat, email, or screenshots
- Use production keys in development environments
- Push files containing secrets to public repositories

### Alternative: Environment Variables

You can also configure secrets using environment variables:

```powershell
$env:Supabase__Url = "https://YOUR-PROJECT.supabase.co"
$env:Supabase__ApiKey = "YOUR_ACTUAL_API_KEY"
```

The application will read from environment variables if they are set, overriding `appsettings.json`.

### Alternative: User Secrets (Development)

For development, you can use .NET User Secrets:

```powershell
cd o-bergen.LiveResultManager
dotnet user-secrets init
dotnet user-secrets set "Supabase:ApiKey" "YOUR_ACTUAL_API_KEY"
dotnet user-secrets set "Supabase:Url" "https://YOUR-PROJECT.supabase.co"
```

This stores secrets in a separate location outside the project directory.
