using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerCore.Services
{
    internal interface ISuggestionsService
    {
        IReadOnlyList<string> GetRecentSuggestions();

        IReadOnlyList<string> FindMatches(string query, int count = 10);

        Task AddSuggestionAsync(string suggestion);

        Task ImportFromFileAsync(IStorageFile file);

        Task ExportToFileAsync(IStorageFile file);
    }

    internal class SuggestionsService : ISuggestionsService
    {
        private record class JsonObject(List<string> Values);

        private const int MaxRecentCount = 10;

        private static readonly JsonSerializerOptions ExportJsonOptions = new () 
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string filePathSuggestions;

        private readonly string filePathRecent;

        private HashSet<string> suggestions = new HashSet<string>();

        private List<string> recent = new List<string>();

        public SuggestionsService(string id)
        {
            string localFolderPath = ApplicationData.Current.LocalFolder.Path;
            filePathSuggestions = Path.Combine(localFolderPath, id + ".json");
            filePathRecent = Path.Combine(localFolderPath, id + "-recent.json");
            _ = LoadSuggestionsAsync();
        }

        public IReadOnlyList<string> GetRecentSuggestions()
        {
            return recent;
        }

        public IReadOnlyList<string> FindMatches(string query, int count)
        {
            return suggestions
                .Where(suggestion => suggestion.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(suggestion => suggestion.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                .ThenBy(suggestion => suggestion)
                .Take(count)
                .ToList();
        }

        public async Task AddSuggestionAsync(string suggestion)
        {
            if (suggestions.Add(suggestion))
            {
                await PersistSuggestionsAsync();
            }

            if (recent.FirstOrDefault() != suggestion)
            {
                if (recent.Count == MaxRecentCount)
                {
                    recent.RemoveAt(recent.Count - 1);
                }
                recent.Insert(0, suggestion);

                await PersistRecentAsync();
            }
        }

        public async Task ImportFromFileAsync(IStorageFile file)
        {
            using var stream = File.OpenRead(file.Path);
            var suggestionsJsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(stream);
            suggestions = suggestionsJsonObject!.Values.ToHashSet();
        }

        public async Task ExportToFileAsync(IStorageFile file)
        {
            var suggestionsJsonObject = new JsonObject(suggestions.ToList());
            using var stream = File.Open(file.Path, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, suggestionsJsonObject, ExportJsonOptions);
        }

        private async Task LoadSuggestionsAsync()
        {
            if (File.Exists(filePathSuggestions))
            {
                using var stream = File.OpenRead(filePathSuggestions);
                var suggestionsJsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(stream);
                suggestions = suggestionsJsonObject!.Values.ToHashSet();
            }
            if (File.Exists(filePathRecent))
            {
                using var stream = File.OpenRead(filePathRecent);
                var recentJsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(stream);
                recent = recentJsonObject!.Values;
            }
        }

        private async Task PersistSuggestionsAsync()
        {
            var suggestionsJsonObject = new JsonObject(suggestions.ToList());
            using var stream = File.Open(filePathSuggestions, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, suggestionsJsonObject);
        }

        private async Task PersistRecentAsync()
        {
            var recentJsonObject = new JsonObject(recent.ToList());
            using var stream = File.Open(filePathRecent, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, recentJsonObject);
        }
    }
}
