namespace CovidAnalyticsPortal.Infrastructure.ExternalServices.Moh;

/// <summary>
/// Minimal, allocation-conscious reader for the well-formed CSV datasets
/// published by the Ministry of Health. The MoH files use simple comma
/// separation without embedded commas or quoted fields, so a full CSV library
/// is unnecessary; this keeps the dependency surface small while remaining
/// robust to blank lines and trailing whitespace.
/// </summary>
internal static class CsvReader
{
    /// <summary>
    /// Parses CSV content into a header-keyed sequence of rows.
    /// </summary>
    /// <param name="content">The raw CSV text.</param>
    /// <returns>
    /// One dictionary per data row, mapping (case-insensitive) column names to
    /// their cell values.
    /// </returns>
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> Parse(string content)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return rows;
        }

        using var reader = new StringReader(content);

        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return rows;
        }

        var headers = headerLine.Split(',');

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = line.Split(',');
            var row = new Dictionary<string, string>(
                headers.Length,
                StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Length && i < cells.Length; i++)
            {
                row[headers[i].Trim()] = cells[i].Trim();
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Reads a column as a non-negative <see cref="long"/>, returning zero when
    /// the value is missing, blank, or non-numeric. MoH figures are reported as
    /// whole numbers (sometimes with a decimal suffix), so the integer part is
    /// taken.
    /// </summary>
    /// <param name="row">The parsed CSV row.</param>
    /// <param name="column">The column name to read.</param>
    /// <returns>The parsed, clamped value.</returns>
    public static long GetLong(IReadOnlyDictionary<string, string> row, string column)
    {
        if (!row.TryGetValue(column, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return 0;
        }

        if (long.TryParse(raw, out var value))
        {
            return value < 0 ? 0 : value;
        }

        if (double.TryParse(raw, out var dbl))
        {
            var truncated = (long)dbl;
            return truncated < 0 ? 0 : truncated;
        }

        return 0;
    }

    /// <summary>
    /// Reads a column as a string, returning <c>null</c> when absent or blank.
    /// </summary>
    /// <param name="row">The parsed CSV row.</param>
    /// <param name="column">The column name to read.</param>
    /// <returns>The cell value, or <c>null</c>.</returns>
    public static string? GetString(IReadOnlyDictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
