<img src="https://user-images.githubusercontent.com/13558917/137551073-ac8958bf-83e3-4ae3-8623-4db6dce49d02.png" alt="..NET Bot" width=125>  [<img src="https://raw.githubusercontent.com/dotnet-foundation/swag/master/logo/dotnetfoundation_v4.svg" alt=".NET Foundation" width=100>](https://dotnetfoundation.org) 

[![Build](https://github.com/CommunityToolkit/Maui/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/CommunityToolkit/Maui/actions/workflows/dotnet-build.yml) [![NuGet Downloads](https://img.shields.io/nuget/dt/CommunityToolkit.Maui)](https://www.nuget.org/packages/CommunityToolkit.Maui)

# .NET MAUI Community Toolkit

The .NET MAUI Community Toolkit is a collection of common elements for development with .NET MAUI that developers tend to replicate across multiple apps. It simplifies and demonstrates common developer tasks when building apps with .NET MAUI. 

All features are contributed by you, our amazing .NET community, and maintained by a core set of maintainers.

And – the best part – the features you add to the .NET MAUI Toolkit may one day be included into the official .NET MAUI library! We leverage the Community Toolkits to debut new features and work closely with the .NET MAUI engineering team to nominate features for promotion.

## Documentation

<a href="https://learn.microsoft.com/dotnet/communitytoolkit/maui/get-started?tabs=CommunityToolkitMaui"><img width="200" alt="image" src="https://user-images.githubusercontent.com/13558917/232885041-35b62d65-26d3-44a7-a525-5239ac811498.png"></a>

All of the [documentation](https://learn.microsoft.com/dotnet/communitytoolkit/maui/get-started?tabs=CommunityToolkitMaui) for `CommunityToolkit.Maui` can be found here on [Microsoft Learn](https://learn.microsoft.com/dotnet/communitytoolkit/maui/get-started?tabs=CommunityToolkitMaui):

https://learn.microsoft.com/dotnet/communitytoolkit/maui/get-started

## Installation

The release versions of .NET MAUI Community Toolkit are available at Nuget.org as NuGet packages. You can install them using the following commands in your terminal or Package Manager Console:

```bash
dotnet add package CommunityToolkit.Maui
dotnet add package CommunityToolkit.Maui.MediaElement
dotnet add package CommunityToolkit.Maui.Maps
dotnet add package CommunityToolkit.Maui.Camera
```

You can also try the latest pre-release version of the main branch. We use GitHub Packages to host the pre-release versions of the .NET MAUI Community Toolkit. To install the pre-release version, you need to add the GitHub Packages source to your NuGet configuration. GitHub Packages only supports authentication using a personal access token (classic) (https://github.com/settings/tokens). For more information, see [Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token). You can use a personal access token (classic) to authenticate to GitHub Packages. When you create a personal access token (classic), you can assign the token different scopes depending on your needs, but `read:packages` is enough.

```bash
dotnet nuget add source https://nuget.pkg.github.com/CommunityToolkit/index.json -n CommunityToolkitGitHubNuget -u YOUR_GITHUB_USERNAME -p YOUR_GITHUB_PERSONAL_ACCESS_TOKEN
```

and then install the packages as follows:

```bash
dotnet add package CommunityToolkit.Maui --version 99.0.0-preview980
dotnet add package CommunityToolkit.Maui.MediaElement --version 99.0.0-preview980
dotnet add package CommunityToolkit.Maui.Maps --version 99.0.0-preview980
dotnet add package CommunityToolkit.Maui.Camera --version 99.0.0-preview980
```

## Getting Started

In order to use the .NET MAUI Community Toolkit you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using CommunityToolkit.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			// Initialize the .NET MAUI Community Toolkit by adding the below line of code
			.UseMauiCommunityToolkit()
			// After initializing the .NET MAUI Community Toolkit, optionally add additional fonts
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Continue initializing your .NET MAUI App here

		return builder.Build();
	}
}
```

### XAML usage

In order to make use of the toolkit within XAML you can use this namespace:

```xml
xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
```

## Roadmap / Plan

All work carried out on the toolkit is done so by the community and the core maintainers in our spare time on nights and weekends. Due to this reason we do not have a concrete plan on when features will be added and delivered. What we do have is a list of proposals and a [Project board](https://github.com/CommunityToolkit/Maui/projects/1) which summarises they states. Please feel free to check them out and jump in to providing any assistance that you feel you can.

## Submitting A New Feature

New features will follow the below workflow, described in more detail in the steps below

[![New Feature Workflow](https://user-images.githubusercontent.com/13558917/160910778-1e61f478-f1f6-48b4-8d37-8016eae1bd12.png)](./build/workflow.sketch)

### 1. Discussion Started

Debate pertaining to new Maui Toolkit features takes place in the form of [Discussions](https://github.com/communitytoolkit/maui/discussions) in this repo.

If you want to suggest a feature, discuss current design notes or proposals, etc., please [open a new Discussion topic](https://github.com/communitytoolkit/maui/discussions/new).

Discussions that are short and stay on topic are much more likely to be read. If you leave comment number fifty, chances are that only a few people will read it. To make discussions easier to navigate and benefit from, please observe a few rules of thumb:

- Discussion should be relevant to the .NET MAUI Toolkit. If they are not, they will be summarily closed.
- Choose a descriptive topic that clearly communicates the scope of discussion.
- Stick to the topic of the discussion. If a comment is tangential, or goes into detail on a subtopic, start a new discussion and link back.
- Is your comment useful for others to read, or can it be adequately expressed with an emoji reaction to an existing comment?

### 2. Proposal Submitted
Once you have a fully fleshed out proposal describing a new feature in syntactic and semantic detail, please [open an issue for it](https://github.com/communitytoolkit/maui/issues/new/choose), and it will be labelled as a [Proposal](https://github.com/communitytoolkit/maui/issues?q=is%3Aopen+is%3Aissue+label%3Aproposal). The comment thread on the issue can be used to hash out or briefly discuss details of the proposal, as well as pros and cons of adopting it into the .NET MAUI Toolkit. If an issue does not meet the bar of being a full proposal, we may move it to a discussion, so that it can be further matured. Specific open issues or more expansive discussion with a proposal will often warrant opening a side discussion rather than cluttering the comment section on the issue.

### 3. Proposal Championed
When a member of the .NET MAUI Toolkit core team finds that a proposal merits promotion into the Toolkit, they can [Champion](https://github.com/communitytoolkit/maui/issues?q=is%3Aopen+is%3Aissue+label%3A%22proposal+champion%22) it, which means that they will bring it to the monthly [.NET MAUI Toolkit Community Standup](https://www.youtube.com/watch?v=0ZBh2Hl54ZY). 

### 4. Proposal Approved
The .NET MAUI Toolkit core team will collectively vote to work on adopting and/or modifying the proposal, requiring a majority approval (i.e. greater than 50%) to be added to the Toolkit.

Once a Proposal has been championed and has received a majority approval from the .NET MAUI Toolkit core team, a Pull Request can be opened.

### 5. Pull Request Approved
After a Pull Request has been submitted, it will be reviewed and approved by the Proposal Champion. 

Every new feature also requires an associated sample to be added to the .NET MAUI Toolkit Sample app.

### 6. Documentation Complete 
Before a Pull Request can be merged into the .NET MAUI Toolkit, the Pull Request Author must also submit the documentation to [documentation repository](https://github.com/MicrosoftDocs/CommunityToolkit).

### 7. Completed
Once a Pull Request has been reviewed + approved AND the documentation has been written, submitted and approved, the new feature will be merged adding it to the .NET MAUI Toolkit

## Code of Conduct
As a part of the .NET Foundation, we have adopted the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). Please familiarize yourself with that before participating with this repository. Thanks!

## .NET Foundation
This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
