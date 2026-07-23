#if ANDROID
using Android.Webkit;

namespace AthkarApp.Platforms.Android
{
    public class CustomWebChromeClient : WebChromeClient
    {
        public override void OnPermissionRequest(PermissionRequest request)
        {
            // Automatically grant WebRTC permissions (Microphone) for Agora SDK
            request.Grant(request.GetResources());
        }

        public override bool OnJsAlert(global::Android.Webkit.WebView view, string url, string message, global::Android.Webkit.JsResult result)
        {
            global::Android.Widget.Toast.MakeText(global::Android.App.Application.Context, message, global::Android.Widget.ToastLength.Long).Show();
            result.Confirm();
            return true;
        }
    }
}
#endif
