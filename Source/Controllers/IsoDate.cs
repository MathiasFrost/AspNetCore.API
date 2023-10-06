namespace AspNetCore.API.Controllers;

public readonly struct IsoDate : ISpanParsable<IsoDate>
{
    public static IsoDate Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse(string? s, IFormatProvider? provider, out IsoDate result)
    {
        result = new IsoDate(s);
        return true;
    }

    public static IsoDate Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(s.ToString());

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out IsoDate result)
    {
        result = new IsoDate(s.ToString());
        return true;
    }

    private IsoDate(string value) => this.value = value;

    private readonly string value = String.Empty;

    public TimeSpan Duration { get; } = TimeSpan.Zero;
    public DateTime Start { get; } = DateTime.MinValue;
    public DateTime End { get; } = DateTime.MinValue;

    public override string ToString() => value;
}