using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal sealed class JsonDraftStore : IDraftStore
{
    private readonly IApplicationDirectoriesService _directoriesService;

    public JsonDraftStore(IApplicationDirectoriesService directoriesService)
    {
        _directoriesService = directoriesService;
    }

    public async Task SaveAsync(Draft draft)
    {
        string path = GetPath(draft.Id);

        string json = JsonSerializer.Serialize(draft);

        await File.WriteAllTextAsync(path, json);
    }

    public async Task<Draft> GetAsync(Guid id)
    {
        string path = GetPath(id);

        if (!File.Exists(path))
            return null;

        string json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<Draft>(json);
    }

    public Task DeleteAsync(Guid id)
    {
        string path = GetPath(id);

        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string GetPath(Guid id)
        => Path.Combine(_directoriesService.DraftsDirectory, $"{id}.json");

    public async Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync()
    {
        if (!Directory.Exists(_directoriesService.DraftsDirectory))
            return [];

        var files = Directory.GetFiles(_directoriesService.DraftsDirectory, "*.json");

        var drafts = new List<OrderDraftSummary>();

        foreach (string file in files)
        {
            try
            {
                string json = await File.ReadAllTextAsync(file);

                Draft draft =
                    JsonSerializer.Deserialize<Draft>(json);

                if (draft is null)
                    continue;

                Guid id = Guid.Parse(
                    Path.GetFileNameWithoutExtension(file));

                drafts.Add(new OrderDraftSummary(
                    id,
                    draft.Data.ClientName,
                    draft.Data.Items.Count,
                    draft.LastModified));
            }
            catch (JsonException)
            {
                // Ignore invalid draft files.
            }
        }

        return drafts
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();
    }
}
