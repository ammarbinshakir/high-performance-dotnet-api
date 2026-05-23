using System.Globalization;
using System.Text;

namespace HighPerformanceDotNetApi.Application.Products;

public static class CursorCodec
{
    public static string Encode(long id)
    {
        var bytes = Encoding.UTF8.GetBytes(id.ToString(CultureInfo.InvariantCulture));
        return Convert.ToBase64String(bytes);
    }

    public static long? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
