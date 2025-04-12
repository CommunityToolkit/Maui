# Contributing

Thank you for your interest in contributing to the .NET MAUI Community Toolkit! In this document we'll outline what you need to know about contributing and how to get started.

First and foremost: we're all friends here. Whether you are a first-time contributor or a core team member from one of the associated projects, we welcome any and all people to contribute to our lovely little project. I mean, it is called *community* toolkit after all.

Having that said, if you are a first-timer and you could use some help please reach out to any core member. They will be happy to help you out or find someone who can.

Furthermore, for anyone, we would like you to take into consideration the following guidelines.

## Kindness

### Make an effort to be nice

If you disagree, that's fine. We don't think about everything the same way, be respectful and at some point decide to agree to disagree. If a decision needs to be made, try to involve at least one other person without continuing an endless discussion.

When you disagree with a piece of code that is written, try to be helpful and explain why you disagree or how things can be improved (according to you). Always remember there are numerous ways to solve things, there is not one right way, but it's always good to learn about alternatives.

During a code review try to make a habit out of it to say at least one nice thing. Obviously about something you like in the code. If a change is not that big or so straight-forward that you can't comment nicely on that, find something else to compliment the person. Make an effort to look at their profile of blog and mention something you like, make that persons day a bit better! <3

### Make an effort to see it from their perspective

Remember English is not everyone's native language. Written communication always lacks non-verbal communication. With written communication in a language that is not your native tongue it is even harder to express certain emotions.

Always assume that people mean to do right. Try to read a sentence a couple of times over and take things more literal. Try to place yourself in their shoes and see the message beyond the actual words.

Things might come across different than they were intended, please keep that in mind and always check to see how someone meant it. If you're not sure, pull someone offline in a private channel on Twitter or email and chat about it for a bit. Maybe even jump on a call to collaborate. We're living in the 21st century, all the tools are there, why not use them to get to know each other and be friends?!

Besides language, we understand that contributing to open-source mostly happens in your spare time. Remember that priorities might change and we can only spend our time once. This works as a two-way street: don't expect things to be solved instantly, but also please let us know if you do not have the capacity to finish work you have in progress. There is no shame in that. That way it's clear to other people that they can step in and take over.

### THANK YOU

Lastly, a big thank you for spending your precious time on our project. We appreciate any effort you make to help us with this project.

## Code of Conduct

