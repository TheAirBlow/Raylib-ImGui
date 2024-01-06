using System.Numerics;
using ImGuiNET;

namespace Raylib_ImGui.Windows; 

/// <summary>
/// A simple choice window
/// </summary>
public class ChoiceWindow : GuiWindow {
    /// <summary>
    /// Popup's title
    /// </summary>
    private readonly string _title;
    
    /// <summary>
    /// Popup's message
    /// </summary>
    private readonly string _message;

    /// <summary>
    /// Constructs a choise window
    /// </summary>
    /// <param name="title">Title</param>
    /// <param name="message">Message</param>
    public ChoiceWindow(string title, string message) {
        _title = title; _message = message;
    }
    
    /// <summary>
    /// Choice window closed delegate
    /// </summary>
    public delegate void ClosedEventHandler(object sender, bool choice);

    /// <summary>
    /// Event fired when a choice was made
    /// </summary>
    public event ClosedEventHandler OnClosed;

    /// <summary>
    /// Fires the closed event and closes this window
    /// </summary>
    /// <param name="choice">Choise</param>
    private void Chosen(bool choice) {
        IsOpen = false; OnClosed.Invoke(this, choice);
    }
    
    /// <summary>
    /// Draws the choise window
    /// </summary>
    public override void DrawGUI(ImGuiRenderer renderer) {
        ImGui.OpenPopup($"{_title} ##{ID}");
        if (ImGui.BeginPopupModal($"{_title} ##{ID}", ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text(_message);
            var split = ImGui.GetWindowWidth() / 2;
            if (ImGui.Button("Yes", 
                    new Vector2(split - 12, 30)))
                Chosen(true);
            ImGui.SameLine();
            if (ImGui.Button("No", 
                    new Vector2(split - 12, 30)))
                Chosen(false);
            ImGui.EndPopup();
        }
    }
}