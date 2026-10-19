using DCSRelay.Models;
using Xunit;
using System.Collections.Generic;

namespace DCSRelay.Tests.Models
{
    public class PortNameComparerTests
    {
        private readonly PortNameComparer _comparer = PortNameComparer.Instance;

        [Theory]
        [InlineData("COM1", "COM1", 0)]
        [InlineData("COM1", "COM2", -1)]
        [InlineData("COM2", "COM1", 1)]
        [InlineData("COM1", "COM10", -1)]
        [InlineData("COM10", "COM1", 1)]
        [InlineData("COM2", "COM10", -1)]
        [InlineData("COM10", "COM2", 1)]
        [InlineData("COM10", "COM11", -1)]
        [InlineData("COM11", "COM10", 1)]
        public void Compare_NumericPorts_SortsCorrectly(string x, string y, int expected)
        {
            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Theory]
        [InlineData("com1", "COM1", 0)]
        [InlineData("com1", "COM2", -1)]
        [InlineData("COM2", "com1", 1)]
        public void Compare_CaseInsensitivity_SortsCorrectly(string x, string y, int expected)
        {
            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Theory]
        [InlineData(null, null, 0)]
        [InlineData(null, "COM1", -1)]
        [InlineData("COM1", null, 1)]
        public void Compare_NullValues_HandlesCorrectly(string? x, string? y, int expected)
        {
            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Theory]
        [InlineData("COM01", "COM1", 1)] // Leading zeros logic: tie-breaker by length
        [InlineData("COM01", "COM02", -1)]
        [InlineData("COM02", "COM01", 1)]
        public void Compare_LeadingZeros_HandlesCorrectly(string x, string y, int expected)
        {
            // Note: based on the implementation:
            // FindSignificantDigitStart removes leading zeros.
            // Then it compares length of significant digits, then the digits themselves.
            // Then it compares original run length.

            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Theory]
        [InlineData("COM1A", "COM1B", -1)]
        [InlineData("COM1B", "COM1A", 1)]
        [InlineData("A1", "A2", -1)]
        [InlineData("A2", "A1", 1)]
        public void Compare_MixedAlphaNumeric_SortsCorrectly(string x, string y, int expected)
        {
            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Fact]
        public void Compare_SameReference_ReturnsZero()
        {
            string s = "COM1";
            Assert.Equal(0, _comparer.Compare(s, s));
        }

        [Theory]
        [InlineData("COM1", "COM100", -1)]
        [InlineData("COM100", "COM1", 1)]
        [InlineData("COM20", "COM100", -1)]
        [InlineData("COM100", "COM20", 1)]
        public void Compare_LargeNumbers_SortsCorrectly(string x, string y, int expected)
        {
            int result = _comparer.Compare(x, y);
            int expectedSign = System.Math.Sign(expected);
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }
    }
}
