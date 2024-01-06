using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Raylib_CsLo;

namespace Raylib_ImGui.Windows; 

/// <summary>
/// About Raylib_ImGui window
/// </summary>
public class AboutWindow : GuiWindow {
    /// <summary>
    /// Raylib_ImGui icon
    /// </summary>
    private IntPtr? _icon;
    
    /// <summary>
    /// TheAirBlow's profile picture
    /// </summary>
    private IntPtr? _pfp;

    /// <summary>
    /// Draws the about window
    /// </summary>
    public override void DrawGUI(ImGuiRenderer renderer) {
        _icon ??= Assembly.GetExecutingAssembly().GetEmbeddedResource("icon.png")
            .LoadAsTexture(".png").CreateBinding();
        _pfp ??= Assembly.GetExecutingAssembly().GetEmbeddedResource("pfp.png")
            .LoadAsTexture(".png").CreateBinding();
        if (ImGui.Begin($"About Raylib-ImGui ##{ID}", ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (ImGui.BeginTable("Logo", 2)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Image(_icon!.Value, new Vector2(60, 60));
                ImGui.TableNextColumn();
                ImGui.Text("Raylib-ImGui is an open-source ImGui renderer for Raylib");
                ImGui.Text("Licence: Mozilla Public License 2.0");
                ImGui.Text("GitHub: TheAirBlow/Raylib_ImGui");
                ImGui.EndTable();
            }
            ImGui.Separator();
            if (ImGui.BeginTable("Dev", 2)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Image(_pfp!.Value, new Vector2(60, 60));
                ImGui.TableNextColumn();
                ImGui.Text("Made by TheAirBlow from scratch");
                ImGui.Text("If you find this useful, consider donating:");
                ImGui.Text("https://ko-fi.com/sussydev");
                ImGui.EndTable();
            }
            ImGui.Separator();
            ImGui.Text($"Running Raylib_ImGui version {Assembly.GetExecutingAssembly().GetName().Version}");
            ImGui.Text($"Running Raylib_CsLo version {typeof(Raylib).Assembly.GetName().Version}");
            ImGui.Text($"Running ImGui.NET version {typeof(ImGui).Assembly.GetName().Version}");
            ImGui.End();
        }
    }
}