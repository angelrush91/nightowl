# Nightowl Guidelines

## Automated CI/CD Android APK Releases
The repository uses GitHub Actions (`.github/workflows/build-apk.yml`) to automatically build, sign, and publish the Android APK on pushes and release tags.

1. **Push to GitHub**: Commit and push changes to `origin/main`.
2. **Releases**: Create semantic version tags/releases via `gh release create <tag> --title "<Title>" --notes "<Notes>"` or git tags. GitHub Actions automatically compiles and attaches the signed APK.
3. **No local APK build on push**: Do not run local `dotnet publish` for Android on routine pushes unless explicitly requested by the user.
