using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Raylib_CsLo;
using Raylib_ImGui.Windows;

namespace Raylib_ImGui;

/// <summary>
/// Simple ImGui renderer for Raylib_CsLo
/// </summary>
public class ImGuiRenderer {
    private ImGuiMouseCursor _cursor = ImGuiMouseCursor.COUNT;
    private readonly List<GuiWindow> _windows = new();
    private readonly Queue<GuiWindow> _toAdd = new();
    private readonly IntPtr _context;
    private IntPtr? _fontTexture;

    /// <summary>
    /// Constructs a new Raylib ImGuiRenderer
    /// </summary>
    public unsafe ImGuiRenderer() {
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.NativePtr->IniFilename = (byte*)IntPtr.Zero;
        io.DisplaySize = new Vector2(800, 480);
        io.DisplayFramebufferScale = Vector2.One;
        io.MousePos = new Vector2(0, 0);
        io.Fonts.AddFontDefault();
        io.SetClipboardTextFn = GetType().GetMethod(nameof(SetClipboardCallback), 
            BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
        io.GetClipboardTextFn = GetType().GetMethod(nameof(GetClipboardCallback), 
            BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
        var handle = GCHandle.Alloc(Encoding.ASCII.GetBytes(
            "imgui_impl_raylib_csharp\0"), GCHandleType.Pinned);
        io.NativePtr->BackendPlatformName = (byte*)handle.AddrOfPinnedObject();
        handle = GCHandle.Alloc(Encoding.ASCII.GetBytes(
            "imgui_impl_opengl\0"), GCHandleType.Pinned);
        io.NativePtr->BackendRendererName = (byte*)handle.AddrOfPinnedObject();
    }

    /// <summary>
    /// Set clipboard ImGui callback
    /// </summary>
    /// <param name="user_data">User data</param>
    /// <param name="text">Clipboard text</param>
    private static void SetClipboardCallback(IntPtr user_data, IntPtr text)
        => Raylib.SetClipboardText(Marshal.PtrToStringAuto(text)!);

    /// <summary>
    /// Get clipboard ImGui callback
    /// </summary>
    /// <param name="user_data">User data</param>
    private static unsafe IntPtr GetClipboardCallback(IntPtr user_data) {
        // raylib can return a null pointer, and ImGui.NET would crash
        // because of it, so I did this bullshit to fix it, crazy right?
        var ptr = (IntPtr)Raylib.GetClipboardText();
        if (ptr == IntPtr.Zero) ptr = Marshal.StringToHGlobalAuto("");
        return ptr;
    }
    
    /// <summary>
    /// Switches ImGUI context
    /// </summary>
    public void SwitchContext()
        => ImGui.SetCurrentContext(_context);

    /// <summary>
    /// Pumps input events
    /// </summary>
    /// <returns>True on success</returns>
    public void Update() {
        var io = ImGui.GetIO();
        if (Raylib.IsWindowFullscreen()) {
            var monitor = Raylib.GetCurrentMonitor();
            io.DisplaySize.X = Raylib.GetMonitorWidth(monitor);
            io.DisplaySize.Y = Raylib.GetMonitorHeight(monitor);
        } else {
            io.DisplaySize.X = Raylib.GetScreenWidth();
            io.DisplaySize.Y = Raylib.GetScreenHeight();
        }
    
        var resolutionScale = Raylib.GetWindowScaleDPI();
        io.DisplayFramebufferScale = new Vector2(resolutionScale.X, resolutionScale.Y);
        io.DeltaTime = Raylib.GetFrameTime();

        if (io.WantSetMousePos)
            Raylib.SetMousePosition((int)io.MousePos.X, (int)io.MousePos.Y);
        else io.AddMousePosEvent(Raylib.GetMouseX(), Raylib.GetMouseY());

        void handleMouseEvent(MouseButton rayMouse, ImGuiMouseButton imGuiMouse) {
            if (Raylib.IsMouseButtonPressed(rayMouse))
                io.AddMouseButtonEvent((int)imGuiMouse, true);
            else if (Raylib.IsMouseButtonReleased(rayMouse))
                io.AddMouseButtonEvent((int)imGuiMouse, false);
        }

        handleMouseEvent(MouseButton.MOUSE_BUTTON_LEFT, ImGuiMouseButton.Left);
        handleMouseEvent(MouseButton.MOUSE_BUTTON_RIGHT, ImGuiMouseButton.Right);
        handleMouseEvent(MouseButton.MOUSE_BUTTON_MIDDLE, ImGuiMouseButton.Middle);
        handleMouseEvent(MouseButton.MOUSE_BUTTON_FORWARD, ImGuiMouseButton.Middle + 1);
        handleMouseEvent(MouseButton.MOUSE_BUTTON_BACK, ImGuiMouseButton.Middle + 2);
        var mouseWheel = Raylib.GetMouseWheelMoveV();
        io.AddMouseWheelEvent(mouseWheel.X, mouseWheel.Y);
        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0) {
            var cursor = ImGui.GetMouseCursor();
            if (cursor != _cursor || io.MouseDrawCursor) {
                _cursor = cursor;
                if (io.MouseDrawCursor || cursor == ImGuiMouseCursor.None)
                    Raylib.HideCursor();
                else {
                    Raylib.ShowCursor(); 
                    Raylib.SetMouseCursor(_cursorMap[cursor]);
                }
            }
        }
        
        void handleModKeys(KeyboardKey left, KeyboardKey right, ImGuiKey key) {
            if (Raylib.IsKeyPressed(left) || Raylib.IsKeyPressed(right))
                io.AddKeyEvent(key, true);
        
            if (Raylib.IsKeyReleased(left) || Raylib.IsKeyReleased(right))
                io.AddKeyEvent(key, false);
        }
        
        handleModKeys(KeyboardKey.KEY_LEFT_ALT, KeyboardKey.KEY_RIGHT_ALT, ImGuiKey.ModAlt);
        handleModKeys(KeyboardKey.KEY_LEFT_SHIFT, KeyboardKey.KEY_RIGHT_SHIFT, ImGuiKey.ModShift);
        handleModKeys(KeyboardKey.KEY_LEFT_SUPER, KeyboardKey.KEY_RIGHT_SUPER, ImGuiKey.ModSuper);
        handleModKeys(KeyboardKey.KEY_LEFT_CONTROL, KeyboardKey.KEY_RIGHT_CONTROL, ImGuiKey.ModCtrl);
        io.AddFocusEvent(Raylib.IsWindowFocused());
        var keyId = Raylib.GetKeyPressed();
        while (keyId != 0) {
            io.AddKeyEvent(_keyMap[(KeyboardKey)keyId], true);
            keyId = Raylib.GetKeyPressed();
        }
        
        foreach (var pair in _keyMap)
            if (Raylib.IsKeyReleased(pair.Key))
                io.AddKeyEvent(pair.Value, false);
        
        var pressed = Raylib.GetCharPressed();
        while (pressed != 0) {
            io.AddInputCharacter((uint)pressed);
            pressed = Raylib.GetCharPressed();
        }
    }

    /// <summary>
    /// Draw all managed windows
    /// </summary>
    public void DrawWindows() {
        while (_toAdd.TryDequeue(out var window))
            _windows.Add(window);
        var closed = new List<GuiWindow>();
        foreach (var window in _windows) {
            if (!window.IsOpen) {
                closed.Add(window);
                continue;
            }
            window.DrawGUI(this);
        }
        
        foreach (var window in closed)
            _windows.Remove(window);
    }
    
    /// <summary>
    /// Adds a window to render
    /// </summary>
    /// <param name="window">Window</param>
    public void OpenWindow(GuiWindow window)
        => _toAdd.Enqueue(window);

    /// <summary>
    /// Recreates the font texture used to render text
    /// </summary>
    public unsafe void RecreateFontTexture() {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height);
        var image = Raylib.GenImageColor(width, height, new Color(0, 0, 0, 0));
        var size = width * height * 4;
        Buffer.MemoryCopy(pixels, image.data, size, size);
        var fontTexture = (Texture*)_fontTexture;
        if (_fontTexture != null) {
            Raylib.UnloadTexture(*fontTexture);
            Raylib.MemFree(fontTexture);
        }
            
        fontTexture = (Texture*)Raylib.MemAlloc((uint)sizeof(Texture));
        *fontTexture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        _fontTexture = (IntPtr)fontTexture;
        io.Fonts.TexID = _fontTexture.Value;
    }

