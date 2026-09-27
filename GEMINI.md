# Nightowl Guidelines

## Automated CI/CD
- **Continuous Integration (`.github/workflows/ci.yml`)**: Automatically runs automated unit tests on pushes to `main` and pull requests.
- **Android Release Pipeline (`.github/workflows/build-apk.yml`)**: Automatically builds, signs, and attaches the Android Release APK to GitHub Releases on version tags (`v*`).

1. **Push to GitHub**: Commit and push changes to `origin/main`.
2. **Releases**: Create semantic version tags/releases via `gh release create <tag> --title "<Title>" --notes "<Notes>"` or git tags. GitHub Actions automatically compiles and attaches the signed APK.
3. **No local APK build on push**: Do not run local `dotnet publish` for Android on routine pushes unless explicitly requested by the user.
