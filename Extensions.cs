using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Raylib_CsLo;
using Color = System.Drawing.Color;

namespace Raylib_ImGui; 

/// <summary>
/// Random extensions I thought are useful
/// </summary>
public static class Extensions {
    /// <summary>
    /// Returns embedded resource bytes from an assembly
    /// </summary>
    /// <param name="assembly">Assembly</param>
    /// <param name="name">Resource Name</param>
    /// <returns>Resource Bytes</returns>
    public static byte[] GetEmbeddedResource(this Assembly assembly, string name) {
        using var s = assembly.GetManifestResourceStream(name);
        var ret = new byte[s!.Length];
        s.Read(ret, 0, (int)s.Length);
        return ret;
    }
    
    /// <summary>
    /// Loads byte buffer as a Raylib texture
    /// </summary>
    /// <param name="buffer">Byte Buffer</param>
    /// <param name="format">Extension</param>
    /// <returns>Loaded texture</returns>
    public static unsafe Texture LoadAsTexture(this byte[] buffer, string format) {
        fixed (byte* ptr = buffer) {
            var image = Raylib.LoadImageFromMemory(format, ptr, buffer.Length);
            var texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image); return texture;
        }
    }
    
    /// <summary>
    /// Loads byte buffer as a Raylib image
    /// </summary>
    /// <param name="buffer">Byte Buffer</param>
    /// <param name="format">Extension</param>
    /// <returns>Loaded image</returns>
    public static unsafe Image LoadAsImage(this byte[] buffer, string format) {
        fixed (byte* ptr = buffer) return Raylib.LoadImageFromMemory(format, ptr, buffer.Length);
    }

    /// <summary>
    /// Creates an ImGui binding from a Raylib texture
    /// </summary>
    /// <param name="texture">Texture</param>
    /// <returns>ImGui binding</returns>
    public static IntPtr CreateBinding(this Texture texture)
        => GCHandle.Alloc(texture, GCHandleType.Pinned).AddrOfPinnedObject();

    /// <summary>
    /// Packs color into a Vector4
    /// </summary>
    /// <param name="color">Color</param>
    /// <returns>Vector4</returns>
    public static Vector4 Pack(this Color color)
        => new(color.R, color.G, color.B, color.A);
}