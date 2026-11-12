---
description: 'Culture-safe C#: casing, comparison, parsing and formatting that are correct in Turkish (tr-TR, az-Latn-AZ) and the other notorious cultures, and the tests that prove it'
applyTo: '**/*.cs'
---

# Culture-Safe Code and Tests

Any code that reads or formats with `CurrentCulture`/`CurrentUICulture`, or takes a `CultureInfo`, must be tested in Turkish and the other notorious cultures. Turkish and Azerbaijani case-map `i`↔`İ` and `ı`↔`I`, so culture-default casing and comparison corrupt ASCII keywords (`"MonsterId".ToLower()` becomes `monsterıd`, lower-casing `Internal` for generated code gives `ınternal`).

## Machine strings are never culture-dependent
* Keywords, enum names, URLs, URI and file schemes, generated C#, XML/JSON, expression tokens and dictionary keys use `StringComparison.Ordinal`/`OrdinalIgnoreCase`, `ToLowerInvariant()`/`ToUpperInvariant()`, `CultureInfo.InvariantCulture` and `FormattableString.Invariant`.
* Do not use `ToUpper()`, `ToLower()`, `char.ToUpper/ToLower`, `StartsWith(string)`, `EndsWith(string)`, `IndexOf(string)`, `string.Compare` or `RegexOptions.IgnoreCase` on machine strings without an explicit ordinal or invariant choice.
* Only user-visible text uses the passed or current culture.
* Source generators and analyzers run in the developer's culture, so everything they emit must be invariant text.

## The toolkit's converter culture rules
* A converter honors the `CultureInfo` it is passed, not `CurrentCulture`.
* When a direct caller passes `null`, the converter falls back to `CurrentCulture`. The exception is `TextCaseConverter`, where `null` means `InvariantCulture`.
* In a binding, .NET MAUI 11 passes `CurrentUICulture` unless `ConverterCulture` is set.
* `EventToCommandBehavior` converts event arguments with the invariant culture by default; an `EventArgsConverterCulture` opts in to another culture.
* `MathExpressionConverter` keeps number literals in the expression invariant (`3.5`) and honors the culture for string inputs and string results (`"3,5"` in de-DE and tr-TR).

## Tests
* Give every culture-taking API tr-TR cases, plus az-Latn-AZ where casing matters. Add the parameter to the existing test (`[InlineData("tr-TR")]` or a `MemberData` row) instead of copying it.
* Cover both a passed culture and `CurrentCulture`/`CurrentUICulture`. Restore the original culture in `finally`, or rely on the UnitTests `BaseTest`, which restores both cultures after each test.
* Cover i/I/ı/İ, comma decimals, dot thousands, `%50` percent and dates.
* Build culture-formatted expectations from the culture, or write the correct literal for that culture. Never weaken an assertion to make a culture pass.
* Add the other notorious cultures when the code touches them:
  * de-DE: comma decimal, dot thousands
  * sv-SE and nb-NO: U+2212 minus sign instead of `-`
  * ar-SA: Arabic decimal and group separators (U+066B, U+066C), a minus sign prefixed with U+061C, the Um Al-Qura calendar, right-to-left text
  * fr-CH: apostrophe thousands separator
  * th-TH: Thai Buddhist calendar (2026 formats as 2569)

## Running the tests in another culture
* xUnit v3 with `dotnet test`: `dotnet test --culture tr-TR`.
* macOS and Linux: `LANG=tr_TR.UTF-8 LC_ALL=tr_TR.UTF-8 dotnet test`.
* Run the whole suite once under Turkish before pushing a change that touches strings, numbers or cultures.
