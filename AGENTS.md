# Agent instructions

This is the .NET MAUI Community Toolkit: four NuGet packages (`CommunityToolkit.Maui`, `.Core`, `.Camera`, `.MediaElement`) in `src/`, a sample app in `samples/`, unit tests (`src/CommunityToolkit.Maui.UnitTests`) and device tests (`src/CommunityToolkit.Maui.DeviceTests`).

## Build and test

* Solution: `src/CommunityToolkit.Maui.slnx`. Pinned SDK: `global.json`.
* Build a library: `dotnet build src/CommunityToolkit.Maui/CommunityToolkit.Maui.csproj -c Release`
* Unit tests: `dotnet test --project src/CommunityToolkit.Maui.UnitTests -c Release`. For code that touches strings, numbers or cultures also run `-- --culture tr-TR`.
* Format: `pwsh eng/format.ps1`
* Device tests: see `eng/devices/README.md`.

## Trimming and NativeAOT are required

Every change to the four packages must keep them fully trim-safe and NativeAOT-safe. The trim, AOT and single-file analyzers run in every library build, every `IL2xxx`, `IL3xxx` and `IL4xxx` warning is an error, and a guard fails the build with `CTMTRIM001-003` if an analyzer is ever off. Do not use reflection on unannotated types or string-path bindings, and do not add `[RequiresUnreferencedCode]`, `[RequiresDynamicCode]` or trim or AOT (`IL`) warning suppressions. Read [.github/instructions/trimming-aot.instructions.md](.github/instructions/trimming-aot.instructions.md) before changing code.

## Instructions

* [.github/copilot-instructions.md](.github/copilot-instructions.md): general rules for contributors and agents
* [.github/instructions/trimming-aot.instructions.md](.github/instructions/trimming-aot.instructions.md): trimming and NativeAOT
* [.github/instructions/codestyle.instructions.md](.github/instructions/codestyle.instructions.md): C# style
* [.github/instructions/async.instructions.md](.github/instructions/async.instructions.md): asynchronous code
* [.github/instructions/culture.instructions.md](.github/instructions/culture.instructions.md): culture-safe code and Turkish tests
* [.github/instructions/device-tests.instructions.md](.github/instructions/device-tests.instructions.md): device tests
* [.github/instructions/mobile-memory-leaks.instructions.md](.github/instructions/mobile-memory-leaks.instructions.md): memory leaks on mobile
* [CONTRIBUTING.md](CONTRIBUTING.md): contribution process
