using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Exceptions;

namespace CovidAnalyticsPortal.Domain.ValueObjects;

/// <summary>
/// Represents a Malaysian state or federal territory as an immutable value
/// object, validated against the ISO 3166-2:MY subdivision set. Encapsulating
/// the code prevents invalid or free-text location values from entering the
/// domain and provides a friendly display name for presentation.
/// </summary>
public sealed class StateCode : ValueObject
{
    /// <summary>
    /// The canonical set of valid subdivision codes mapped to their display
    /// names. Keys are compared case-insensitively.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KnownStates =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["JHR"] = "Johor",
            ["KDH"] = "Kedah",
            ["KTN"] = "Kelantan",
            ["MLK"] = "Melaka",
            ["NSN"] = "Negeri Sembilan",
            ["PHG"] = "Pahang",
            ["PNG"] = "Pulau Pinang",
            ["PRK"] = "Perak",
            ["PLS"] = "Perlis",
            ["SGR"] = "Selangor",
            ["TRG"] = "Terengganu",
            ["SBH"] = "Sabah",
            ["SWK"] = "Sarawak",
            ["KUL"] = "W.P. Kuala Lumpur",
            ["LBN"] = "W.P. Labuan",
            ["PJY"] = "W.P. Putrajaya",
        };

    /// <summary>
    /// Gets the canonical upper-case subdivision code (e.g. <c>SGR</c>).
    /// </summary>
    public string Code { get; }

    private StateCode(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Gets the human-readable display name of the state (e.g. <c>Selangor</c>).
    /// </summary>
    public string Name => KnownStates[Code];

    /// <summary>
    /// Creates a validated <see cref="StateCode"/> from a subdivision code.
    /// </summary>
    /// <param name="code">The ISO 3166-2:MY subdivision code.</param>
    /// <returns>A valid <see cref="StateCode"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when the code is null, empty, or unknown.</exception>
    public static StateCode Create(string code)
    {
        DomainException.ThrowIf(
            string.IsNullOrWhiteSpace(code),
            "State code must be provided.");

        var normalized = code.Trim().ToUpperInvariant();

        DomainException.ThrowIf(
            !KnownStates.ContainsKey(normalized),
            $"'{code}' is not a recognised Malaysian state or federal territory code.");

        return new StateCode(normalized);
    }

    /// <summary>
    /// Creates a validated <see cref="StateCode"/> from a display name
    /// (e.g. <c>Selangor</c>), as typically supplied by the upstream MoH feed.
    /// </summary>
    /// <param name="name">The display name of the state.</param>
    /// <returns>A valid <see cref="StateCode"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when the name is null, empty, or unknown.</exception>
    public static StateCode FromName(string name)
    {
        DomainException.ThrowIf(
            string.IsNullOrWhiteSpace(name),
            "State name must be provided.");

        var match = KnownStates
            .FirstOrDefault(pair => string.Equals(pair.Value, name.Trim(), StringComparison.OrdinalIgnoreCase));

        DomainException.ThrowIf(
            match.Key is null,
            $"'{name}' is not a recognised Malaysian state or federal territory name.");

        return new StateCode(match.Key!);
    }

    /// <summary>
    /// Attempts to create a <see cref="StateCode"/> from a code or display
    /// name without throwing on failure.
    /// </summary>
    /// <param name="value">The code or display name to parse.</param>
    /// <param name="stateCode">When this method returns, contains the parsed value if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out StateCode? stateCode)
    {
        stateCode = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        if (KnownStates.ContainsKey(trimmed))
        {
            stateCode = new StateCode(trimmed.ToUpperInvariant());
            return true;
        }

        var match = KnownStates
            .FirstOrDefault(pair => string.Equals(pair.Value, trimmed, StringComparison.OrdinalIgnoreCase));

        if (match.Key is not null)
        {
            stateCode = new StateCode(match.Key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the full set of valid states and federal territories.
    /// </summary>
    public static IReadOnlyCollection<StateCode> All =>
        KnownStates.Keys.Select(code => new StateCode(code)).ToArray();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