    /// <summary>
    /// Renders ImDraw data 
    /// </summary>
    public unsafe void RenderImGui() {
        ImGui.Render();
        var io = ImGui.GetIO();
        var data = ImGui.GetDrawData();
        RlGl.rlDrawRenderBatchActive();
        RlGl.rlDisableBackfaceCulling();
        for (var i = 0; i < data.CmdListsCount; ++i) {
            var drawList = data.CmdLists[i];
            for (var j = 0; j < drawList.CmdBuffer.Size; j++) {
                var cmd = drawList.CmdBuffer[j];
                var x = cmd.ClipRect.X - data.DisplayPos.X;
                var y = cmd.ClipRect.Y - data.DisplayPos.Y;
                var width = cmd.ClipRect.Z - (cmd.ClipRect.X - data.DisplayPos.X);
                var height = cmd.ClipRect.W - (cmd.ClipRect.Y - data.DisplayPos.Y);
                RlGl.rlEnableScissorTest();
                RlGl.rlScissor((int)(x * io.DisplayFramebufferScale.X),
                    (int)((io.DisplaySize.Y - (int)(y + height)) * io.DisplayFramebufferScale.Y),
                    (int)(width * io.DisplayFramebufferScale.X),
                    (int)(height * io.DisplayFramebufferScale.Y));
                if (cmd.ElemCount < 3) {
                    RlGl.rlDrawRenderBatchActive();
                    continue;
                }
                var texture = (Texture*)cmd.TextureId;
                var textureId = cmd.TextureId == IntPtr.Zero ? 0 : texture->id;
                RlGl.rlBegin(RlGl.RL_TRIANGLES);
                RlGl.rlSetTexture(textureId);
                for (var k = 0; k <= cmd.ElemCount - 3; k += 3) {
                    if (RlGl.rlCheckRenderBatchLimit(3)) {
                        RlGl.rlBegin(RlGl.RL_TRIANGLES);
                        RlGl.rlSetTexture(textureId);
                    }

                    void renderVertex(ImDrawVertPtr vertex) {
                        RlGl.rlColor4ub((byte)((vertex.col >> 0) & 0xff), 
                            (byte)((vertex.col >> 8) & 0xff),
                            (byte)((vertex.col >> 16) & 0xff),
                            (byte)((vertex.col >> 24) & 0xff));
                        RlGl.rlTexCoord2f(vertex.uv.X, vertex.uv.Y);
                        RlGl.rlVertex2f(vertex.pos.X, vertex.pos.Y);
                    }
                    
                    renderVertex(drawList.VtxBuffer[drawList.IdxBuffer[(int)cmd.IdxOffset + k]]);
                    renderVertex(drawList.VtxBuffer[drawList.IdxBuffer[(int)cmd.IdxOffset + k + 1]]);
                    renderVertex(drawList.VtxBuffer[drawList.IdxBuffer[(int)cmd.IdxOffset + k + 2]]);
                }
                RlGl.rlEnd();
                RlGl.rlDrawRenderBatchActive();
            }
        }
        RlGl.rlSetTexture(0);
        RlGl.rlDisableScissorTest();
        RlGl.rlEnableBackfaceCulling();
    }

