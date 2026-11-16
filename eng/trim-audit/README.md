# Trim and NativeAOT audit

Checks that the four NuGet packages stay trim-safe and NativeAOT-safe in an app. A library build can pass its analyzers and still hand an app warnings, because the trimmer and ILC see the whole app. So this builds the Sample against the packed packages and fails when the toolkit adds a warning that [allow.tsv](allow.tsv) does not list.

## The one command

```bash
pwsh eng/trim-audit/run.ps1
```

On macOS and Linux it runs the gate's own tests first (Windows skips them: the Turkish pass sets POSIX locale variables, and CI runs them on the macOS leg). Then it packs the four libraries from this checkout, builds the Sample for every platform this machine can build, and prints a verdict: PASS or FAIL, the warnings per bucket for each leg, and where the logs are.

| Machine | Default legs |
| --- | --- |
| macOS | iOS and Android, NativeAOT (`aotroot`) |
| Windows | Windows, full trimming (`rootlibs`) |
| Linux | Android, NativeAOT (`aotroot`) |

Needs the .NET SDK and MAUI workload from `global.json`, PowerShell 7 and Python 3 (without Python the run stops before any build with exit 4). Each run packs a new version and deletes the old audit versions and the leg folders of the legs it plans, so a leg that does not run shows `NOT RUN`. A default platform that is not set up (no Xcode, no Android SDK) is skipped, and the plan says so.

## Options

| Option | Meaning |
| --- | --- |
| `-Platform android,ios,maccatalyst,windows` | Only these legs. Mac Catalyst is not a default. |
| `-Mode aot` or `-Mode full` | NativeAOT (`aotroot`) or full trimming with all toolkit code analyzed (`rootlibs`). Windows `aot` only reports (dotnet/maui#39053). |
| `-Variant <v>` | One harness variant for every platform, in place of `-Mode`: `none`, `partial`, `full`, `rootlibs`, `aot`, `aotroot`. |
| `-Feed <folder>` or `-Feed auto` | Use packed nupkgs and skip packing. `auto` finds the ones a CI pack step left. |
| `-Version <v>` | The version for packing. Default: a new one per run, so that a changed library never meets a cached package. |
| `-OutRoot <dir>` | Default `artifacts/trim-audit` (`C:\ta` on Windows). Per leg: `<platform>-<variant>/`. |
| `-CI` | GitHub annotations, the job summary, `AUDIT-EXIT` and `GATE-EXIT` lines, no skipped platforms. |
| `-SkipSelfTest` | Skip the gate's own tests (Windows never runs them). |
| `-DryRun` | Print the plan only. |
| `-Help` | Show the full help. |

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Every leg passed |
| 1 | New toolkit or toolkit-api warnings |
| 2 | Usage error or a bad feed |
| 3 | The build did not use the packages, or the NativeAOT roots check failed |
| 4 | A build failed, its logs are unreadable, or Python 3 is missing |
| 5 | Packing failed |
| 6 | The gate's own tests failed (macOS, Linux) |
| 71 | No platform can be built here |

## What CI runs

The `Build Library` job of `.github/workflows/dotnet-build.yml` calls `pwsh eng/trim-audit/run.ps1 -Feed auto -CI` on both legs, after the packages are packed and uploaded. The job summary and the `GATE-EXIT` line carry the verdict; GitHub may drop annotations when earlier steps used all 50.

## A failing leg

Fix the warning in the library. Adding a line to `allow.tsv` needs a maintainer's approval and a reason that names the issue or upstream blocker. Open `<out-root>/<leg>/warnings-summary.txt` or `warnings-unique.tsv` for the list, and `build.binlog` for the build. On CI, download the `trim-audit-<OS>` artifact of the run (`trim-audit-macOS`, `trim-audit-Windows`; 14 days, without `bin`, `obj` and `publish`).

## Internals (advanced)

`run.ps1` drives these; their headers list every flag. They use a fixed version (`99.0.0-audit`), so a direct rerun after a code change needs a new `--version` or `-Version`, or a deleted `<out-root>/nuget/packages`.

* `audit.sh run <platforms> <variant> (--repo . | --feed <dir>)`: macOS and Linux. Also `audit.sh <platform> <variant>` (one leg, build only), `gate`, `pack` and `env`.
* `audit-win.ps1 -Variant <v> (-Repo . | -Feed <dir>)`: Windows.
* `ilwarn.py`: parses the warnings and judges them; `test_ilwarn.py` tests it.