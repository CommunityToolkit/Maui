using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class MathExpressionConverter_Tests
    {
        const double tolerance = 0.00001d;
        readonly Type type = typeof(MathExpressionConverter_Tests);
        readonly CultureInfo cultureInfo = CultureInfo.CurrentCulture;

        [TestCase("min(max(4+x, 5), 10)", 2d, 6d)]
        [TestCase("-10 + x * -2", 2d, -14d)]
        [TestCase("x * (-2 * 5)", 2d, -20d)]
        [TestCase("x-10", 19d, 9d)]
        [TestCase("x*-10", 3d, -30d)]
        [TestCase("min(x+6, 8)", 1d, 7d)]
        [TestCase("x + x * x", 2d, 6d)]
        [TestCase("(x + x) * x", 2d, 8d)]
        [TestCase("3 + x * 2 / (1 - 5)^2", 4d, 3.5d)]
        [TestCase("3 + 4 * 2 + cos(100 + x) / (1 - 5)^2 + pow(x0, 2)", 20d, 411.05088631065792d)]
        public void MathExpressionConverter_ReturnsCorrectResult(
            string expression, double x, double expectedResult)
        {
            var mathExpressionConverter = new MathExpressionConverter();

            var result = mathExpressionConverter.Convert(x, type, expression, cultureInfo);

            if (result == null)
            {
                Assert.Fail();
            }
            else
            {
                Assert.IsTrue(Math.Abs((double)result - expectedResult) < tolerance);
            }
        }

        [TestCase("x + x1 * x1", new object[] { 2d, 1d }, 3d)]
        [TestCase("(x1 + x) * x1", new object[] { 2d, 3d }, 15d)]
        [TestCase("3 + x * x1 / (1 - 5)^x1", new object[] { 4d, 2d }, 3.5d)]
        [TestCase("3 + 4 * 2 + cos(100 + x) / (x1 - 5)^2 + pow(x0, 2)", new object[] { 20d, 1d }, 411.05088631065792d)]
        public void MathExpressionConverter_WithMultiplyVariable_ReturnsCorrectResult(
            string expression, object[] variables, double expectedResult)
        {
            var mathExpressionConverter = new MultiMathExpressionConverter();

            var result = mathExpressionConverter.Convert(variables, type, expression, cultureInfo);

            if (result == null)
            {
                Assert.Fail();
            }
            else
            {
                Assert.IsTrue(Math.Abs((double)result - expectedResult) < tolerance);
            }
        }

        [TestCase("1 + 3 + 5 + (3 - 2))")]
        [TestCase("1 + 2) + (9")]
        [TestCase("100 + pow(2)")]
        public void MathExpressionConverterThrowsArgumentException(string expression)
        {
            var mathExpressionConverter = new MathExpressionConverter();

            var result = new TestDelegate(()
                => mathExpressionConverter.Convert(0d, type, expression, cultureInfo));

            Assert.Throws<ArgumentException>(result);
        }
    }
}