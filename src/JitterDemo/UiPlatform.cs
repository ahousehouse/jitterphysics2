using Vellum;
using JitterDemo.Renderer.OpenGL;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo;

public class UiPlatform : IUiPlatform
{
    private GLFWWindow window;

    public UiPlatform(GLFWWindow window)
    {
        this.window = window;
    }
    
    public string GetClipboardText()
    {
        return window.GetClipboardString();
    }

    public void SetClipboardText(string text)
    {
        throw new System.NotImplementedException();
    }

    public void SetCursor(UiCursor cursor)
    {
        //throw new System.NotImplementedException();
    }
}
