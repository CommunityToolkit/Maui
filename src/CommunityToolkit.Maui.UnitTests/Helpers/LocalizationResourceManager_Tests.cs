#nullable enable
using CommunityToolkit.Maui.Helpers;
using CommunityToolkit.Maui.UnitTests.Mocks;
using NUnit.Framework;
using System.Globalization;
using System.Resources;

namespace CommunityToolkit.Maui.UnitTests.Helpers
{
    [NonParallelizable]
	public class LocalizationResourceManager_Tests
	{
		ResourceManager resourceManager = null!;
		CultureInfo initialCulture = null!;
		LocalizationResourceManager localizationManager = null!;

		[SetUp]
		public void Setup()
		{
			resourceManager = new MockResourceManager();
			initialCulture = CultureInfo.InvariantCulture;
			localizationManager = LocalizationResourceManager.Current;

			localizationManager.Init(resourceManager, initialCulture);
		}

		[Test]
		public void LocalizationResourceManager_GetCulture_Equal_Indexer()
		{
			// Arrange
			var testString = "test";
			var culture2 = new CultureInfo("en");

			// Act
			var responceIndexerCulture1 = localizationManager[testString];
			var responceGetValueCulture1 = localizationManager.GetValue(testString);
			var responceResourceManagerCulture1 = resourceManager.GetString(testString, initialCulture);

			localizationManager.CurrentCulture = culture2;
			var responceIndexerCulture2 = localizationManager[testString];
			var responceGetValueCulture2 = localizationManager.GetValue(testString);
			var responceResourceManagerCulture2 = resourceManager.GetString(testString, culture2);

			// Assert
			Assert.AreEqual(responceResourceManagerCulture1, responceIndexerCulture1);
			Assert.AreEqual(responceResourceManagerCulture1, responceGetValueCulture1);
			Assert.AreEqual(responceResourceManagerCulture2, responceIndexerCulture2);
			Assert.AreEqual(responceResourceManagerCulture2, responceGetValueCulture2);
		}

		[Test]
		public void LocalizationResourceManager_PropertyChanged_Triggered()
		{
			// Arrange
			var culture2 = new CultureInfo("en");
			CultureInfo? changedCulture = null;
			localizationManager.CurrentCulture = culture2;
			localizationManager.PropertyChanged += (s, e) => changedCulture = localizationManager.CurrentCulture;

			// Act, Assert
			localizationManager.Init(resourceManager, initialCulture);
			Assert.AreEqual(initialCulture, changedCulture);

			localizationManager.CurrentCulture = culture2;
			Assert.AreEqual(culture2, changedCulture);
		}
	}
}
