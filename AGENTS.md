# Nightowl Agent Rules & Guidelines

## CI/CD Pipeline
- **Continuous Integration (`.github/workflows/ci.yml`)**: Automatically runs automated unit tests on pushes to `main` and pull requests.
- **Android Release Pipeline (`.github/workflows/build-apk.yml`)**: Automatically builds, signs, and attaches the Android Release APK to GitHub Releases on version tags (`v*`).
- **No local APK build on routine pushes**: Do NOT run local Android `dotnet publish` when pushing code changes.
- **Publishing Releases**: When releasing a new version, commit and push your changes to `origin/main` and create the release tag (e.g. `gh release create <tag> --title "<tag> - <Title>" --notes "<Release notes>"`). The Android Release workflow will compile the signed APK and attach it to the release asset list automatically.
- **Local APK Builds**: Only build the APK locally (`dotnet publish src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-android -c Release`) if the user explicitly asks for a local APK build or direct local file delivery.
