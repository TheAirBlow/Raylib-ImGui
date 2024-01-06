namespace Raylib_ImGui.Windows; 

/// <summary>
/// An ImGUI window
/// </summary>
public abstract class GuiWindow {
    /// <summary>
    /// Is the window currently open
    /// </summary>
    public bool IsOpen = true;

    /// <summary>
    /// Random window identificator
    /// </summary>
    protected readonly string ID = Guid.NewGuid().ToString();

    /// <summary>
    /// Draw the window's GUI here
    /// </summary>
    public virtual void DrawGUI(ImGuiRenderer renderer) {
        
    }
}