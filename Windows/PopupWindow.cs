using System.Numerics;
using ImGuiNET;

namespace Raylib_ImGui.Windows; 

/// <summary>
/// A simple popup window
/// </summary>
public class PopupWindow : GuiWindow {
    /// <summary>
    /// Popup's title
    /// </summary>
    private readonly string _title;
    
    /// <summary>
    /// Popup's message
    /// </summary>
    private readonly string _message;

    /// <summary>
    /// Constructs a popup window
    /// </summary>
    /// <param name="title">Title</param>
    /// <param name="message">Message</param>
    public PopupWindow(string title, string message) {
        _title = title; _message = message;
    }
    
    /// <summary>
    /// Draws the popup window
    /// </summary>
    public override void DrawGUI(ImGuiRenderer renderer) {
        ImGui.OpenPopup($"{_title} ##{ID}");
        if (ImGui.BeginPopupModal($"{_title} ##{ID}", ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text(_message);
            if (ImGui.Button("OK", new Vector2(
                    ImGui.GetWindowWidth() - 18, 30)))
                IsOpen = false;
            ImGui.EndPopup();
        }
    }
}