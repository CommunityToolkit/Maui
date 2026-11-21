# Fixture sources (macos-runner-image)

Built by `impl/U5/build_fixtures.py` from the saved runner-images READMEs (research/runners/fixtures), fetched 2026-11-21 at runner-images main 0e63d4f9d2c29043b4cf1d2f40d1812a7cde1369. The sha256 is of the original file. The listing schema is DESIGN 5.2; the README has no `LicenseInfo.plist`, so license types are derived.

| Fixture | Source | sha256 of the original | What was kept |
|---|---|---|---|
| `xcode-27-20261104.json` | research/runners/fixtures/xcode-27-arm64-Readme.md | `96e11336d7b1776475cefd2bbf274d9e12c9efdffeaf37e2aeac2c49413e2033` | Image version 20261104.0337.1 (the build the rollout state calls deployed). Xcode table, simulators and .NET SDKs; license types derived: "(beta)" -> Beta, else Release. |
| `macos-26.json` | research/runners/fixtures/macos-26-arm64-Readme.md | `38e0e086edd9d5479967b659c8b72901731f8b23e2b89adfe6e4f145a275b56b` | Image version 20261117.0516.1; Xcode table, simulators, .NET SDKs. |
| `macos-15.json` | research/runners/fixtures/macos-15-arm64-Readme.md | `864f9744fbd18964cf89d2a7401aafd2f8fb3955749d101d98b9cac258a37009` | Image version 20261118.0550.1; Xcode table, simulators, .NET SDKs. |
| `xcode-27-20261119.json` | research/runners/fixtures/xcode-27-arm64-Readme.md and release-xcode-27-20261119.md L50-57 | `96e11336d7b1776475cefd2bbf274d9e12c9efdffeaf37e2aeac2c49413e2033` (README), `457520e10c14990b3ca2de657b273c2aedea07609b66759f8fe88c82b7f99c00` (release notes) | The xcode-27 listing plus what release 20261119.0378.1 added: the `Xcode_27.3_beta.app` real folder (license type Beta) with its symlinks `Xcode_27.3.app` and `Xcode_27.3.0.app`, an iOS 27.3 runtime and .NET SDK 11.0.100. The rollout state says this build is still deploying. |
| `imagedata.json` | format of research/runners/review-fetch/imageos/configure-preimagedata.sh L19-41 | `0de1da45dacbb507f52185c0c22d6be61ec84249bcd40a51e156494f3cbfcbac` | Written by hand in the script's real format with the xcode-27 image name and version. |
| `simctl-runtimes.json` | the real `xcrun simctl list runtimes -j` format | n/a (written by hand) | The iOS runtimes of the xcode-27 README, with the keys simctl prints. |
| `list-sdks.txt` | the real `dotnet --list-sdks` format | n/a (written by hand) | The .NET SDK versions of the macos-26 README. |
