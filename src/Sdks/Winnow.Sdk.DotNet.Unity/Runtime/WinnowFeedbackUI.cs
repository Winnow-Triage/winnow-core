using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

namespace Winnow.Sdk.DotNet.Unity
{
    /// <summary>
    /// A "ready-to-use" Unity UI component for manual bug reporting.
    /// Attach this to a GameObject and link it to your UI elements in the Inspector.
    /// </summary>
    public class WinnowFeedbackUI : MonoBehaviour
    {
        [Header("Winnow Configuration")]
        public string apiKey;
        public string apiUrl = "https://api.winnowtriage.com";

        [Header("UI References")]
        public InputField titleInputField;
        public InputField descriptionInputField;
        public Button submitButton;
        public Text statusText;
        public GameObject formPanel;
        public GameObject successPanel;

        private WinnowSdkClient _client;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Try to find a global WinnowUnity component as a fallback
                var winnowUnity = FindObjectOfType<WinnowUnity>();
                if (winnowUnity != null)
                {
                    apiKey = winnowUnity.apiKey;
                    apiUrl = winnowUnity.environment; // Assuming environment is the URL or maps to it
                }
            }

            _client = new WinnowSdkClient(apiKey, new WinnowConfig { BaseUrl = apiUrl });
        }

        private void Start()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (successPanel != null)
            {
                successPanel.SetActive(false);
            }
        }

        private void OnSubmitClicked()
        {
            if (string.IsNullOrWhiteSpace(titleInputField.text) || string.IsNullOrWhiteSpace(descriptionInputField.text))
            {
                UpdateStatus("Please fill in all fields.");
                return;
            }

            _ = SendReportAsync();
        }

        private async Task SendReportAsync()
        {
            SetSubmitEnabled(false);
            UpdateStatus("Submitting...");

            var payload = new ReportPayload
            {
                Title = titleInputField.text,
                Message = descriptionInputField.text,
                Platform = Application.platform.ToString(),
                EngineVersion = Application.unityVersion,
                AppVersion = Application.version
            };

            try
            {
                await _client.SendReportAsync(payload);
                ShowSuccess();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error: " + ex.Message);
                SetSubmitEnabled(true);
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetSubmitEnabled(bool enabled)
        {
            if (submitButton != null)
            {
                submitButton.interactable = enabled;
            }
        }

        private void ShowSuccess()
        {
            if (formPanel != null) formPanel.SetActive(false);
            if (successPanel != null) successPanel.SetActive(true);
        }

        public void ResetForm()
        {
            if (titleInputField != null) titleInputField.text = "";
            if (descriptionInputField != null) descriptionInputField.text = "";
            if (statusText != null) statusText.text = "";
            if (formPanel != null) formPanel.SetActive(true);
            if (successPanel != null) successPanel.SetActive(false);
            SetSubmitEnabled(true);
        }
    }
}
