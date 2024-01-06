using ImGuiNET;

namespace Raylib_ImGui.Windows; 

/// <summary>
/// Built-in ImGUI demo window
/// </summary>
public class DemoWindow : GuiWindow {
    /// <summary>
    /// Draws the demo window
    /// </summary>
    public override void DrawGUI(ImGuiRenderer renderer)
        => ImGui.ShowDemoWindow(ref IsOpen);
}