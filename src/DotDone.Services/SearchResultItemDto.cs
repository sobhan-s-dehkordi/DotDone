using System;
using DotDone.Core.Enums;

namespace DotDone.Services;

public enum SearchResultType
{
    Repository,
    Project,
    HeadTask,
    SubTask,
    Attachment
}

public class SearchResultItemDto
{
    public int Id { get; set; }
    public SearchResultType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MatchedSnippet { get; set; } = string.Empty;
    public string LocationBreadcrumb { get; set; } = string.Empty;
    public int RepositoryId { get; set; }
    public int? ProjectId { get; set; }
    public int? HeadTaskId { get; set; }
    public ItemStatus? Status { get; set; }

    public string TypeGlyph => Type switch
    {
        SearchResultType.Repository => "\uE8B7",
        SearchResultType.Project => "\uE82D",
        SearchResultType.HeadTask => "\uE73E",
        SearchResultType.SubTask => "\uE8FD",
        SearchResultType.Attachment => "\uE8A5",
        _ => "\uE71E"
    };
}
