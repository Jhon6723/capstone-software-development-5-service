using PixPro.Services.Projects.Domain.ValueObjects;

namespace PixPro.Services.Projects.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid OwnerId { get; private set; }

    public Project(string name, Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be an empty GUID.", nameof(ownerId));

        var projectName = new ProjectName(name);

        Id = Guid.NewGuid();
        Name = projectName.Value;
        CreatedAt = DateTimeOffset.UtcNow;
        OwnerId = ownerId;
    }

    private Project()
    {
        Name = string.Empty;
    }
}
