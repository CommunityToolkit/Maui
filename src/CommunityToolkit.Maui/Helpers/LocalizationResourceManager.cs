#nullable enable
using CommunityToolkit.Maui.ObjectModel;
using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace CommunityToolkit.Maui.Helpers
{
    public class LocalizationResourceManager : ObservableObject
	{
		static readonly Lazy<LocalizationResourceManager> currentHolder = new(() => new LocalizationResourceManager());

		public static LocalizationResourceManager Current => currentHolder.Value;

		ResourceManager? resourceManager;
		CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;

		LocalizationResourceManager()
		{
		}

		public void Init(ResourceManager resource) => resourceManager = resource;

		public void Init(ResourceManager resource, CultureInfo initialCulture)
		{
			CurrentCulture = initialCulture;
			Init(resource);
		}

		public string GetValue(string text)
		{
			if (resourceManager == null)
				throw new InvalidOperationException($"Must call {nameof(LocalizationResourceManager)}.{nameof(Init)} first");

			return resourceManager.GetString(text, CurrentCulture) ?? throw new NullReferenceException($"{nameof(text)}: {text} not found");
		}

		public string this[string text] => GetValue(text);

		public CultureInfo CurrentCulture
		{
			get => currentCulture;
			set => SetProperty(ref currentCulture, value, null);
		}
	}
}
