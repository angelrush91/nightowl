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
            var requestedResources = request.GetResources() ?? Array.Empty<string>();
            // Principle of Least Privilege: only grant video capture for barcode scanning
            var allowedResources = requestedResources
                .Where(r => r == PermissionRequest.ResourceVideoCapture)
                .ToArray();

            if (allowedResources.Length > 0)
            {
                request.Grant(allowedResources);
            }
            else
            {
                request.Deny();
            }
        }
        catch (Exception)
        {
            request.Deny();
        }
    }
}
#endif
