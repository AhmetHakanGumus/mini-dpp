namespace MiniDpp.Api.Validation;

public static class Gtin
{
    public const int Length = 14;

    public static bool IsFourteenDigits(string value) =>
        value.Length == Length && value.All(char.IsAsciiDigit);

    /// <summary>
    /// GS1 mod-10: kontrol basamağı hariç, sağdan sola 3 ve 1 ağırlıklarıyla toplanır;
    /// kontrol basamağı toplamı 10'un katına tamamlar.
    /// </summary>
    public static int ComputeCheckDigit(string value)
    {
        var sum = 0;
        var weight = 3;
        for (var i = value.Length - 2; i >= 0; i--)
        {
            sum += (value[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (10 - sum % 10) % 10;
    }
}
