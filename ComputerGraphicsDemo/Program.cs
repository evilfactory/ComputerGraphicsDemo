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
    static uint ebo;
    static uint vao;
    static uint shader;

    static List<Vertex> verts = new List<Vertex>();
    static List<uint> indices = new List<uint>();

    static string fragShaderSrc = """
        #version 330 core
        out vec4 FragColor;
        in vec3 Color;
        in float Height;

        void main()
        {
            vec3 color = Color;
            if (Height < 0.0)
            {
                color = vec3(0.0, 0.3, 0.6);
            }
            else if (Height < 0.05)
            {
                color = vec3(0.76, 0.70, 0.50);
            }
            else if (Height < 0.3)
            {
                color = vec3(0.1, 0.6, 0.1);
            }
            else if (Height < 0.6)
            {
                color = vec3(0.5, 0.5, 0.5);
            }
            else
            {
                color = vec3(1.0, 1.0, 1.0);
            }

            FragColor = vec4(1.0f, 1.0f, 1.0f, 1.0f) * vec4(color, 1.0f);
        }
    """;

    static string vertShaderSrc = """
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;
        layout (location = 2) in vec3 aNormal;
    
        uniform float uTime;
        uniform mat4 uView;
        uniform mat4 uProj;

        out vec3 Color;
        out vec3 Normal; 
        out float Height;
    
        void main()
        {
            gl_Position = uProj * uView * vec4(aPos, 1.0);
            Color = aColor;
            Height = aPos.y;
            Normal = aNormal;
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

    private static void CheckShaderCompileStatus(uint shader)
    {
        gl.GetShader(shader, GLEnum.CompileStatus, out int status);
        if (status == 0)
        {
            string infoLog = gl.GetShaderInfoLog(shader);
            Console.WriteLine($"Shader compilation failed:\n{infoLog}");
        }
        else
        {
            Console.WriteLine("Shader compiled successfully.");
        }
    }

    private static void Window_Load()
    {
        gl = GL.GetApi(window);

        gl.Enable(GLEnum.DepthTest);

        uint vertexShader;
        vertexShader = gl.CreateShader(GLEnum.VertexShader);
        uint fragShader;
        fragShader = gl.CreateShader(GLEnum.FragmentShader);

        gl.ShaderSource(vertexShader, vertShaderSrc);
        gl.CompileShader(vertexShader);
        gl.ShaderSource(fragShader, fragShaderSrc);
        gl.CompileShader(fragShader);

        CheckShaderCompileStatus(vertexShader);
        CheckShaderCompileStatus(fragShader);

        shader = gl.CreateProgram();
        gl.AttachShader(shader, vertexShader);
        gl.AttachShader(shader, fragShader);
        gl.LinkProgram(shader);

        int size = 1000;
        int vertsPerRow = size + 1;

        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.01f);

        for (int z = 0; z < vertsPerRow; z++)
        {
            for (int x = 0; x < vertsPerRow; x++)
            {
                float fx = ((float)x / size) * 25 - 10f;
                float fz = ((float)z / size) * 25 - 22f;

                float y = noise.GetNoise(x, z);

                Vector3 color = new Vector3(1, 1, 1);

                verts.Add(new Vertex
                {
                    Position = new Vector3(fx, y / 1.2f, fz),
                    Color = color
                });
            }
        }

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int topLeft = z * vertsPerRow + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * vertsPerRow + x;
                int bottomRight = bottomLeft + 1;

                indices.Add((uint)topLeft);
                indices.Add((uint)bottomLeft);
                indices.Add((uint)topRight);

                indices.Add((uint)topRight);
                indices.Add((uint)bottomLeft);
                indices.Add((uint)bottomRight);
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

        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        unsafe
        {
            fixed (uint* indexData = indices.ToArray())
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * indices.Count), indexData, BufferUsageARB.StaticDraw);
            }
        }

        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 4 * 6, 0);
        gl.EnableVertexArrayAttrib(vao, 0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 4 * 6, 4 * 3);
        gl.EnableVertexArrayAttrib(vao, 1);

    }

    private static double time = 0;
    private static void Window_Render(double delta)
    {
        time += delta;

        gl.Viewport(window.Size);

        gl.ClearColor(Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        gl.UseProgram(shader);

        int timeLocation = gl.GetUniformLocation(shader, "uTime");
        int viewLocation = gl.GetUniformLocation(shader, "uView");
        int projLocation = gl.GetUniformLocation(shader, "uProj");

        gl.Uniform1(timeLocation, (float)time);

        var center = new Vector3(5f, 0f, -5f);
        float eyeX = center.X + 2.5f * MathF.Sin((float)time);
        float eyeZ = center.Y + 2.5f * MathF.Cos((float)time);
        var eye = new Vector3(eyeX, 1.5f, eyeZ);
        var target = center;
        var up = Vector3.UnitY;

        var view = Matrix4x4.CreateLookAt(eye, target, up);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 3f,
            (float)window.Size.X / (float)window.Size.Y,
            0.1f,
            100f
        );

        unsafe
        {
            gl.UniformMatrix4(viewLocation, 1, false, (float*)&view);
            gl.UniformMatrix4(projLocation, 1, false, (float*)&proj);
        }

        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        unsafe
        {
            gl.DrawElements(GLEnum.Triangles, (uint)indices.Count, GLEnum.UnsignedInt, null);
        }
    }

    private static void Window_Update(double delta)
    {

    }
}
