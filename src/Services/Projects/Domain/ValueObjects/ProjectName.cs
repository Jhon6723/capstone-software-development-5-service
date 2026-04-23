namespace PixPro.Services.Projects.Domain.ValueObjects;

public sealed class ProjectName : IEquatable<ProjectName>
{
    public const int MaxLength = 100;

    public string Value { get; }

    public ProjectName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Project name cannot be null or empty", nameof(raw));

        string trimmed = raw.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException(
                $"Project name cannot exceed {MaxLength} characters got {trimmed.Length}.",
                nameof(raw));

        Value = trimmed;
    }

    public override string ToString() => Value;
    public bool Equals(ProjectName? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is ProjectName other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
