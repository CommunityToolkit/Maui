@ECHO OFF
SETLOCAL EnableDelayedExpansion
IF EXIST ".\src\CommunityToolkit.Maui\bin\" (
rmdir .\src\CommunityToolkit.Maui\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui\obj\" (
rmdir .\src\CommunityToolkit.Maui\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Analyzers\bin\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Analyzers\obj\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.Benchmarks\bin\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.Benchmarks\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.Benchmarks\obj\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.Benchmarks\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.CodeFixes\bin\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.CodeFixes\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.CodeFixes\obj\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.CodeFixes\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.UnitTests\bin\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.UnitTests\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Analyzers.UnitTests\obj\" (
rmdir .\src\CommunityToolkit.Maui.Analyzers.UnitTests\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Camera\bin\" (
rmdir .\src\CommunityToolkit.Maui.Camera\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Camera\obj\" (
rmdir .\src\CommunityToolkit.Maui.Camera\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Camera.Analyzers\bin\" (
rmdir .\src\CommunityToolkit.Maui.Camera.Analyzers\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Camera.Analyzers\obj\" (
rmdir .\src\CommunityToolkit.Maui.Camera.Analyzers\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Camera.Analyzers.CodeFixes\bin\" (
rmdir .\src\CommunityToolkit.Maui.Camera.Analyzers.CodeFixes\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Camera.Analyzers.CodeFixes\obj\" (
rmdir .\src\CommunityToolkit.Maui.Camera.Analyzers.CodeFixes\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Core\bin\" (
rmdir .\src\CommunityToolkit.Maui.Core\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Core\obj\" (
rmdir .\src\CommunityToolkit.Maui.Core\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.Maps\bin\" (
rmdir .\src\CommunityToolkit.Maui.Maps\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.Maps\obj\" (
rmdir .\src\CommunityToolkit.Maui.Maps\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.MediaElement\bin\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.MediaElement\obj\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.MediaElement.Analyzers\bin\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement.Analyzers\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.MediaElement.Analyzers\obj\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement.Analyzers\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.MediaElement.Analyzers.CodeFixes\bin\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement.Analyzers.CodeFixes\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.MediaElement.Analyzers.CodeFixes\obj\" (
rmdir .\src\CommunityToolkit.Maui.MediaElement.Analyzers.CodeFixes\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.SourceGenerators\bin\" (
rmdir .\src\CommunityToolkit.Maui.SourceGenerators\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.SourceGenerators\obj\" (
rmdir .\src\CommunityToolkit.Maui.SourceGenerators\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.SourceGenerators.Internal\bin\" (
rmdir .\src\CommunityToolkit.Maui.SourceGenerators.Internal\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.SourceGenerators.Internal\obj\" (
rmdir .\src\CommunityToolkit.Maui.SourceGenerators.Internal\obj\ /s /q
)

IF EXIST ".\src\CommunityToolkit.Maui.UnitTests\bin\" (
rmdir .\src\CommunityToolkit.Maui.UnitTests\bin\ /s /q
)
IF EXIST ".\src\CommunityToolkit.Maui.UnitTests\obj\" (
rmdir .\src\CommunityToolkit.Maui.UnitTests\obj\ /s /q
)


IF EXIST ".\samples\CommunityToolkit.Maui.Sample\bin\" (
rmdir .\samples\CommunityToolkit.Maui.Sample\bin\ /s /q
)
IF EXIST ".\samples\CommunityToolkit.Maui.Sample\obj\" (
rmdir .\samples\CommunityToolkit.Maui.Sample\obj\ /s /q
)