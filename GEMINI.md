# Nightowl Guidelines

## Mandatory APK Release on Git Push
Whenever pushing commits to GitHub (`origin/main`), always:
1. Build and sign the Release Android APK (`dotnet publish src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-android -c Release`).
2. Copy the signed APK to root `nightowl.apk`.
3. Push to `origin main`.
4. Create an official GitHub Release with `gh release create <next-version> nightowl.apk ...`.
5. Copy the APK to the conversation artifact directory so the user can download and install it on their device immediately.
