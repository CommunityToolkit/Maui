using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Helpers;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class VariableMultiValueConverter_Tests
    {
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.None, false)]
        [TestCase(new object[] { true, false, true }, MultiBindingCondition.None, false)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.None, true)]
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.All, true)]
        [TestCase(new object[] { true, false, true }, MultiBindingCondition.All, false)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.All, false)]
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.Any, true)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.Any, false)]
        [TestCase(new object[] { false, true, false }, MultiBindingCondition.Any, true)]
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.Exact, true, 3)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.Exact, false, 1)]
        [TestCase(new object[] { false, true, false }, MultiBindingCondition.Exact, true, 1)]
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.GreaterThan, true, 2)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.GreaterThan, false, 1)]
        [TestCase(new object[] { false, true, false }, MultiBindingCondition.GreaterThan, true, 0)]
        [TestCase(new object[] { true, true, true }, MultiBindingCondition.LessThan, false, 2)]
        [TestCase(new object[] { false, false, false }, MultiBindingCondition.LessThan, true, 1)]
        [TestCase(new object[] { false, true, false }, MultiBindingCondition.LessThan, true, 2)]
        [TestCase(null, MultiBindingCondition.All, false)]
        [TestCase(null, MultiBindingCondition.Any, false)]
        [TestCase(null, MultiBindingCondition.Exact, false)]
        [TestCase(null, MultiBindingCondition.GreaterThan, false)]
        [TestCase(null, MultiBindingCondition.LessThan, false)]
        [TestCase(null, MultiBindingCondition.None, false)]
        public void VariableMultiConverter(object[] value, MultiBindingCondition type, object expectedResult, int count = 0)
        {
            var variableMultiConverter = new VariableMultiValueConverter() { ConditionType = type, Count = count };
            var result = variableMultiConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
            Assert.AreEqual(result, expectedResult);
        }
    }
}