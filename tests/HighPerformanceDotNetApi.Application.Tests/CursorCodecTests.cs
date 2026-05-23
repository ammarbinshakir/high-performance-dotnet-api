using HighPerformanceDotNetApi.Application.Products;

namespace HighPerformanceDotNetApi.Application.Tests;

public sealed class CursorCodecTests
{
    [Fact]
    public void EncodeDecode_RoundTripsProductId()
    {
        var cursor = CursorCodec.Encode(42);

        var decoded = CursorCodec.Decode(cursor);

        Assert.Equal(42, decoded);
    }

    [Fact]
    public void Decode_ReturnsNullForInvalidCursor()
    {
        Assert.Null(CursorCodec.Decode("not-valid-base64"));
    }
}
