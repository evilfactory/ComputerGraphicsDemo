using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.GLFW;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ComputerGraphicsDemo;

static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Color;
    }

    private static IWindow window;
    private static GL gl;

    static uint vbo;
    static uint vao;
    static uint shader;

    static List<Vertex> verts = new List<Vertex>();

    static string fragShaderSrc = """
        #version 330 core
        out vec4 FragColor;
        in vec3 Color;

        void main()
        {
            FragColor = vec4(1.0f, 1.0f, 1.0f, 1.0f) * vec4(Color, 1.0f);
        } 
    """;

    static string vertShaderSrc = """
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;
    
        out vec3 Color;
    
        void main()
        {
           gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
           Color = aColor;
        }
    """;

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Demo";
        options.Size = new Vector2D<int>(800, 600);
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Debug, new APIVersion(4, 1));
        window = Window.Create(options);

        window.Load += Window_Load;
        window.Update += Window_Update;
        window.Render += Window_Render;

        window.Run();
    }

    private static void Window_Load()
    {
        gl = GL.GetApi(window);

        uint vertexShader;
        vertexShader = gl.CreateShader(GLEnum.VertexShader);
        uint fragShader;
        fragShader = gl.CreateShader(GLEnum.FragmentShader);

        gl.ShaderSource(vertexShader, vertShaderSrc);
        gl.CompileShader(vertexShader);
        gl.ShaderSource(fragShader, fragShaderSrc);
        gl.CompileShader(fragShader);

        shader = gl.CreateProgram();
        gl.AttachShader(shader, vertexShader);
        gl.AttachShader(shader, fragShader);
        gl.LinkProgram(shader);

        verts.Add(new Vertex { Position = new Vector3(0, 0, 0), Color = new Vector3(1f, 1f, 0f) });
        verts.Add(new Vertex { Position = new Vector3(1, 0, 0), Color = new Vector3(1f, 1f, 0f) });
        verts.Add(new Vertex { Position = new Vector3(0, 1, 0), Color = new Vector3(1f, 1f, 0f) });


        for (int x = 0; x < 500; x++)
        {
            for (int z = 0; z < 500; z++)
            {
                verts.Add(new Vertex() { Position = new Vector3(x / 500f, z / 500f, 0f), Color = new Vector3(0f, 1f, 1f) });
            }
        }

        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (Vertex* buffer = verts.ToArray())
            {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)(sizeof(Vertex) * verts.Count), (void*)buffer, BufferUsageARB.StaticDraw);
            }
        }

        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 4 * 6, 0);
        gl.EnableVertexArrayAttrib(vao, 0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 4 * 6, 4 * 3);
        gl.EnableVertexArrayAttrib(vao, 1);
    }

    private static void Window_Render(double delta)
    {
        gl.ClearColor(Color.Red);
        gl.Clear(ClearBufferMask.ColorBufferBit);

        gl.UseProgram(shader);
        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        gl.DrawArrays(GLEnum.Points, 0, (uint)verts.Count);
    }

    private static void Window_Update(double delta)
    {

    }
}
