using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;

namespace DevHabit.Api.DTOs.Tags;

public sealed record TagsCollectionDto : ICollectionResponse<TagDto>
{
    public List<TagDto> Items { get; init; }
}
