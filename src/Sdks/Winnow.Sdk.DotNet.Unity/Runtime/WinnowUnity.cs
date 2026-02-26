using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

namespace Winnow.Sdk.DotNet.Unity
{
    /// <summary>
    /// A drop-in Unity component wrapper around the Winnow.Sdk.DotNet.Core client.
    /// Captures unhandled errors and exceptions, optionally capturing a screenshot.
    /// </summary>
    public class WinnowUnity : MonoBehaviour
    {
        [Header("Winnow Configuration")]
        [Tooltip("Your project API key.")]
        public string apiKey;

        [Tooltip("The environment identifier (e.g., Development, Production).")]
        public string environment = "Production";

        [Tooltip("If true, only prints to the console locally when inside the Unity Editor without sending payloads to the API.")]
        public bool forceOfflineInEditor = true;

        private WinnowSdkClient _winnowClient;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogWarning("[WinnowUnity] API key is missing. Winnow reporting will not initialize.");
                return;
            }

            // Map inspector properties to the core WinnowConfig model
            var config = new WinnowConfig
            {
                Environment = environment,
                OfflineMode = forceOfflineInEditor && Application.isEditor
            };

            // Instantiate the core library client
            _winnowClient = new WinnowSdkClient(apiKey, config);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLogMessage;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLogMessage;
            
            // Note: We don't necessarily dispose _winnowClient here on every disable 
            // since HttpClient is long-lived underneath, but we stop listening for logs.
        }

        /// <summary>
        /// Hooks into Unity's global log system. We filter heavily to save quota.
        /// </summary>
        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            // Only care about actual errors and unhandled exceptions
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (_winnowClient == null) return;

                // Fire off the coroutine to safely capture screenshots at the end of the current frame
                StartCoroutine(CaptureAndSendReportCoroutine(logString, stackTrace));
            }
        }

        private IEnumerator CaptureAndSendReportCoroutine(string message, string stackTrace)
        {
            // Mandatory for ScreenCapture APIs to avoid reading GPU data mid-render
            yield return new WaitForEndOfFrame();

            byte[] screenshotBytes = null;
            try
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture != null)
                {
                    // Encode to JPG instead of PNG for massive bandwidth savings
                    screenshotBytes = texture.EncodeToJPG(75);
                    
                    // Crucial to destroy UI/Texture resources to avoid Unity memory leaks
                    Destroy(texture); 
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WinnowUnity] Failed to capture screenshot: {ex.Message}");
            }

            // Build payload using standard Unity systemic variables mapped to the library's POCO
            var payload = new ReportPayload
            {
                Message = message,
                StackTrace = stackTrace,
                OsVersion = SystemInfo.operatingSystem,
                EngineVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                AppVersion = Application.version,
                Resolution = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}"
            };
            
            if (!string.IsNullOrWhiteSpace(SystemInfo.deviceModel))
            {
                payload.Metadata["DeviceModel"] = SystemInfo.deviceModel;
            }

            // Send asynchronously without waiting/blocking the Unity Main Thread.
            // Push it fully to the thread pool so gameplay does not stutter.
            Task.Run(async () => 
            {
                try
                {
                    await _winnowClient.SendReportAsync(payload, screenshotBytes).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Core SDK is wrapped tightly, but safety first against unexpected thread issues.
                    Debug.LogWarning($"[WinnowUnity] Background task exception while sending: {ex.Message}");
                }
            });
        }
    }
}
