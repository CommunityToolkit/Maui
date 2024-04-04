namespace CommunityToolkit.Maui.UnitTests.Mocks;

using System.Collections.Generic;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;

public class FontElementHandlerStub : ViewHandler<IView, object>
{
	public static readonly IPropertyMapper<IView, FontElementHandlerStub> Mapper =
		new PropertyMapper<IView, FontElementHandlerStub>
		{
			[nameof(IFontElement.FontAttributes)] = (h, v) => h.MapProperty(nameof(IFontElement.FontAttributes)),
			[nameof(IFontElement.FontAutoScalingEnabled)] = (h, v) => h.MapProperty(nameof(IFontElement.FontAutoScalingEnabled)),
			[nameof(IFontElement.FontFamily)] = (h, v) => h.MapProperty(nameof(IFontElement.FontFamily)),
			[nameof(IFontElement.FontSize)] = (h, v) => h.MapProperty(nameof(IFontElement.FontSize)),
			[nameof(ITextStyle.Font)] = (h, v) => h.MapProperty(nameof(ITextStyle.Font)),
		};

	public FontElementHandlerStub()
		: base(Mapper)
	{
	}

	public List<string> Updates { get; set; } = [];

	protected override object CreatePlatformView() => new object();

	void MapProperty(string propertyName) => Updates.Add(propertyName);
}