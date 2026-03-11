using System.Collections.Generic;

namespace CommunityToolkit.Maui.SourceGenerators.Models;

public record EnumDescriptionModel(
    string EnumName,
    string Namespace,
    string QualifiedName,
    IReadOnlyList<EnumMemberModel> Members
);

public record EnumMemberModel(
    string Name,
    string? Description,
    string? DisplayName,
    string? ResourceType
);
