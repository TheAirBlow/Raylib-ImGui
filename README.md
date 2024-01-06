# Raylib-ImGui
This is a simple plug-and-play [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) renderer for [Raylib_CsLo](https://github.com/NotNotTech/Raylib-CsLo). \
Intended for use as a GUI library, but you can use it in your game if you want to. \
**Note: `imgui.ini` is disabled by default. Set `IniFilename` manually.**

## Features
- Supports `GetClipboardText`/`SetClipboardText`
- Supports `SetMouseCursor`/`SetCursorPos`
- A simple ImGui window management system

## Window system
The `ImGuiRenderer` has a built-in way to manage ImGui windows. \
Additionally, this library ships with multiple example windows:
- `ChoiceWindow` - Simple yes or no modal
```csharp
var window = new ChoiceWindow("Choice Popup", 
    "Do you like the game \"Among Us\"?");
window.OnClosed += (_, choice) => {
    renderer.OpenWindow(new PopupWindow("Choice Popup", choice 
        ? "You've probably got brain damage now.\nThis is dangerous, consult a doctor." 
        : "You can't escape Among Us.\nIt's everywhere, whether you like it or not."));
};
renderer.OpenWindow(window);
```
- `PopupWindow` - Simple popup modal
```csharp
renderer.OpenWindow(new PopupWindow("Popup", "Hello, this is a popup!"));
```
- `AboutWindow` - About Raylib-ImGui
```csharp
 renderer.OpenWindow(new AboutWindow());
```
- `DemoWindow` - ImGui demo window
```csharp
renderer.OpenWindow(new DemoWindow());
```

## Example usage
```csharp
using System.Numerics;
using ImGuiNET;
using Raylib_CsLo;
using Raylib_ImGui;
using Raylib_ImGui.Windows;

// Set up raylib and create an ImGui renderer
var renderer = new ImGuiRenderer();
Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
Raylib.InitWindow(1280, 720, "Raylib-ImGui demo");

// Here it isn't really necessary, because it would be the correct context
// But if you have multiple ImGui contexts, you can use this
renderer.SwitchContext();

// You have to call this at least once before the first frame
renderer.RecreateFontTexture();

// At any point you can change the current ImGui styles
ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign,
    new Vector2(0.5f, 0.5f));
ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10);
ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6);
ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 12);
ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

// This is how you can load a custom font
ImFontPtr fontPointer;
unsafe {
    var font = typeof(ImGuiRenderer).Assembly
        .GetEmbeddedResource("Comfortaa.ttf");
    fixed (byte* p = font) fontPointer = 
        ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
            (IntPtr)p, font.Length, 18,
            ImGuiNative.ImFontConfig_ImFontConfig(),
            ImGui.GetIO().Fonts.GetGlyphRangesDefault());
    
    // You have to call this every time you add or delete a font
    renderer.RecreateFontTexture();
}

// Set window icon to Raylib-ImGui icon
Raylib.SetWindowIcon(typeof(ImGuiRenderer).Assembly
    .GetEmbeddedResource("icon.png").LoadAsImage(".png"));

// Draw a background image behind ImGui interface
// This is just an example, you can draw your game instead
var bg = typeof(ImGuiRenderer).Assembly
    .GetEmbeddedResource("bg.png").LoadAsTexture(".png");

// Here's out main game loop
while (!Raylib.WindowShouldClose()) {
    // Process input events for ImGui
    renderer.Update();
    
    // Begin drawing and create a new frame
    Raylib.BeginDrawing(); ImGui.NewFrame();
    
    // Here you can draw anything with Raylib or ImGui
    Raylib.ClearBackground(new Color(0, 0, 0, 255));
    Raylib.DrawTexturePro(bg, new Rectangle(0, 0, bg.width, bg.height),
        new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
        new Vector2(0, 0), 0f, new Color(255, 255, 255, 255));
    ImGui.PushFont(fontPointer);
    ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(),
        ImGuiDockNodeFlags.PassthruCentralNode);
    if (ImGui.BeginMainMenuBar()) {
        if (ImGui.BeginMenu("Open")) {
            if (ImGui.MenuItem("Example Popup"))
                renderer.OpenWindow(new PopupWindow(
                    "Popup", "Hello, this is a popup!"));
            if (ImGui.MenuItem("Example Choice")) {
                var window = new ChoiceWindow("Choice Popup", 
                    "Do you like the game \"Among Us\"?");
                window.OnClosed += (_, choice) => {
                    renderer.OpenWindow(new PopupWindow("Choice Popup", choice 
                        ? "You've probably got brain damage now.\nThis is dangerous, consult a doctor." 
                        : "You can't escape Among Us.\nIt's everywhere, whether you like it or not."));
                };
                renderer.OpenWindow(window);
            }
            ImGui.Separator();
            if (ImGui.MenuItem("ImGui Demo"))
                renderer.OpenWindow(new DemoWindow());
            
            ImGui.EndMenu();
        }
        
        if (ImGui.BeginMenu("Help")) {
            if (ImGui.MenuItem("About Raylib-ImGui"))
                renderer.OpenWindow(new AboutWindow());
                
            ImGui.EndMenu();
        }
        
        ImGui.EndMainMenuBar();
    }
    
    // Render windows managed by the built-in window system
    renderer.DrawWindows();
    
    // Render the ImGui frame using RlGl (Raylib + OpenGL)
    renderer.RenderImGui();
    
    // Stops drawing
    Raylib.EndDrawing();
}

Raylib.CloseWindow();
```