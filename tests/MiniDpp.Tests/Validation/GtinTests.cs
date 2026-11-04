using MiniDpp.Api.Validation;

namespace MiniDpp.Tests.Validation;

public class GtinTests
{
    [Theory]
    [InlineData("08690000000012", 2)]
    [InlineData("08690000000029", 9)]
    [InlineData("08690000000043", 3)]
    [InlineData("08690000000050", 0)]
    [InlineData("00012345678905", 5)]
    public void ComputeCheckDigit_returns_gs1_digit(string gtin, int expected) =>
        Assert.Equal(expected, Gtin.ComputeCheckDigit(gtin));

    [Theory]
    [InlineData("8690000000012")] // Excel'in baştaki sıfırı silmiş hali
    [InlineData("086900000000120")]
    [InlineData("0869000000001A")]
    [InlineData("٠٨٦٩٠٠٠٠٠٠٠٠١٢")] // ASCII olmayan rakamlar
    public void IsFourteenDigits_rejects_other_shapes(string value) =>
        Assert.False(Gtin.IsFourteenDigits(value));
}
