using System.Globalization;

namespace SB.Scripts.Currency
{
    public static class CurrencyTextFormatter
    {
        private static readonly string[] Suffixes = { "K", "M", "B", "T", "Qa", "Qi" };

        public static string Format(long amount)
        {
            if (amount < 1000)
                return amount.ToString("N0", CultureInfo.InvariantCulture);

            double scaledAmount = amount;
            int suffixIndex = -1;

            while (scaledAmount >= 1000d && suffixIndex < Suffixes.Length - 1)
            {
                scaledAmount /= 1000d;
                suffixIndex++;
            }

            return scaledAmount.ToString("0.##", CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
        }

        public static string FormatName(CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    return "골드";
                case CurrencyType.Diamond:
                    return "다이아";
                case CurrencyType.Emerald:
                    return "에메랄드";
                default:
                    return currencyType.ToString();
            }
        }
    }
}
