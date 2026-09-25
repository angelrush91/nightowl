#if ANDROID
using Android.Webkit;

namespace Nightowl.Maui.Platforms.Android;

public class PermissionWebChromeClient : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest? request)
    {
        if (request == null) return;

        try
        {
            // Grant requested WebView resources such as video capture for camera barcode scanning
            request.Grant(request.GetResources());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to grant WebView camera permission: {ex.Message}");
        }
    }
}
#endif