    /// <summary>
    /// ImGui mouse cursor to Raylib map
    /// </summary>
    private static readonly Dictionary<ImGuiMouseCursor, MouseCursor> _cursorMap = new() {
        [ImGuiMouseCursor.Arrow] = MouseCursor.MOUSE_CURSOR_ARROW,
        [ImGuiMouseCursor.TextInput] = MouseCursor.MOUSE_CURSOR_IBEAM,
        [ImGuiMouseCursor.Hand] = MouseCursor.MOUSE_CURSOR_POINTING_HAND,
        [ImGuiMouseCursor.ResizeAll] = MouseCursor.MOUSE_CURSOR_RESIZE_ALL,
        [ImGuiMouseCursor.ResizeEW] = MouseCursor.MOUSE_CURSOR_RESIZE_EW,
        [ImGuiMouseCursor.ResizeNESW] = MouseCursor.MOUSE_CURSOR_RESIZE_NESW,
        [ImGuiMouseCursor.ResizeNS] = MouseCursor.MOUSE_CURSOR_RESIZE_NS,
        [ImGuiMouseCursor.ResizeNWSE] = MouseCursor.MOUSE_CURSOR_RESIZE_NWSE,
        [ImGuiMouseCursor.NotAllowed] = MouseCursor.MOUSE_CURSOR_NOT_ALLOWED
    };
    
