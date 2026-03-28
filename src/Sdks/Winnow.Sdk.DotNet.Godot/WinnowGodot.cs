using System;
using System.Threading.Tasks;
using Godot;
using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

namespace Winnow.Sdk.DotNet.GodotNode
{
    /// <summary>
    /// A Godot 4 AutoLoad (Singleton) Node wrapper for the Winnow SDK.
    /// Captures unhandled C# exceptions, grabs a screenshot, and sends the report.
    /// </summary>
    public partial class WinnowGodot : Node
    {
        [ExportCategory("Winnow Configuration")]
        [Export] public string ApiKey { get; set; } = string.Empty;
        
        [Export] public string Environment { get; set; } = "Production";
        
        [Export] public bool ForceOfflineInEditor { get; set; } = true;

        private WinnowSdkClient _winnowClient;

        public override void _Ready()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                GD.PushWarning("[WinnowGodot] API Key is empty. SDK will not initialize.");
                return;
            }

            // Determine if we should force offline mode (e.g. while testing in the Godot Editor)
            bool isOffline = ForceOfflineInEditor && OS.HasFeature("editor");

            var config = new WinnowConfig
            {
                Environment = Environment,
                OfflineMode = isOffline
            };

            _winnowClient = new WinnowSdkClient(ApiKey, config);

            // Hook into globally unhandled C# Exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        protected override void _ExitTree()
        {
            // Clean up event delegates to prevent memory leaks if the Node is ever freed
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleCrashException(ex);
            }
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                HandleCrashException(e.Exception);
            }
        }

        /// <summary>
        /// Orchestrates the process of capturing Godot state, rendering a screenshot, and dispatching.
        /// </summary>
        private void HandleCrashException(Exception ex)
        {
            if (_winnowClient == null) return;

            // We cannot safely block or yield from within these synchronous .NET exception event handlers 
            // directly if it locks the Godot main thread. We must marshal back or fire-and-forget an async task
            // that handles the Godot SceneTree interaction properly.
            CallDeferred(MethodName.CaptureAndSendAsync, ex.Message, ex.StackTrace);
        }

        private async void CaptureAndSendAsync(string message, string stackTrace)
        {
            byte[] screenshotBytes = null;

            try
            {
                // Wait for the end of the current frame to ensure the Viewport has fully rendered
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var viewport = GetViewport();
                if (viewport != null)
                {
                    var texture = viewport.GetTexture();
                    if (texture != null)
                    {
                        var image = texture.GetImage();
                        if (image != null)
                        {
                            // Encode to JPG to vastly reduce payload size compared to PNG
                            screenshotBytes = image.SaveJpgToBuffer(75);
                        }
                    }
                }
            }
            catch (Exception captureEx)
            {
                GD.PushWarning($"[WinnowGodot] Failed to capture screenshot: {captureEx.Message}");
            }

            // Construct standard SDK payload using Godot-specific environment mappings
            var payload = new ReportPayload
            {
                Message = message,
                StackTrace = stackTrace,
                OsVersion = OS.GetName(),
                EngineVersion = Engine.GetVersionInfo()["string"].AsString(),
                Platform = OS.GetName(), // Godot relies on OS.GetName for platform as well
                Resolution = $"{GetViewport().GetVisibleRect().Size.X}x{GetViewport().GetVisibleRect().Size.Y}"
            };

            var deviceModel = OS.GetModelName();
            if (!string.IsNullOrWhiteSpace(deviceModel))
            {
                payload.Metadata["DeviceModel"] = deviceModel;
            }

            // Fire off the HTTP request on the .NET Task ThreadPool so the Godot Main loop does not stutter or hang
            _ = Task.Run(async () =>
            {
                try
                {
                    await _winnowClient.SendReportAsync(payload, screenshotBytes).ConfigureAwait(false);
                }
                catch (Exception sendEx)
                {
                    // Fallback log. The internal client is wrapped, but this catches Task threading specifics.
                    GD.PushWarning($"[WinnowGodot] Background dispatch failed: {sendEx.Message}");
                }
            });
        }
    }
}
