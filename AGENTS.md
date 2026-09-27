# Nightowl Agent Rules & Guidelines

## Always Make an APK Release on Push
Whenever committing and pushing code changes to GitHub (`origin/main`), the agent must **always** create and publish a new signed Android APK release:

1. **Build the signed Android Release APK**:
   ```powershell
   dotnet publish src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-android -c Release
   Copy-Item 'src/Nightowl.Maui/bin/Release/net10.0-android/publish/com.nightowl.app-Signed.apk' -Destination 'nightowl.apk' -Force
   ```

2. **Commit & Push to GitHub**:
   ```powershell
   git add .
   git commit -m "<Clear, descriptive commit message>"
   git push origin main
   ```

3. **Publish a GitHub Release**:
   Increment the semantic patch version (e.g., `v1.0.5`), tag it, and attach `nightowl.apk`:
   ```powershell
   gh release create <tag> nightowl.apk --title "<tag> - <Title>" --notes "<Release notes>"
   ```

4. **Provide Artifact to User**:
   Copy `nightowl.apk` to the conversation artifacts directory and update/create the `android_apk_download.md` artifact with a direct download link and QR code/install instructions so the user can immediately install the APK on their phone.
