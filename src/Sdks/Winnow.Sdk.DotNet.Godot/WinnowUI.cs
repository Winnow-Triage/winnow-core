using Godot;
using System;
using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

namespace Winnow.Sdk.DotNet.GodotNode;

public partial class WinnowUI : Control
{
    [Export] public string ApiKey { get; set; } = "";
    [Export] public string ApiUrl { get; set; } = "https://api.winnowtriage.com";

    private LineEdit _titleInput;
    private TextEdit _descriptionInput;
    private Button _submitButton;
    private Label _statusLabel;
    private Panel _formPanel;
    private Panel _successPanel;

    private WinnowSdkClient _client;

    public override void _Ready()
    {
        // Try to find the WinnowGodot singleton if ApiKey is empty
        if (string.IsNullOrEmpty(ApiKey))
        {
            var winnowGodot = GetNodeOrNull("/root/WinnowGodot");
            if (winnowGodot != null && winnowGodot.Get("ApiKey").AsString() != "")
            {
                ApiKey = winnowGodot.Get("ApiKey").AsString();
            }
        }

        _client = new WinnowSdkClient(ApiKey, new WinnowConfig { BaseUrl = ApiUrl });

        _titleInput = GetNode<LineEdit>("Form/TitleInput");
        _descriptionInput = GetNode<TextEdit>("Form/DescriptionInput");
        _submitButton = GetNode<Button>("Form/SubmitButton");
        _statusLabel = GetNode<Label>("Form/StatusLabel");
        _formPanel = GetNode<Panel>("Form");
        _successPanel = GetNode<Panel>("Success");

        _submitButton.Pressed += OnSubmitPressed;
    }

    private async void OnSubmitPressed()
    {
        if (string.IsNullOrEmpty(_titleInput.Text) || string.IsNullOrEmpty(_descriptionInput.Text))
        {
            _statusLabel.Text = "Please fill in all fields.";
            return;
        }

        _submitButton.Disabled = true;
        _statusLabel.Text = "Submitting...";

        var payload = new ReportPayload
        {
            Title = _titleInput.Text,
            Message = _descriptionInput.Text,
            Platform = OS.GetName(),
            EngineVersion = Engine.GetVersionInfo()["string"].AsString()
        };

        try
        {
            await _client.SendReportAsync(payload);
            _formPanel.Visible = false;
            _successPanel.Visible = true;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Error: " + ex.Message;
            _submitButton.Disabled = false;
        }
    }

    public void Close()
    {
        Visible = false;
        _formPanel.Visible = true;
        _successPanel.Visible = false;
        _titleInput.Text = "";
        _descriptionInput.Text = "";
        _statusLabel.Text = "";
    }
}