Please see our [Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

As should be clear by now: we assume everyone tries to do their best, everyone should be treated with respect and equally.

In the unfortunate event that doesn't happen, please feel free to report it to any of the team members or reach out to [Gerald](mailto:gerald.versluis@microsoft.com) directly.

We will take appropriate actions and measures if necessary.

## Prerequisites

1. Install latest stable [.NET SDK](https://dotnet.microsoft.com/en-us/download)
1. Install .NET MAUI workloads (we recommend using Visual Studio installer)

> You will need to complete a Contribution License Agreement before any pull request can be accepted. Complete the CLA at <https://cla.dotnetfoundation.org/>. This will also be triggered whenever you open a PR and the link should guide you through it.

## Reporting a bug

If you found something that looks like a bug don't hesitate in opening an issue reporting it. We strongly recommend you to follow our template, if you don't follow your issue can be closed, and that's because we don't have a lot of resources, so we will focus on issues that have most information that we need to work with. And, we would say, the most important part is the reproduction sample that shows the bug.

### Bug reproduction

We always request a reproduction sample, and that's not to make your life hard or anything like that... The reason of having a reproduction is to save us time to identify and fix the bug. Create a new project; download the MCT NuGet; write UI; ViewModel; create a service; run it. As you can see it's a lot of timing consuming for us that we could spend fixing the actual issue. So **PLEASE** create a small reproduction project, upload it on GitHub or GitLab and paste the link in the issue.

> We don't accept `.zip` files as reproduction samples, for security reasons. So if you send us a `.zip` file we will kindly ask you to upload it to GitHub or GitLab and share the link

And most important: **Please, help us to help you ❤️**

## Opening a PR process

### TL;DR

* Find an issue/feature, make sure that the issue/feature has been `Approved` and is welcomed (also see [New Features](https://github.com/CommunityToolkit/Maui#submitting-a-new-feature))
* Fork repository
* Create branch
* Implement
* Open a PR
* We merge
* High-fives all-around

### Please consider

#### Tabs vs. Spaces?

[Tabs](https://www.reddit.com/r/javascript/comments/c8drjo/nobody_talks_about_the_real_reason_to_use_tabs/).

#### Make your changes small, don't keep adding

We love your enthusiasm, but small changes and small PRs are easier to digest. We're all doing this in our spare time, it is easier to review a couple of small things and merge that and iterate from there than to have a PR with 100+ files changed that will sit there forever.

#### Added features should have tests, a sample and documentation

We like quality as much as the next person, so please provide tests.

In addition, we would want a new feature or change to be as clear as possible for other developers. Please add a sample to the sample app as part of your PR and also provide a PR to our [documentation repository](https://github.com/MicrosoftDocs/CommunityToolkit).

## Where to make your changes?

If you are unsure on where to locate the changes you need to make then please use the following section and flowchart.

![New Code Workflow](https://user-images.githubusercontent.com/13558917/145694198-7addbd35-0e5f-4816-b351-759a01ec2672.png)

### CommunityToolkit.Maui.Core

In general, this project will have all the basement to develop our Toolkit, including some primitive types, interfaces and base classes, base views, and common code. This will be referenced by other Frameworks/Toolkit based on .NET MAUI that wants to have the same features that us.

We ask that all classes in `CommunityToolkit.Maui.Core` are `public`. This allows developers to extend `CommunityToolkit.Maui.Core`. The `[EditorBrowsable(EditorBrowsableState.Never)]` attribute can be added to classes that we don't recommend developers discovering.

Here is an example of classes that will 

* BaseViews, could be Views that will be used by other Views, like PaddingButton (that's used by Snackbar) or the MauiPopup (used by Popup) that will be a native control implemented in a way that can work with our handler. This same approach is used here

* Primitives, which will be base types that can be used by everyone, like our MathOperator. So other frameworks may not have the concept of Behavior or Converter but they can mimic them as helper classes/methods and use our primitives.

* Common Code, this will be all generic code (platform-specific or not) that can be used by other Frameworks/Toolkits

* Layout Managers, were introduced on .NET MAUI and they live on Microsoft.Maui.Core so makes sense to have our managers on Core as well.

* Handlers, on Core will be the most general Handler with the majority of features.

### CommunityToolkit.Maui

This project has a reference to the Core project. Here will live the implementation of our Controls, Views, Behaviors, Animations, etc. In other words, this project will work with the .NET MAUI and will be MVVM friendly. Also, other Toolkits/Frameworks can reference this package if needed.

Here we will have some:

* View Implementation, with BindableProperties, support to attach effects, behaviors, triggers, and all that jazz.

* Platform Configuration, that is Platform-specific features, that can relate to some control - like the ArrowDirection that is part of Popup and works just on iOS - or the application itself - like the StatusBarColorEffect from XCT.

* Handlers Implementation, We will add to our PropertyMapper and/or CommandMapper any Platform Configuration that some Handler/View may have. We also can implement here some features that we think will not be great to have on Core. Here is a reference for this

* Layout, will be the implementation of ours custom layouts and will use the Layout Managers on Core

## Contributing Code - Best Practices

### Debug Logging

* Always use `Trace.WriteLine()` instead of `Debug.WriteLine` for debug logging because `Debug.WriteLine` is removed by the compiler in Release builds

### Methods Returning Task and ValueTask

* Always include a `CancellationToken` as a parameter to every method returning `Task` or `ValueTask`
* If the method is public, provide the default value for the `CancellationToken` (e.g. `CancellationToken token = default`)
  * If the method is not public, do not provide a default value for the `CancellationToken`
* Use `CancellationToken.ThrowIfCancellationRequested()` to verify the `CancellationToken`

### Enums

* Always use `Unknown` at index 0 for return types that may have a value that is not known
* Always use `Default` at index 0 for option types that can use the system default option
* Follow naming guidelines for tense... `SensorSpeed` not `SensorSpeeds`
* Assign values (0,1,2,3) for all enums

### Property Names

* Include units only if one of the platforms includes it in their implementation. For instance HeadingMagneticNorth implies degrees on all platforms, but PressureInHectopascals is needed since platforms don't provide a consistent API for this.

### Units

* Use the standard units and most well accepted units when possible. For instance Hectopascals are used on UWP/Android and iOS uses Kilopascals so we have chosen Hectopascals.

### Pattern matching

#### Null checking

* Prefer using `is` when checking for null instead of `==`.

e.g.

```csharp
// null
if (something is null)
{

}

// or not null
if (something is not null)
{
   
}
```

* Avoid using the `!` [null forgiving operator](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-forgiving) to avoid the unintended introduction of bugs.

#### Type checking

* Prefer `is` when checking for types instead of casting.

e.g.

```csharp
if (something is Bucket bucket)
{
   bucket.Empty();
}
```

### File Scoped Namespaces

* Use [file scoped namespaces](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/file-scoped-namespaces) to help reduce code verbosity.

e.g.

```csharp
namespace CommunityToolkit.Maui.Converters;

using System;

class BoolToObjectConverter
{
}
```

### Braces

Please use `{ }` after `if`, `for`, `foreach`, `do`, `while`, etc.

e.g.

```csharp
if (something is not null)
{
   ActOnIt();
}
```

### `NotImplementedException`

Please avoid adding new code that throws a `NotImplementedException`. According to the [Microsoft Docs](https://docs.microsoft.com/dotnet/api/system.notimplementedexception), we should only "throw a `NotImplementedException` exception in properties or methods in your own types when that member is still in development and will only later be implemented in production code. In other words, a NotImplementedException exception should be synonymous with 'still in development.'"

In other words, `NotImplementedException` implies that a feature is still in development, indicating that the Pull Request is incomplete.

### Bug Fixes

If you're looking for something to fix, please browse [open issues](https://github.com/CommunityToolkit/Maui/issues).

Follow the style used by the [.NET Foundation](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md), with two primary exceptions:

* We do not use the `private` keyword as it is the default accessibility level in C#.
* We will **not** use `_` or `s_` as a prefix for internal or private field names
* We will use `camelCaseFieldName` for naming internal or private fields in both instance and static implementations

Read and follow our [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md)

### Proposals

To propose a change or new feature, review the guidance on [Submitting a New Feature](https://github.com/CommunityToolkit/Maui#submitting-a-new-feature).

#### Non-Starter Topics

The following topics should generally not be proposed for discussion as they are non-starters:

* Large renames of APIs
* Large non-backward-compatible breaking changes
* Platform-Specifics which can be accomplished without changing the .NET MAUI Community Toolkit
* Avoid clutter posts like "+1" which do not serve to further the conversation, please use the emoji responses for that
