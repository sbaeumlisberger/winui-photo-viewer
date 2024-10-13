using Essentials.NET;
using System.Text.Encodings.Web;
using System.Text.Json;
using Windows.Storage;

namespace PhotoViewer.Core.Services
{
    public interface ISuggestionsService
    {
        List<string> GetAll(string? query = null);

        List<string> GetRecent(ICollection<string>? exclude = null, int max = 12);

        List<string> FindMatches(string query, ICollection<string>? exclude = null, int max = 12);

        Task AddSuggestionAsync(string suggestion);

        Task ImportFromFileAsync(IStorageFile file);

        Task ExportToFileAsync(IStorageFile file);

        Task ResetAsync();

        Task RemoveSuggestionAsync(string suggestion);
    }

    internal record class SuggestionsJsonObject(List<string> Values);

    internal class SuggestionsService : ISuggestionsService
    {
        private const int MaxRecentCount = 20;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            TypeInfoResolver = PhotoViewerCoreJsonSerializerContext.Default,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string filePathSuggestions;

        private readonly string filePathRecent;

        private HashSet<string> suggestions = new HashSet<string>();

        private List<string> recent = new List<string>();

        public SuggestionsService(string id)
        {
            filePathSuggestions = Path.Combine(AppData.PublicFolder, id + ".json");
            filePathRecent = Path.Combine(AppData.PublicFolder, id + "-recent.json");
            Task.Run(LoadSuggestionsAsync);
        }

        public List<string> GetAll(string? query)
        {
            if(!string.IsNullOrEmpty(query))
            {
                return suggestions
                    .Where(suggestion => suggestion.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                    .OrderBy(suggestion => suggestion)
                    .ToList();
            }
            return suggestions.OrderBy(suggestion => suggestion).ToList();
        }

        public List<string> GetRecent(ICollection<string>? exclude, int max)
        {
            exclude ??= [];

            return recent.Where(suggestion => !exclude.Contains(suggestion)).Take(max).ToList();
        }

        public List<string> FindMatches(string query, ICollection<string>? exclude, int max)
        {
            exclude ??= [];

            if (string.IsNullOrWhiteSpace(query))
            {
                return GetRecent(exclude, max);
            }

            return suggestions
                .Where(suggestion => suggestion.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .Where(suggestion => !exclude.Contains(suggestion))
                .OrderByDescending(suggestion => suggestion.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                .ThenBy(suggestion => suggestion)
                .Take(max)
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
                recent.Remove(suggestion);
                recent.Insert(0, suggestion);

                if (recent.Count > MaxRecentCount)
                {
                    recent.RemoveLast();
                }

                await PersistRecentAsync();
            }
        }

        public async Task ImportFromFileAsync(IStorageFile file)
        {
            using var stream = File.OpenRead(file.Path);
            var suggestionsJsonObject = await JsonSerializer.DeserializeAsync<SuggestionsJsonObject>(stream, JsonOptions);
            suggestions = suggestionsJsonObject!.Values.ToHashSet();
            await PersistSuggestionsAsync();
        }

        public async Task ExportToFileAsync(IStorageFile file)
        {
            var suggestionsJsonObject = new SuggestionsJsonObject(suggestions.ToList());
            using var stream = File.Open(file.Path, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, suggestionsJsonObject, JsonOptions);
        }

        private async Task LoadSuggestionsAsync()
        {
            if (File.Exists(filePathSuggestions))
            {
                using var stream = File.OpenRead(filePathSuggestions);
                var suggestionsJsonObject = await JsonSerializer.DeserializeAsync<SuggestionsJsonObject>(stream, JsonOptions);
                suggestions = suggestionsJsonObject!.Values.ToHashSet();
            }
            if (File.Exists(filePathRecent))
            {
                using var stream = File.OpenRead(filePathRecent);
                var recentJsonObject = await JsonSerializer.DeserializeAsync<SuggestionsJsonObject>(stream, JsonOptions);
                recent = recentJsonObject!.Values;
            }
        }

        private async Task PersistSuggestionsAsync()
        {
            var suggestionsJsonObject = new SuggestionsJsonObject(suggestions.ToList());
            using var stream = File.Open(filePathSuggestions, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, suggestionsJsonObject, JsonOptions);
        }

        private async Task PersistRecentAsync()
        {
            var recentJsonObject = new SuggestionsJsonObject(recent.ToList());
            using var stream = File.Open(filePathRecent, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, recentJsonObject, JsonOptions);
        }

        public async Task ResetAsync()
        {
            suggestions = new HashSet<string>();
            await PersistSuggestionsAsync();
            recent = new List<string>();
            await PersistRecentAsync();
        }

        public async Task RemoveSuggestionAsync(string suggestion)
        {
            if (suggestions.Remove(suggestion))
            {
                await PersistSuggestionsAsync();
            }
            if (recent.Remove(suggestion))
            {
                await PersistRecentAsync();
            }
        }
    }
}
