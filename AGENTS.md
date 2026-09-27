# Nightowl Agent Rules & Guidelines

## CI/CD Pipeline Builds Android APKs
The GitHub Actions pipeline (`.github/workflows/build-apk.yml`) automatically builds, signs, and attaches the Android Release APK to GitHub Releases on pushes to `main` and version tags (`v*`).

- **No local APK build on routine pushes**: Do NOT run local Android `dotnet publish` when pushing code changes. The GitHub Actions CI/CD pipeline builds and releases the signed APK automatically.
- **Publishing Releases**: When releasing a new version, commit and push your changes to `origin/main` and create the release tag (e.g. `gh release create <tag> --title "<tag> - <Title>" --notes "<Release notes>"`). GitHub Actions will compile the signed APK and attach it to the release asset list automatically.
- **Local APK Builds**: Only build the APK locally (`dotnet publish src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-android -c Release`) if the user explicitly asks for a local APK build or direct local file delivery.
