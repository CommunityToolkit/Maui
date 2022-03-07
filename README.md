[![Build Status](https://dev.azure.com/dotnet/CommunityToolkit/_apis/build/status/CommunityToolkit.Maui?branchName=main)](https://dev.azure.com/dotnet/CommunityToolkit/_build/latest?definitionId=169&branchName=main)

[<img src="https://raw.githubusercontent.com/dotnet-foundation/swag/master/logo/dotnetfoundation_v4.svg" alt=".NET Foundation" width=100>](https://dotnetfoundation.org)

# .NET MAUI Community Toolkit

[![NuGet](https://buildstats.info/nuget/CommunityToolkit.Maui?includePreReleases=true)](https://www.nuget.org/packages/CommunityToolkit.Maui/)

The .NET MAUI Community Toolkit is a collection of common elements for development with .NET MAUI that developers tend to replicate across multiple apps. It simplifies and demonstrates common developer tasks when building apps with .NET MAUI. 

All features are contributed by you, our amazing .NET community, and maintained by a core set of maintainers.

And – the best part – the features you add to the .NET MAUI Toolkit may one day be included into the official .NET MAUI library! We leverage the Community Toolkits to debut new features and work closely with the .NET MAUI engineering team to nominate features for promotion.

## Getting Started

In order to use the .NET MAUI Community Toolkit you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using CommunityToolkit.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		
		// Initialise the toolkit
		builder.UseMauiApp<App>().UseMauiCommunityToolkit();

		// the rest of your logic...
	}
}
```

### XAML usage

In order to make use of the toolkit within XAML you can use this namespace:

```xml
xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
```

## Submitting A New Feature

New features will follow the following steps:

### 1. Discussion Started

Debate pertaining to new Maui Toolkit features takes place in the form of [Discussions](https://github.com/communitytoolkit/maui/discussions) in this repo.

If you want to suggest a feature, discuss current design notes or proposals, etc., please [open a new Discussion topic](https://github.com/communitytoolkit/maui/discussions/new).

Discussions that are short and stay on topic are much more likely to be read. If you leave comment number fifty, chances are that only a few people will read it. To make discussions easier to navigate and benefit from, please observe a few rules of thumb:

- Discussion should be relevant to the .NET MAUI Toolkit. If they are not, they will be summarily closed.
- Choose a descriptive topic that clearly communicates the scope of discussion.
- Stick to the topic of the discussion. If a comment is tangential, or goes into detail on a subtopic, start a new discussion and link back.
- Is your comment useful for others to read, or can it be adequately expressed with an emoji reaction to an existing comment?

### 2. Proposal Submitted
Once you have a fully fleshed out proposal describing a new feature in syntactic and semantic detail, please [open an issue for it](https://github.com/communitytoolkit/maui/issues/new/choose), and it will be labeled as a [Proposal](https://github.com/communitytoolkit/maui/issues?q=is%3Aopen+is%3Aissue+label%3Aproposal). The comment thread on the issue can be used to hash out or briefly discuss details of the proposal, as well as pros and cons of adopting it into the .NET MAUI Toolkit. If an issue does not meet the bar of being a full proposal, we may move it to a discussion, so that it can be further matured. Specific open issues or more expansive discussion with a proposal will often warrant opening a side discussion rather than cluttering the comment section on the issue.

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

### 7. Merged
Once a Pull Request has been reviewed + approved AND the documentation has been written, submitted and approved, the new feature will be merged adding it to the .NET MAUI Toolkit

## Code of Conduct
As a part of the .NET Foundation, we have adopted the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). Please familiarize yourself with that before participating with this repository. Thanks!

## .NET Foundation
This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
