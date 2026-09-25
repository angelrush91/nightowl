using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Nightowl.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted)
        {
            RequestPermissions(new[] { global::Android.Manifest.Permission.Camera }, 1001);
        }
    }
}