    /// <summary>
    /// Raylib keyboard key to ImGui map
    /// </summary>
    private static readonly Dictionary<KeyboardKey, ImGuiKey> _keyMap = new() {
        [KeyboardKey.KEY_APOSTROPHE] = ImGuiKey.Apostrophe,
        [KeyboardKey.KEY_COMMA] = ImGuiKey.Comma,
        [KeyboardKey.KEY_MINUS] = ImGuiKey.Minus,
        [KeyboardKey.KEY_PERIOD] = ImGuiKey.Period,
        [KeyboardKey.KEY_SLASH] = ImGuiKey.Slash,
        [KeyboardKey.KEY_ZERO] = ImGuiKey._0,
        [KeyboardKey.KEY_ONE] = ImGuiKey._1,
        [KeyboardKey.KEY_TWO] = ImGuiKey._2,
        [KeyboardKey.KEY_THREE] = ImGuiKey._3,
        [KeyboardKey.KEY_FOUR] = ImGuiKey._4,
        [KeyboardKey.KEY_FIVE] = ImGuiKey._5,
        [KeyboardKey.KEY_SIX] = ImGuiKey._6,
        [KeyboardKey.KEY_SEVEN] = ImGuiKey._7,
        [KeyboardKey.KEY_EIGHT] = ImGuiKey._8,
        [KeyboardKey.KEY_NINE] = ImGuiKey._9,
        [KeyboardKey.KEY_SEMICOLON] = ImGuiKey.Semicolon,
        [KeyboardKey.KEY_EQUAL] = ImGuiKey.Equal,
        [KeyboardKey.KEY_A] = ImGuiKey.A,
        [KeyboardKey.KEY_B] = ImGuiKey.B,
        [KeyboardKey.KEY_C] = ImGuiKey.C,
        [KeyboardKey.KEY_D] = ImGuiKey.D,
        [KeyboardKey.KEY_E] = ImGuiKey.E,
        [KeyboardKey.KEY_F] = ImGuiKey.F,
        [KeyboardKey.KEY_G] = ImGuiKey.G,
        [KeyboardKey.KEY_H] = ImGuiKey.H,
        [KeyboardKey.KEY_I] = ImGuiKey.I,
        [KeyboardKey.KEY_J] = ImGuiKey.J,
        [KeyboardKey.KEY_K] = ImGuiKey.K,
        [KeyboardKey.KEY_L] = ImGuiKey.L,
        [KeyboardKey.KEY_M] = ImGuiKey.M,
        [KeyboardKey.KEY_N] = ImGuiKey.N,
        [KeyboardKey.KEY_O] = ImGuiKey.O,
        [KeyboardKey.KEY_P] = ImGuiKey.P,
        [KeyboardKey.KEY_Q] = ImGuiKey.Q,
        [KeyboardKey.KEY_R] = ImGuiKey.R,
        [KeyboardKey.KEY_S] = ImGuiKey.S,
        [KeyboardKey.KEY_T] = ImGuiKey.T,
        [KeyboardKey.KEY_U] = ImGuiKey.U,
        [KeyboardKey.KEY_V] = ImGuiKey.V,
        [KeyboardKey.KEY_W] = ImGuiKey.W,
        [KeyboardKey.KEY_X] = ImGuiKey.X,
        [KeyboardKey.KEY_Y] = ImGuiKey.Y,
        [KeyboardKey.KEY_Z] = ImGuiKey.Z,
        [KeyboardKey.KEY_SPACE] = ImGuiKey.Space,
        [KeyboardKey.KEY_ESCAPE] = ImGuiKey.Escape,
        [KeyboardKey.KEY_ENTER] = ImGuiKey.Enter,
        [KeyboardKey.KEY_TAB] = ImGuiKey.Tab,
        [KeyboardKey.KEY_BACKSPACE] = ImGuiKey.Backspace,
        [KeyboardKey.KEY_INSERT] = ImGuiKey.Insert,
        [KeyboardKey.KEY_DELETE] = ImGuiKey.Delete,
        [KeyboardKey.KEY_RIGHT] = ImGuiKey.RightArrow,
        [KeyboardKey.KEY_LEFT] = ImGuiKey.LeftArrow,
        [KeyboardKey.KEY_DOWN] = ImGuiKey.DownArrow,
        [KeyboardKey.KEY_UP] = ImGuiKey.UpArrow,
        [KeyboardKey.KEY_PAGE_UP] = ImGuiKey.PageUp,
        [KeyboardKey.KEY_PAGE_DOWN] = ImGuiKey.PageDown,
        [KeyboardKey.KEY_HOME] = ImGuiKey.Home,
        [KeyboardKey.KEY_END] = ImGuiKey.End,
        [KeyboardKey.KEY_CAPS_LOCK] = ImGuiKey.CapsLock,
        [KeyboardKey.KEY_SCROLL_LOCK] = ImGuiKey.ScrollLock,
        [KeyboardKey.KEY_NUM_LOCK] = ImGuiKey.NumLock,
        [KeyboardKey.KEY_PRINT_SCREEN] = ImGuiKey.PrintScreen,
        [KeyboardKey.KEY_PAUSE] = ImGuiKey.Pause,
        [KeyboardKey.KEY_F1] = ImGuiKey.F1,
        [KeyboardKey.KEY_F2] = ImGuiKey.F2,
        [KeyboardKey.KEY_F3] = ImGuiKey.F3,
        [KeyboardKey.KEY_F4] = ImGuiKey.F4,
        [KeyboardKey.KEY_F5] = ImGuiKey.F5,
        [KeyboardKey.KEY_F6] = ImGuiKey.F6,
        [KeyboardKey.KEY_F7] = ImGuiKey.F7,
        [KeyboardKey.KEY_F8] = ImGuiKey.F8,
        [KeyboardKey.KEY_F9] = ImGuiKey.F9,
        [KeyboardKey.KEY_F10] = ImGuiKey.F10,
        [KeyboardKey.KEY_F11] = ImGuiKey.F11,
        [KeyboardKey.KEY_F12] = ImGuiKey.F12,
        [KeyboardKey.KEY_LEFT_SHIFT] = ImGuiKey.LeftShift,
        [KeyboardKey.KEY_LEFT_CONTROL] = ImGuiKey.LeftCtrl,
        [KeyboardKey.KEY_LEFT_ALT] = ImGuiKey.LeftAlt,
        [KeyboardKey.KEY_LEFT_SUPER] = ImGuiKey.LeftSuper,
        [KeyboardKey.KEY_RIGHT_SHIFT] = ImGuiKey.RightShift,
        [KeyboardKey.KEY_RIGHT_CONTROL] = ImGuiKey.RightCtrl,
        [KeyboardKey.KEY_RIGHT_ALT] = ImGuiKey.RightAlt,
        [KeyboardKey.KEY_RIGHT_SUPER] = ImGuiKey.RightSuper,
        [KeyboardKey.KEY_KB_MENU] = ImGuiKey.Menu,
        [KeyboardKey.KEY_LEFT_BRACKET] = ImGuiKey.LeftBracket,
        [KeyboardKey.KEY_BACKSLASH] = ImGuiKey.Backslash,
        [KeyboardKey.KEY_RIGHT_BRACKET] = ImGuiKey.RightBracket,
        [KeyboardKey.KEY_GRAVE] = ImGuiKey.GraveAccent,
        [KeyboardKey.KEY_KP_0] = ImGuiKey.Keypad0,
        [KeyboardKey.KEY_KP_1] = ImGuiKey.Keypad1,
        [KeyboardKey.KEY_KP_2] = ImGuiKey.Keypad2,
        [KeyboardKey.KEY_KP_3] = ImGuiKey.Keypad3,
        [KeyboardKey.KEY_KP_4] = ImGuiKey.Keypad4,
        [KeyboardKey.KEY_KP_5] = ImGuiKey.Keypad5,
        [KeyboardKey.KEY_KP_6] = ImGuiKey.Keypad6,
        [KeyboardKey.KEY_KP_7] = ImGuiKey.Keypad7,
        [KeyboardKey.KEY_KP_8] = ImGuiKey.Keypad8,
        [KeyboardKey.KEY_KP_9] = ImGuiKey.Keypad9,
        [KeyboardKey.KEY_KP_DECIMAL] = ImGuiKey.KeypadDecimal,
        [KeyboardKey.KEY_KP_DIVIDE] = ImGuiKey.KeypadDivide,
        [KeyboardKey.KEY_KP_MULTIPLY] = ImGuiKey.KeypadMultiply,
        [KeyboardKey.KEY_KP_SUBTRACT] = ImGuiKey.KeypadSubtract,
        [KeyboardKey.KEY_KP_ADD] = ImGuiKey.KeypadAdd,
        [KeyboardKey.KEY_KP_ENTER] = ImGuiKey.KeypadEnter,
        [KeyboardKey.KEY_KP_EQUAL] = ImGuiKey.KeypadEqual,
    };
}