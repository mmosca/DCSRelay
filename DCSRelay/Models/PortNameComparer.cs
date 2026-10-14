namespace DCSRelay.Models
{
    public sealed class PortNameComparer : IComparer<string>
    {
        public static PortNameComparer Instance { get; } = new PortNameComparer();

        private PortNameComparer()
        {
        }

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var xIndex = 0;
            var yIndex = 0;
            while (xIndex < x.Length && yIndex < y.Length)
            {
                if (char.IsAsciiDigit(x[xIndex]) && char.IsAsciiDigit(y[yIndex]))
                {
                    var xEnd = FindDigitRunEnd(x, xIndex);
                    var yEnd = FindDigitRunEnd(y, yIndex);
                    var xSignificantStart = FindSignificantDigitStart(x, xIndex, xEnd);
                    var ySignificantStart = FindSignificantDigitStart(y, yIndex, yEnd);
                    var lengthComparison = (xEnd - xSignificantStart).CompareTo(yEnd - ySignificantStart);
                    if (lengthComparison != 0)
                    {
                        return lengthComparison;
                    }

                    for (var offset = 0; offset < xEnd - xSignificantStart; offset++)
                    {
                        var digitComparison = x[xSignificantStart + offset].CompareTo(y[ySignificantStart + offset]);
                        if (digitComparison != 0)
                        {
                            return digitComparison;
                        }
                    }

                    var runLengthComparison = (xEnd - xIndex).CompareTo(yEnd - yIndex);
                    if (runLengthComparison != 0)
                    {
                        return runLengthComparison;
                    }

                    xIndex = xEnd;
                    yIndex = yEnd;
                    continue;
                }

                var characterComparison = char.ToUpperInvariant(x[xIndex]).CompareTo(char.ToUpperInvariant(y[yIndex]));
                if (characterComparison != 0)
                {
                    return characterComparison;
                }

                xIndex++;
                yIndex++;
            }

            return (x.Length - xIndex).CompareTo(y.Length - yIndex);
        }

        private static int FindDigitRunEnd(string value, int start)
        {
            var end = start;
            while (end < value.Length && char.IsAsciiDigit(value[end]))
            {
                end++;
            }

            return end;
        }

        private static int FindSignificantDigitStart(string value, int start, int end)
        {
            while (start < end - 1 && value[start] == '0')
            {
                start++;
            }

            return start;
        }
    }
}
