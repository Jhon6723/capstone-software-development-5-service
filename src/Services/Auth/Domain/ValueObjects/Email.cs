using System.Net.Mail;

namespace PixPro.Services.Auth.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    public Email(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Email cannot be null or empty", nameof(raw));

        string normalized = raw.Trim().ToLowerInvariant();

        if (!IsValidFormat(normalized))
            throw new ArgumentException($"'{raw}' is not a valid email address", nameof(raw));

        Value = normalized;
    }

    private static bool IsValidFormat(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public override string ToString() => Value;
    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
