using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Habitat.Models;

namespace Habitat.Services;

public class SupabaseDataService<Table> : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly string url;
    private List<Table> cache = new();
    private readonly string[]? selectedColumns;
    private DateTime lastFetch = DateTime.MinValue;
    private readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(60);
    private Task? currentFetchTask;
    private readonly object fetchLock = new();

    private string BuildUrl (params string[] columns)
    {
        if (columns == null || columns.Length == 0)
            return url;
        var seperator = url.Contains("?") ? "&" : "?";
        var select = string.Join(",", columns);
        return $"{url}{seperator}select={select}";
    }

    public IReadOnlyList<Table> Data => cache;
    public bool IsLoading => currentFetchTask != null && !currentFetchTask.IsCompleted;

    public SupabaseDataService(string baseUrl, string anonKey, string tableName, params string[] columns)
    {
        url = $"{baseUrl}/rest/v1/{tableName}";
        selectedColumns = columns;

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", anonKey);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", anonKey);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    public void EnsureData()
    {
        if (DateTime.UtcNow - lastFetch < cacheDuration)
        {
            //Log.Information($"{Plugin.PluginInterface.Manifest.Name} Supabase VipList cache hit");
            return;
        }
        lock (fetchLock)
        {
            if (currentFetchTask == null || currentFetchTask.IsCompleted)
            {
                currentFetchTask = FetchAsync();
            }
        }
    }

    public void Refresh()
    {
        lock (fetchLock)
        {
            currentFetchTask = FetchAsync();
        }
    }

    private async Task FetchAsync()
    {
        try
        {
            var requestUrl = BuildUrl(selectedColumns ?? Array.Empty<string>());
            var response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Supabase Error loading {requestUrl}: {error}");
                return;
            }
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<Table>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data != null)
            {
                cache = data;
                lastFetch = DateTime.UtcNow;
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Supabase loaded data async for {requestUrl}");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"{Plugin.PluginInterface.Manifest.Name} Supabase Failed to fetch data: {ex}");
        }
    }
}
