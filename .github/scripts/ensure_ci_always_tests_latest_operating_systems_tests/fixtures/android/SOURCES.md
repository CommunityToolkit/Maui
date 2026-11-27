# Fixture sources (android)

Fetched 2026-11-21. The sha256 is of the original file, before trimming.

| Fixture | Source | sha256 of the original | What was kept |
|---|---|---|---|
| `google_apis-sys-img2-4.xml` | https://dl.google.com/android/repository/sys-img/google_apis/sys-img2-4.xml (copy in research/android/fixtures/google_apis-sys-img2-4-2026-11-21.xml) | `0435f4ccc89958cda478fa7b193883bc6ead06b7fa0c32f141d434a563a68f4d` | every x86_64 package (both tags, incl. ext, canary, beta) + both android-24;google_apis;arm64-v8a duplicates: 35 of 94 packages; one license cut to one line |
| `google_apis-sys-img2-3.xml` | https://dl.google.com/android/repository/sys-img/google_apis/sys-img2-3.xml (copy in research/android/fixtures/google_apis-sys-img2-3-2026-11-21.xml) | `f46a7c36aba029eba520189f731901a95c8f423bc446d16c326bf351efd8855e` | every x86_64 package (both tags, incl. ext, canary, beta) + both android-24;google_apis;arm64-v8a duplicates: 35 of 94 packages; one license cut to one line |
| `google_apis-sys-img2-3-2025-01-03.xml` | web.archive.org capture 20250103171835 of https://dl.google.com/android/repository/sys-img/google_apis/sys-img2-3.xml (copy in research/android/review/dl4-wayback/google_apis-sys-img2-3-wayback-20250103171835.xml) | `f4ab32ea0fca58a049b26297c01814fb6e3b766e230b01b9583fcbbeb77b5cc9` | every x86_64 package, a state before Android 17 with Baklava previews: 17 of 63 packages; one license cut to one line |
| `repository2-3.xml` | https://dl.google.com/android/repository/repository2-3.xml (copy in research/android/fixtures/repository2-3-2026-11-21.xml) | `b97da160df8d80f0fe67739643cb53fbfcafd9c502ef57b78c038645062865ee` | every platforms;android-* package: 51 of 291 packages; only the android-sdk-license kept, cut to one line |
