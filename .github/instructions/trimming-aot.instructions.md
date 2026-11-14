---
description: 'Trimming and NativeAOT are required: every change to the CommunityToolkit.Maui, .Core, .Camera and .MediaElement packages must stay fully trim-safe and NativeAOT-safe. Rules for code, MSBuild and generators, and how to check'
applyTo: '**/*.cs,**/*.csproj,**/*.props,**/*.targets,**/*.xml,**/*.yml,**/.editorconfig,**/*.globalconfig,**/.globalconfig,.github/workflows/*.yml,eng/trim-audit/**'
---

# Trimming and NativeAOT Are Required

The four NuGet packages (`CommunityToolkit.Maui`, `.Core`, `.Camera`, `.MediaElement`) must stay fully trim-safe and NativeAOT-safe, without losing features (issue [#3381](https://github.com/CommunityToolkit/Maui/issues/3381)). This file is the single source of truth for contributors, coding agents and code reviewers (including Copilot).

## The requirement

* Every change to a package keeps it trim-safe and NativeAOT-safe. Apps that publish trimmed or with `PublishAot=true` must get no warning from the toolkit.
* The trim, AOT and single-file analyzers run in every library build (`IsAotCompatible=true`).
* Every `IL2xxx`, `IL3xxx` and `IL4xxx` warning is an error (`WarningsAsErrors` in `Directory.Build.props`, all configurations). Never remove codes from that list.
* A guard target in `Directory.Build.targets` fails the build with `CTMTRIM001` (trim analyzer), `CTMTRIM002` (AOT analyzer) or `CTMTRIM003` (single-file analyzer) when an analyzer is off. Fix the cause. Never disable or weaken the guard.
* Each library `.csproj` sets `<TrimMode>full</TrimMode>`. Why: `Microsoft.Maui.Controls` turns the trim analyzer off in library builds unless `TrimMode` is `full` ([xamarin/xamarin-macios#21351](https://github.com/xamarin/xamarin-macios/pull/21351)). `TrimMode` only changes analysis and feature-switch defaults in a library build; none of it reaches the package, so keep the line.
* Never pass a global `-p:TrimMode=<not full>`: it overrides the project value and breaks every build that compiles the libraries, such as the Sample or the device tests through `ProjectReference` (`CTMTRIM001`). Package consumers are not affected.
* Never add `IL` codes to `NoWarn` or `WarningsNotAsErrors`, and never lower their severity in `.editorconfig` or `.globalconfig`.
* Never set `EnableTrimAnalyzer`, `EnableAotAnalyzer` or `EnableSingleFileAnalyzer` to `false` anywhere.

## Rules for code

Fix a warning in this order, and stop at the first step that works:

1. Remove the reflection (a direct call, a typed binding, a generic).
2. Annotate with `[DynamicallyAccessedMembers]` (DAM) so the trimmer keeps what is needed.
3. Ask a maintainer. Do not go further on your own.

Never:

* Use reflection on a type that is not annotated (`GetType().GetProperty(...)`, `Activator.CreateInstance(Type)`, `Type.GetType(string)`, `MakeGenericType`, `Enum.GetValues(Type)`).
* Use string-path bindings (`new Binding("Path")`, `SetBinding(property, "Path")`). They reflect over the source at run time. Use `BindingBase.Create(static (T s) => s.Path, ...)`, the `SetBinding` lambda overloads, or `TypedBinding`.
* Add `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` to a public API.
* Add `[UnconditionalSuppressMessage]`, `[SuppressMessage]` or `#pragma warning disable` for an `IL` code without maintainer approval and proof that the code is safe (a trimmed and a NativeAOT run).
* Serialize JSON with reflection. Use `System.Text.Json` with a source-generated `JsonSerializerContext`.
* Add a line to `eng/trim-audit/allow.tsv` without maintainer approval. Each line needs a reason that names the issue or the upstream blocker. Delete it when the warning is fixed.
* Add a loose `linker.xml` under `build/`: nothing reads it. Only an embedded `ILLink.Descriptors.xml`, or a `TrimmerRootDescriptor` item from a package `.targets` file, reaches the trimmer.

Do:

* Use `Enum.GetValues<T>()` and `Enum.GetNames<T>()`, not the `Type` overloads.
* Keep the DAM annotations that MAUI APIs need. Example: `BindableProperty.Create(returnType: typeof(T))` needs `T` annotated with `PublicParameterlessConstructor`, so generic parameters that flow into it carry `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]` (`IsInRangeConverter<TValue, TReturnObject>`). Removing such an annotation reintroduces `IL2087` in apps.
* Source generators: generated code must never suppress `IL2xxx`, `IL3xxx` or `IL4xxx` warnings. Any suppression in generated code is a narrow list of codes with a WHY comment in the generator source, limited to diagnostics the user's own declarations cause (no blanket `#pragma warning disable`). Generated code must also be culture-invariant (`FormattableString.Invariant`, `ToLowerInvariant`). Generators run in the developer's culture; see `.github/instructions/culture.instructions.md`.
* Android: use `JavaCast<T>()` for a Java object whose binding type may be trimmed. A plain C# cast fails in a trimmed app.
* Apple (iOS, Mac Catalyst): members exported to Objective-C with `[Export]` are kept by the SDK; no extra annotation is needed.
* Windows: a class that crosses the WinRT ABI is `partial`, so the CsWinRT generator can emit its vtable. That includes a class that gets WinRT interfaces from its base class, such as a `ContentView` subclass; without `partial` the Windows build fails with `CsWinRT1028`.
* Windows: a method that hands a managed collection to WinRT returns the concrete type (for example `List<T>`) from a `return` statement. The CsWinRT generator misses expression bodies and `??` results, and Windows NativeAOT then has no CCW vtable for it (see `MauiSemanticOrderView.windows.cs`).
* Windows built-in COM: under full trimming the trimmer removes `[ComImport]` interface methods that no managed code calls (`IL2050`), but built-in COM calls them by vtable slot, so the app crashes. `OfflineSpeechToTextImplementation.windows.cs` keeps every `[ComImport]` interface of `System.Speech` whole with `[DynamicDependency(DynamicallyAccessedMemberTypes.All, ...)]` on `CreateSpeechRecognitionEngine`, so create every `SpeechRecognitionEngine` through that method. The unit test `OfflineSpeechToTextWindowsTrimmingTests` fails when a `System.Speech` update adds or removes such an interface, and when Windows code in Core creates the engine anywhere else; add or remove the `DynamicDependency` it names. Keep any new built-in COM interop whole the same way: an embedded ILLink descriptor cannot name another assembly (`IL2101`).

### Validation behaviors

* `ValuePropertyName` must name a `BindableProperty` that has a public static field named `<Name>Property` (inherited and custom ones work; an internal or differently named field throws). Any other non-empty name (a CLR property, a dotted path, a typo) throws `ArgumentException` when the behavior attaches or when the name changes while attached. With `Options.ShouldSuppressExceptionsInBehaviors` it writes a `Trace` warning instead and leaves `Value` unbound.
* To validate a plain CLR property or a dotted path, set `ValuePropertyName="{x:Null}"` first, then bind `Value` with a compiled binding:
  `Value="{Binding MyProp, Source={x:Reference myView}, x:DataType=local:MyView}"` (or `BindingBase.Create` in C#). A null or empty `ValuePropertyName` leaves a binding set on `Value` untouched.

## Check locally

Run these before opening a PR that touches package code, MSBuild files or generators.

1. Build the library you changed in Release (the analyzers run on every build):
   `dotnet build src/CommunityToolkit.Maui.Core/CommunityToolkit.Maui.Core.csproj -c Release`. One build covers every target framework your OS builds. macOS and Linux skip the Windows TFM; CI's Windows leg builds it. Repeat for each of the four libraries your change reaches.
2. Pack the libraries and build the Sample against the packages with NativeAOT and with full trimming, then gate on toolkit warnings. The harness is `eng/trim-audit`; the script headers of `eng/trim-audit/audit.sh` and `audit-win.ps1` list every flag:

   ```bash
   eng/trim-audit/audit.sh run ios,android aotroot --repo . --out-root <out-root>        # pack, build, gate: NativeAOT, what CI gates on macOS
   eng/trim-audit/audit.sh run ios,android,maccatalyst rootlibs --repo . --out-root <out-root>   # full trimming, all toolkit code analyzed
   eng/trim-audit/audit.sh run ios aotroot --feed <folder-of-nupkgs> --out-root <out-root>   # the same on packages that are already packed
   eng/trim-audit/audit.sh <android|ios|maccatalyst> <none|partial|full|rootlibs|aot|aotroot> <out-root>   # one leg: build only (needs a feed from `audit.sh pack`)
   eng/trim-audit/audit.sh gate <out-root>/<platform>-<variant>   # exit 0 = no toolkit warning outside allow.tsv
   ```

   `eng/trim-audit/audit.sh run` builds and gates in one command, and packs first when it is given a checkout (`--repo`) instead of a feed (`--feed`). CI runs it on the packages the job just packed. `--github`, `--summary <file>`, `--max-annotations <n>` and `--report-only` shape the output for CI. After a code change, pass a new `--version` (for example `--version 99.0.0-local2`) or delete `<out-root>/nuget/packages`; a reused version fails the assets check (exit 3).

   On Windows use `eng/trim-audit/audit-win.ps1 -Variant <none|full|rootlibs|aot> (-Repo . | -Feed <folder-of-nupkgs>) [-OutRoot <short path>]`. Its `rootlibs` variant is the gated one in CI; `aot` only reports until [dotnet/maui#39053](https://github.com/dotnet/maui/issues/39053) is fixed.

   In CI, the job summary and the `GATE-EXIT` and `AUDIT-EXIT` log lines always carry the verdict. Annotations may not appear: GitHub keeps 50 per job, and earlier steps can use them all.

   `eng/trim-audit/allow.tsv` lines are `code<TAB>member pattern (glob)<TAB>reason[<TAB>scope]`. The optional scope is a comma separated list of globs over the leg name `<platform>-<variant>` (for example `android-*,ios-aot*`); without it the line applies to every leg. A scope glob that matches no leg makes the gate fail with exit 2.
3. CI runs the same check in the `Build Library` job of `.github/workflows/dotnet-build.yml` (the job that packs the NuGets). It builds the Sample against the just-packed packages, with NativeAOT (`aotroot`, iOS and Android) on the macOS leg and full trimming (`rootlibs`) on the Windows leg, and fails on any toolkit warning that `eng/trim-audit/allow.tsv` does not list.

## Platform support

| Platform | Full trimming | NativeAOT |
|---|---|---|
| iOS | Supported | Supported |
| Mac Catalyst | Supported | Supported |
| Android | Supported | Experimental in .NET 11 |
| Windows | Supported | Not available: blocked in MAUI by [dotnet/maui#39053](https://github.com/dotnet/maui/issues/39053) |

Notes: NativeAOT always trims fully. The iOS simulator does not trim by default. Windows apps are not trimmed unless they set `PublishTrimmed=true`.

Windows offline speech-to-text uses `System.Speech`, which needs built-in COM. A trimmed Windows app must set `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>`; the toolkit throws `NotSupportedException` naming it. That setting is enough under full trimming too, because the toolkit keeps the `System.Speech` COM interfaces (see Rules for code). NativeAOT does not support it.

Windows, a .NET MAUI issue rather than a toolkit one: under full trimming, a `CollectionView` on the legacy Windows handler (`UseWindowsCollectionView2Handler=false`) shows no templated items. WinUI binds the properties of `ItemTemplateContext` and `GroupTemplateContext` by reflection, and the trimmer removes them ([dotnet/maui#31227](https://github.com/dotnet/maui/issues/31227)). The device tests use that handler and keep those properties in `src/CommunityToolkit.Maui.DeviceTests/NativeAotReflectionRoots.windows.cs`. Apps use the `DynamicDependency` attributes from the docs page Trimming and Native AOT.

**Agents: if you notice that dotnet/maui#39053 is closed or fixed, tell the maintainers** (open an issue, or mention it in your PR or review). Windows NativeAOT can then be enabled in the toolkit, and the Windows leg of the CI step switched from full trimming to NativeAOT.

## Device tests stay NativeAOT-safe

The device tests run fully trimmed and with NativeAOT, so a test must reach its product assertions there too:

* Reach private toolkit members with `[UnsafeAccessor]`, or with reflection the trimmer can see (`typeof(T).GetField("name", ...)` with literal names). Add `[DynamicDependency]` for a type that only reflection reaches, such as a private nested type. Never reflect over `GetType()` or a type name from a string without annotations.
* Use `AotSafeAssert` (in `CommunityToolkit.Maui.DeviceTests.Shared`) instead of xunit's `Assert.Equal`, `Assert.Empty`, `Assert.All` or `Assert.Collection` over value-type items (tuples, `Nullable<T>`, structs). xunit builds their comparers with `MakeGenericMethod`, which fails under NativeAOT.
* Put Windows-only roots that the tests need in `NativeAotReflectionRoots.windows.cs`.

## Code review guidance (Copilot and humans)

Flag a change that:

* adds reflection, a string-path binding, `[RequiresUnreferencedCode]`, `[RequiresDynamicCode]` or any suppression of an `IL` warning;
* removes a `[DynamicallyAccessedMembers]` annotation that a MAUI API needs;
* disables an analyzer, removes an `IL` code from `WarningsAsErrors`, removes `TrimMode` or `IsAotCompatible`, or weakens the `CTMTRIM` guard;
* adds a line to `eng/trim-audit/allow.tsv`, puts an `IL` code in `NoWarn`, `WarningsNotAsErrors`, `.editorconfig` or `.globalconfig`, or removes or weakens the `Build Library` trim/AOT step or the gate;
* adds generator output that suppresses an `IL` warning, uses a blanket `#pragma warning disable`, or adds a suppression without a WHY comment, or formats with the current culture;
* adds a `Type`-based `Enum` call, or JSON without a source-generated context;
* adds private reflection or an xunit value-type assert to the device tests that NativeAOT cannot run.
