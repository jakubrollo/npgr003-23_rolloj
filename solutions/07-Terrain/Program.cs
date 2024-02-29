using System;
using System.Diagnostics;
using CommandLine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using Util;
using System.Numerics;
using Silk.NET.Vulkan;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace _07_Terrain;

using Vector3 = Vector3D<float>;
using Matrix4 = Matrix4X4<float>;

public class Options
{
    [Option('w', "width", Required = false, Default = 800, HelpText = "Window width in pixels.")]
    public int WindowWidth { get; set; } = 800;

    [Option('h', "height", Required = false, Default = 600, HelpText = "Window height in pixels.")]
    public int WindowHeight { get; set; } = 600;

    [Option('t', "texture", Required = false, Default = ":check:", HelpText = "User-defined texture.")]
    public string TextureFile { get; set; } = ":check:";
}

/// <summary>
/// Single object = sequence of primitives of the same type (point, line, triangle).
/// </summary>
class Object
{
    public Object() => Reset();

    /// <summary>
    /// Buffer id. Not used here as we have only one shared buffer.
    /// </summary>
    public uint BufferId { get; set; }

    public BufferObject<float>? Vbo_Terrain { get; set; }

    public PrimitiveType Type { get; set; } = PrimitiveType.Triangles;

    /// <summary>
    /// Start of the object in the index buffer (in indices).
    /// </summary>
    public int BufferOffset { get; set; }

    /// <summary>
    /// Number of indices (should be multiple of two for lines, multiple of three for triangles, multiple of four for quads).
    /// </summary>
    public int Indices { get; set; }

    /// <summary>
    /// World space coordinates of the object's center.
    /// </summary>
    public Vector3 Center { get; private set; }

    /// <summary>
    /// Object-to-world (local-to-world) transformation.
    /// </summary>
    public Matrix4 ModelTransform { get; private set; }

    public void Reset()
    {
        Center = Vector3.Zero;
        ModelTransform = Matrix4.Identity;
    }

    public void Translate(Vector3 t)
    {
        Center += t;
        Matrix4 translation = Matrix4X4.CreateTranslation(t);
        ModelTransform *= translation;
    }

    /// <summary>
    /// Rotate the object around its center.
    /// </summary>
    /// <param name="angle">Angle in radians.</param>
    public void Rotate(float angle)
    {
        Matrix4 rotation = Matrix4X4.CreateFromYawPitchRoll(angle, -0.414141414f * angle, 0.3333333f * angle);
        ModelTransform *= Matrix4X4.CreateTranslation(-Center) * rotation * Matrix4X4.CreateTranslation(Center);
    }
}

internal class Program
{
    // Terrain Settings
    private static int gridSize = 257; // Initial grid size (adjust as needed)
    private static float roughness = 45f; // Roughness coefficient for Diamond-Square algorithm
    private static float minHeight = -30.0f; // Minimum height for the terrain
    private static float maxHeight = 60.0f; // Maximum height for the terrain
    private static float[,] terrainData = new float[0, 0];

    // Scene Dimensions
    private static float spacing = 0.5f;
    private static float centerPoint = (gridSize * spacing) / 2;
    private static Vector3 sceneCenter = new(centerPoint, 10, centerPoint);
    private static float sceneDiameter = gridSize * spacing;

    // Hovercraft
    private static List<uint> hovercraftIndexBuffer = new(24);
    private static List<float> hovercraftVertexBuffer = new(9 * VERTEX_SIZE);
    private static float currentHoverHeight = 10f;
    private static float hovercraftSpeed = 30f;
    static Hovercraft hovercraft = new(hoverHeight: currentHoverHeight, inertia: 0.95f, gravity: 9.8f, posX: centerPoint, posY: centerPoint); // parametrize
    private static System.Numerics.Vector3 acceleration = new();

    // Camera
    private static CameraState cameraState = CameraState.NormalTrackBall;
    enum CameraState
    {
        NormalTrackBall,
        ThirdPerson,
        FirstPerson
    }

    // Trackball
    private static Trackball? tb;

    // Window Settings
    private static IWindow? window;
    private static GL? Gl;
    private static float width;
    private static float height;

    // Locking
    private static object renderLock = new();

    // FPS Counter
    private static FPS fps = new();

    // Key States
    private static bool wDown = false;
    private static bool sDown = false;
    private static bool aDown = false;
    private static bool dDown = false;

    // 3D Data Buffer
    private const int MAX_INDICES = 524288; // Divisible by 64
    private const int MAX_VERTICES = 131072; // Divisible by 64
    private const int VERTEX_SIZE = 11;
    private static List<uint> indexBuffer = new(MAX_INDICES);
    private static List<float> vertexBuffer = new(MAX_VERTICES * VERTEX_SIZE);

    private static BufferObject<float>? Vbo_Terrain;
    private static BufferObject<uint>? Ebo_Terrain;
    private static VertexArrayObject<float, uint>? Vao_Terrain;

    private static BufferObject<float>? Vbo_Hovercraft;
    private static BufferObject<uint>? Ebo_Hovercraft;
    private static VertexArrayObject<float, uint>? Vao_Hovercraft;


    // Objects
    private static List<Object> Objects = new();
    private static Object FirstObject => Objects[0];
    private static Object LastObject => Objects[^1];

    // Vertex to Triangle Mapping
    private static Dictionary<int, Dictionary<uint, List<int>>> vertexToTriangle = new();
        // Index buffer for each level of recursion (less and less triangles)
    private static Dictionary<int, List<uint>> indexBuffers = new();
    private static Dictionary<int, List<System.Numerics.Vector3>> recursionToTriangleNormals = new();

    // Texture
    private static Util.Texture? texture;
    private static bool useTexture = false;
    private static string textureFile = ":check:";
    private const int TEX_SIZE = 128;

    // Recursion
    private static int maxRecursion = 8;
    private static int currentLevelOfRecursion = maxRecursion;

    // Input Context
    private static IInputContext? input;

    // Lighting
    private static bool usePhong = false;

    // Shader Program
    private static ShaderProgram? ShaderPrg;
    private static void PreGenerateIndexBuffers()
    {
        for (int level = 8; level >= 0; level--)
        {
            recursionToTriangleNormals[level] = new List<System.Numerics.Vector3>();
            vertexToTriangle[level] = new Dictionary<uint, List<int>>();
            CreateIndexBuffer(level);
        }
    }
    private static void CreateIndexBuffer(int recursionLevel)
    {
        List<uint> indexBuffer = new();

        int numVerticesToSkip = (int)Math.Pow(2, 8 - recursionLevel);
        int numVerticesOnLine = (int)Math.Pow(2, recursionLevel) + 1;

        int triangleNumber = 0;

        for (uint num = 0; num < Math.Pow(Math.Pow(2, currentLevelOfRecursion + 1), 2); num++)
        {
            vertexToTriangle[recursionLevel][num] = new List<int>();
        }

        for (int z = 0; z < numVerticesOnLine - 1; z++)
        {
            for (int x = 0; x < numVerticesOnLine - 1; x++)
            {
                // Calculate the indices of the vertices for the current quad
                uint topLeft = (uint)(z * numVerticesToSkip * gridSize + x * numVerticesToSkip);
                uint topRight = (uint)(z * numVerticesToSkip * gridSize + (x + 1) * numVerticesToSkip);
                uint bottomLeft = (uint)(((z + 1) * numVerticesToSkip) * gridSize + x * numVerticesToSkip);
                uint bottomRight = (uint)(((z + 1) * numVerticesToSkip) * gridSize + ((x + 1) * numVerticesToSkip));

                // Define the indices of the vertices for the two triangles forming the quad
                indexBuffer.Add(topLeft);
                indexBuffer.Add(bottomLeft);
                indexBuffer.Add(topRight);

                vertexToTriangle[recursionLevel][topLeft].Add(triangleNumber);
                vertexToTriangle[recursionLevel][bottomLeft].Add(triangleNumber);
                vertexToTriangle[recursionLevel][topRight].Add(triangleNumber);
                triangleNumber++;

                indexBuffer.Add(topRight);
                indexBuffer.Add(bottomLeft);
                indexBuffer.Add(bottomRight);
                vertexToTriangle[recursionLevel][topRight].Add(triangleNumber);
                vertexToTriangle[recursionLevel][bottomLeft].Add(triangleNumber);
                vertexToTriangle[recursionLevel][bottomRight].Add(triangleNumber);
                triangleNumber++;
            }
        }

        indexBuffers[recursionLevel] = indexBuffer;
    }
    private static void InitializeTerrainMesh()
    {
        // Generate terrain vertices using the Diamond-Square algorithm
        terrainData = TerrainGeneration.DiamondSquare.Generate(gridSize, roughness, minHeight, maxHeight);

        // Convert terrain data to vertex buffer format
        ConvertTerrainDataToVertexBuffer();
    }

    private static void RecomputeNormals()
    {
        recursionToTriangleNormals[currentLevelOfRecursion].Clear();
        for (int j = 0; j < indexBuffer.Count; j += 3)
        {
            // Get the vertices of the triangle
            System.Numerics.Vector3 v0 = new(vertexBuffer[(int)indexBuffer[j] * VERTEX_SIZE], vertexBuffer[(int)indexBuffer[j] * VERTEX_SIZE + 1], vertexBuffer[(int)indexBuffer[j] * VERTEX_SIZE + 2]);
            System.Numerics.Vector3 v1 = new(vertexBuffer[(int)indexBuffer[j + 1] * VERTEX_SIZE], vertexBuffer[(int)indexBuffer[j + 1] * VERTEX_SIZE + 1], vertexBuffer[(int)indexBuffer[j + 1] * VERTEX_SIZE + 2]);
            System.Numerics.Vector3 v2 = new(vertexBuffer[(int)indexBuffer[j + 2] * VERTEX_SIZE], vertexBuffer[(int)indexBuffer[j + 2] * VERTEX_SIZE + 1], vertexBuffer[(int)indexBuffer[j + 2] * VERTEX_SIZE + 2]);

            // Compute the triangle's normal
            System.Numerics.Vector3 edge1 = v1 - v0;
            System.Numerics.Vector3 edge2 = v2 - v0;
            System.Numerics.Vector3 triangleNormal = System.Numerics.Vector3.Cross(edge1, edge2);

            recursionToTriangleNormals[currentLevelOfRecursion].Add(triangleNormal);
        }

        // Iterate over each vertex from the start index
        int numberOfVerticesUsed = (int)Math.Pow((Math.Sqrt((indexBuffer.Count / 3) / 2)) + 1, 2);
        int[] verticesUsed = new int[numberOfVerticesUsed];
        int chunkSize = (gridSize - 1) / (int)(Math.Pow(2, currentLevelOfRecursion));
        int size = 0;
        for (int y = 0; y < gridSize; y += chunkSize)
        {
            for (int x = 0; x < gridSize; x += chunkSize)
            {
                verticesUsed[size] = x + y * gridSize;
                size++;
            }
        }

        foreach (int vertex in verticesUsed)
        {
            System.Numerics.Vector3 normal = System.Numerics.Vector3.Zero;

            foreach (var triangle in vertexToTriangle[currentLevelOfRecursion][(uint)vertex])
            {
                normal += recursionToTriangleNormals[currentLevelOfRecursion][triangle];
            }

            // Normalize the sum of normals to get the averaged normal
            normal = System.Numerics.Vector3.Normalize(normal);

            // Update the normal in the vertex buffer
            vertexBuffer[vertex * VERTEX_SIZE + 6] = normal.X;
            vertexBuffer[vertex * VERTEX_SIZE + 7] = normal.Y;
            vertexBuffer[vertex * VERTEX_SIZE + 8] = normal.Z;
        }
    }
    private static void ConvertTerrainDataToVertexBuffer()
    {
        // Convert terrain data to vertex buffer format and populate the vertex buffer
        for (int y = 0; y < terrainData.GetLength(1); y++)
        {
            for (int x = 0; x < terrainData.GetLength(0); x++)
            {
                float height = terrainData[x, y];
                // Calculate vertex coordinates based on grid position and height
                float xPos = x * spacing; // Adjust spacing as needed
                float yPos = height;
                float zPos = y * spacing; // Adjust spacing as needed

                // Add vertex data to the vertex buffer
                vertexBuffer.Add(xPos);
                vertexBuffer.Add(yPos);
                vertexBuffer.Add(zPos);

                // Add color data, normal data, texture coordinates, etc., as needed
                // Example: Add color data (RGB values)
                float[] color = CalculateColor(height);
                vertexBuffer.Add(color[0]); // Red component
                vertexBuffer.Add(color[1]); // Green component
                vertexBuffer.Add(color[2]); // Blue component

                // Example: Add normal data (assuming flat terrain, so the normal is the up direction)
                vertexBuffer.Add(0); // Normal X component
                vertexBuffer.Add(0); // Normal Y component
                vertexBuffer.Add(0); // Normal Z component

                float s = x % 2 == 0 ? 0.0f : 1.0f; // Alternates between 0 and 1
                float t = y % 2 == 0 ? 0.0f : 1.0f; // Alternates between 0 and 1

                // Example: Add texture coordinates (assuming no texture mapping)
                vertexBuffer.Add(s); // Texture coordinate S
                vertexBuffer.Add(t); // Texture coordinate T
            }
        }
    }

    // Function to calculate color based on height
    private static float[] CalculateColor(float height)
    {
        Random random = new();
        float grayLimit = 30 - random.Next(0, 1);
        if (height < 0)
        {
            // Below sea level - interpolate from light blue to dark blue
            float t = height / -10.0f; // Assuming sea level at -100
            return InterpolateColors(new float[] { 0.7f, 0.8f, 1.0f }, new float[] { 0.0f, 0.1f, 0.3f }, t);
        }
        else if (height < 10)
        {
            // Sandy beaches - interpolate from light brown to light green
            float t = height / 10.0f;
            return InterpolateColors(new float[] { 0.8f, 0.7f, 0.5f }, new float[] { 0.1f, 0.6f, 0.1f }, t);
        }
        else if (height > grayLimit && height < 55)
        {
            float t = (height - 10) / 90.0f;  // Assuming height ranges from 10 to 100 for grass
            return InterpolateColors(new float[] { t, t, t }, new float[] { 1.0f, 1.0f, 1.0f }, t);
        }
        else if (height < 55)
        {
            // Green grass - interpolate from light green to dark green
            float t = (height - 10) / 20.0f;
            return InterpolateColors(new float[] { 0.1f, 0.6f, 0.1f }, new float[] { 0.0f, 0.4f, 0.0f }, t);
        }
        else
        {
            // Snowy peaks - constant white color
            return new float[] { 1.0f, 1.0f, 1.0f };
        }
    }

    // Function to interpolate between two colors
    private static float[] InterpolateColors(float[] color1, float[] color2, float t)
    {
        float r = color1[0] + (color2[0] - color1[0]) * t;
        float g = color1[1] + (color2[1] - color1[1]) * t;
        float b = color1[2] + (color2[2] - color1[2]) * t;
        return new float[] { r, g, b };
    }

    //////////////////////////////////////////////////////
    // Application.

    private static string WindowTitle()
    {
        StringBuilder sb = new("07-Terrain");

        sb.Append(string.Format(CultureInfo.InvariantCulture, ", fps={0:f1}", fps.Fps));
        if (window != null &&
            window.VSync)
            sb.Append(" [VSync]");

        double pps = fps.Pps;
        if (pps > 0.0)
            if (pps < 5.0e5)
                sb.Append(string.Format(CultureInfo.InvariantCulture, ", pps={0:f1}k", pps * 1.0e-3));
            else
                sb.Append(string.Format(CultureInfo.InvariantCulture, ", pps={0:f1}m", pps * 1.0e-6));

        if (tb != null)
        {
            sb.Append(tb.UsePerspective ? ", perspective" : ", orthographic");
            sb.Append(string.Format(CultureInfo.InvariantCulture, ", zoom={0:f2}", tb.Zoom));
        }

        if (useTexture &&
            texture != null &&
            texture.IsValid())
            sb.Append($", txt={texture.name}");
        else
            sb.Append(", no texture");

        if (usePhong)
            sb.Append(", Phong shading");

        return sb.ToString();
    }

    private static void SetWindowTitle()
    {
        if (window != null)
            window.Title = WindowTitle();
    }

    private static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
          .WithParsed<Options>(o =>
          {
              WindowOptions options = WindowOptions.Default;
              options.Size = new Vector2D<int>(o.WindowWidth, o.WindowHeight);
              options.Title = WindowTitle();
              options.PreferredDepthBufferBits = 24;
              options.VSync = true;
              PreGenerateIndexBuffers();

              window = Window.Create(options);
              width = o.WindowWidth;
              height = o.WindowHeight;

              window.Load += OnLoad;
              window.Render += OnRender;
              window.Update += OnUpdate;
              window.Closing += OnClose;
              window.Resize += OnResize;

              textureFile = o.TextureFile;

              window.Run();
          });
    }

    static void OnUpdate(double deltaTime)
    {
        // Simulate hovercraft movement
        if (cameraState != CameraState.NormalTrackBall)
        {
            UpdateAcceleration();
            hovercraft.Simulate(deltaTime, acceleration, terrainData, 0.5f);
            acceleration.X = 0;
            acceleration.Y = 0;
            acceleration.Z = 0;
            System.Numerics.Vector3 eyePos = hovercraft.Position;

            hovercraftVertexBuffer.Clear();
            hovercraftVertexBuffer.AddRange(new[]
            {
        //      x,              y,                  z,           R,     G,     B,     Nx,    Ny,    Nz,   s,    t
            eyePos.X-1.0f,  eyePos.Y,        eyePos.Z + 1.0f,   1.0f,  1.0f,  1.0f, -0.3f,  1.0f,  0.6f, 0.0f, 0.0f,  // 0
            eyePos.X,       eyePos.Y+0.1f,   eyePos.Z + 1.0f,   0.5f,  0.8f,  1.0f,  0.0f,  1.0f,  0.6f, 0.5f, 0.0f,  // 1
            eyePos.X+1.0f,  eyePos.Y,        eyePos.Z+ 1.0f,    0.0f,  0.6f,  1.0f,  0.3f,  1.0f,  0.6f, 1.0f, 0.0f,  // 2
            eyePos.X-1.0f,  eyePos.Y + 0.1f, eyePos.Z,          0.0f,  0.2f,  0.2f, -0.6f,  1.0f,  0.0f, 0.0f, 0.5f,  // 3
            eyePos.X,       eyePos.Y + 0.3f, eyePos.Z,          0.2f,  0.4f,  0.4f,  0.0f,  1.0f, -0.1f, 0.5f, 0.5f,  // 4
            eyePos.X+1.0f,  eyePos.Y + 0.2f, eyePos.Z,          0.9f,  0.6f,  0.6f,  0.6f,  1.0f,  0.0f, 1.0f, 0.5f,  // 5
            eyePos.X-1.0f,  eyePos.Y + 0.2f, eyePos.Z-1.0f,     1.0f,  0.0f,  0.2f, -0.4f,  1.0f, -0.5f, 0.0f, 1.0f,  // 6
            eyePos.X-0.0f,  eyePos.Y + 0.3f, eyePos.Z-1.0f,     0.7f,  0.5f,  0.6f,  0.0f,  1.0f, -0.5f, 0.5f, 1.0f,  // 7
            eyePos.X+1.0f,  eyePos.Y+ 0.2f,  eyePos.Z-1.0f,     0.4f,  1.0f,  0.0f,  0.4f,  1.0f, -0.5f, 1.0f, 1.0f,  // 8
        });

            var span = CollectionsMarshal.AsSpan(hovercraftVertexBuffer);

            Vbo_Hovercraft?.UpdateData(span, 0, hovercraftVertexBuffer.Count);

            Debug.Assert(tb != null);
            tb.ChangeCenter(new Vector3(eyePos.X, eyePos.Y, eyePos.Z));

        }
    }

    private static void OnLoad()
    {
        Debug.Assert(window != null);

        // Initialize all the inputs (keyboard + mouse).
        input = window.CreateInput();

        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += KeyDown;
            input.Keyboards[i].KeyUp += KeyUp;
        }
        for (int i = 0; i < input.Mice.Count; i++)
        {
            //** Maybe only in firstPerson mode
            //input.Mice[i].Cursor.CursorMode = CursorMode.Raw;

            input.Mice[i].MouseDown += MouseDown;
            input.Mice[i].MouseUp += MouseUp;
            input.Mice[i].MouseMove += MouseMove;
            input.Mice[i].DoubleClick += MouseDoubleClick;
            input.Mice[i].Scroll += MouseScroll;
        }
        var mouse = input.Mice[0];
        Vector2 mousePosition = mouse.Position;

        // OpenGL global reference (shortcut).
        Gl = GL.GetApi(window);
        Gl.ClearColor(Color.SkyBlue);

        //------------------------------------------------------
        // Render data.

        InitializeTerrainMesh();
        indexBuffer = indexBuffers[currentLevelOfRecursion];
        RecomputeNormals();

        hovercraftVertexBuffer.AddRange(new[]
        {
        //  x,     y,     z,     R,     G,     B,     Nx,    Ny,    Nz,   s,    t
          -1.0f,  0.0f,  1.0f,  1.0f,  1.0f,  1.0f, -0.3f,  1.0f,  0.6f, 0.0f, 0.0f,  // 0
           0.0f,  0.1f,  1.0f,  0.5f,  0.8f,  1.0f,  0.0f,  1.0f,  0.6f, 0.5f, 0.0f,  // 1
           1.0f,  0.0f,  1.0f,  0.0f,  0.6f,  1.0f,  0.3f,  1.0f,  0.6f, 1.0f, 0.0f,  // 2
          -1.0f,  0.1f,  0.0f,  0.0f,  0.2f,  0.2f, -0.6f,  1.0f,  0.0f, 0.0f, 0.5f,  // 3
           0.0f,  0.3f,  0.0f,  0.2f,  0.4f,  0.4f,  0.0f,  1.0f, -0.1f, 0.5f, 0.5f,  // 4
           1.0f,  0.2f,  0.0f,  0.9f,  0.6f,  0.6f,  0.6f,  1.0f,  0.0f, 1.0f, 0.5f,  // 5
          -1.0f,  0.2f, -1.0f,  1.0f,  0.0f,  0.2f, -0.4f,  1.0f, -0.5f, 0.0f, 1.0f,  // 6
           0.0f,  0.3f, -1.0f,  0.7f,  0.5f,  0.6f,  0.0f,  1.0f, -0.5f, 0.5f, 1.0f,  // 7
           1.0f,  0.2f, -1.0f,  0.4f,  1.0f,  0.0f,  0.4f,  1.0f, -0.5f, 1.0f, 1.0f,  // 8
        });


        hovercraftIndexBuffer.AddRange(new uint[]
        {
              // 8 triangles
              0, 1, 4, 0, 4, 3,
              1, 2, 5, 1, 5, 4,
              3, 4, 7, 3, 7, 6,
              4, 5, 8, 4, 8, 7,
        });

        // Create terrain (objects[0])
        lock (renderLock)
        {
            Objects.Add(new()
            {
                BufferId = 0,
                Type = PrimitiveType.Triangles,
                BufferOffset = 0,
                Indices = MAX_INDICES
            });

            // Vertex Array Object = Vertex buffer + Index buffer.
            Ebo_Terrain = new BufferObject<uint>(Gl, indexBuffer.ToArray(), BufferTargetARB.ElementArrayBuffer);
            Vbo_Terrain = new BufferObject<float>(Gl, vertexBuffer.ToArray(), BufferTargetARB.ArrayBuffer);
            Vao_Terrain = new VertexArrayObject<float, uint>(Gl, Vbo_Terrain, Ebo_Terrain);
            VaoPointers();

            // Create hovercraft (objects[1])

            Objects.Add(new()
            {
                BufferId = 1,
                Type = PrimitiveType.Triangles,
                BufferOffset = 0,
                Indices = 24
            });

            Ebo_Hovercraft = new BufferObject<uint>(Gl, hovercraftIndexBuffer.ToArray(), BufferTargetARB.ElementArrayBuffer);
            Vbo_Hovercraft = new BufferObject<float>(Gl, hovercraftVertexBuffer.ToArray(), BufferTargetARB.ArrayBuffer);
            Vao_Hovercraft = new VertexArrayObject<float, uint>(Gl, Vbo_Hovercraft, Ebo_Hovercraft);
            Vao_Hovercraft.Bind();
            Vao_Hovercraft.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 0);
            Vao_Hovercraft.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 3);
            Vao_Hovercraft.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 6);
            Vao_Hovercraft.VertexAttributePointer(3, 2, VertexAttribPointerType.Float, VERTEX_SIZE, 9);


            // Initialize the shaders.
            ShaderPrg = new ShaderProgram(Gl, "shader.vert", "shader.frag");

            // Initialize the texture.
            if (textureFile.StartsWith(":"))
            {
                // Generated texture.
                texture = new(TEX_SIZE, TEX_SIZE, textureFile);
                texture.GenerateTexture(Gl);
            }
            else
            {
                texture = new(textureFile, textureFile);
                texture.OpenglTextureFromFile(Gl);
            }

            // Trackball.
            tb = new(sceneCenter, sceneDiameter);
        }

        // Main window.
        SetWindowTitle();
        SetupViewport();

    }

    /// <summary>
    /// Mouse horizontal scaling coefficient.
    /// One unit/pixel of mouse movement corresponds to this distance in world space.
    /// </summary>
    private static float mouseCx = 1;

    /// <summary>
    /// Mouse vertical scaling coefficient.
    /// Vertical scaling is just negative value of horizontal one.
    /// </summary>
    private static float mouseCy = -1;

    /// <summary>
    /// Does all necessary steps after window setup/resize.
    /// Assumes valid values in 'width' and 'height'.
    /// </summary>
    private static void SetupViewport()
    {
        // OpenGL viewport.
        Gl?.Viewport(0, 0, (uint)width, (uint)height);

        tb?.ViewportChange((int)width, (int)height, 0.05f, 20.0f);
    }

    /// <summary>
    /// Called after window resize.
    /// </summary>
    /// <param name="newSize">New window size in pixels.</param>
    private static void OnResize(Vector2D<int> newSize)
    {
        width = newSize[0];
        height = newSize[1];
        SetupViewport();
    }

    private static void VaoPointers()
    {

        Debug.Assert(Vao_Terrain != null);
        Vao_Terrain.Bind();
        Vao_Terrain.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 0);
        Vao_Terrain.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 3);
        Vao_Terrain.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, VERTEX_SIZE, 6);
        Vao_Terrain.VertexAttributePointer(3, 2, VertexAttribPointerType.Float, VERTEX_SIZE, 9);
    }
    /// <summary>
    /// Called every time the content of the window should be redrawn.
    /// </summary>
    /// <param name="obj"></param>
    private static unsafe void OnRender(double obj)
    {
        Debug.Assert(Gl != null);
        Debug.Assert(ShaderPrg != null);
        Debug.Assert(tb != null);

        Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        lock (renderLock)
        {
            // Rendering properties (set in every frame for clarity).
            Gl.Enable(GLEnum.DepthTest);
            Gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
            Gl.Disable(GLEnum.CullFace);

            VaoPointers();
            ShaderPrg.Use();

            ShaderPrg.TrySetUniform("view", tb.View);
            ShaderPrg.TrySetUniform("projection", tb.Projection);

            ShaderPrg.TrySetUniform("eyePosition", tb.Eye);

            // Shared shader uniforms - Phong shading.
            ShaderPrg.TrySetUniform("lightColor", 1.0f, 1.0f, 1.0f);
            ShaderPrg.TrySetUniform("lightPosition", 100.0f, 110.0f, 120.0f);
            ShaderPrg.TrySetUniform("Ka", 0.6f);
            ShaderPrg.TrySetUniform("Kd", 0.3f);
            ShaderPrg.TrySetUniform("Ks", 0.1f);
            ShaderPrg.TrySetUniform("shininess", 50.0f);
            ShaderPrg.TrySetUniform("usePhong", usePhong);

            // Shared shader uniforms - Texture.
            if (texture == null || !texture.IsValid())
                useTexture = false;
            ShaderPrg.TrySetUniform("useTexture", useTexture);
            ShaderPrg.TrySetUniform("tex", 0);
            if (useTexture)
                texture?.Bind(Gl);

            // Draw the objects.
            foreach (var o in Objects)
            {

                // Draw terrain
                if (o.BufferId == 0) {
                    Debug.Assert(Vao_Terrain != null);
                    Vao_Terrain.Bind();
                    // Object-specific uniforms.
                    ShaderPrg.TrySetUniform("model", o.ModelTransform);

                    // Draw the batch.
                    Gl.DrawElements(o.Type, (uint)o.Indices, DrawElementsType.UnsignedInt, (void*)(o.BufferOffset * sizeof(float)));

                    // Update Pps.
                    fps.AddPrimitives(o.Indices / 3);
                }

                // Draw hovercraft
                else if (o.BufferId == 1 && cameraState == CameraState.ThirdPerson)
                {
                    Gl.BindTexture(TextureTarget.Texture2D, 0);
                    Debug.Assert(Vao_Hovercraft != null);
                    Vao_Hovercraft.Bind();
                    // Object-specific uniforms.
                    ShaderPrg.TrySetUniform("model", o.ModelTransform);

                    // Draw the batch.
                    Gl.DrawElements(o.Type, (uint)o.Indices, DrawElementsType.UnsignedInt, (void*)(o.BufferOffset * sizeof(float)));

                    // Update Pps.
                    fps.AddPrimitives(o.Indices / 3);
                }
            }
        }

        // Cleanup.
        Gl.UseProgram(0);

        // FPS.
        if (fps.AddFrames())
            SetWindowTitle();
    }

    private static unsafe void UpdateNormals()
    {
        lock (renderLock)
        {
            RecomputeNormals();
            var span = CollectionsMarshal.AsSpan(vertexBuffer);
            Vbo_Terrain?.UpdateData(span, 0, vertexBuffer.Count);

            Ebo_Terrain?.Dispose();

            Debug.Assert(Gl != null);
            Ebo_Terrain = new BufferObject<uint>(Gl, indexBuffer.ToArray(), BufferTargetARB.ElementArrayBuffer);
            Objects[0].Indices = indexBuffer.Count;
        }
    }

    private static void HideCursor()
    {
        for (int i = 0; i < input?.Mice.Count; i++)
        {
            //** Maybe only in firstPerson mode
            input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
        }
    }

    private static void ShowCursor()
    {
        for (int i = 0; i < input?.Mice.Count; i++)
        {
            //** Maybe only in firstPerson mode
            input.Mice[i].Cursor.CursorMode = CursorMode.Normal;
        }
    }

    /// <summary>
    /// Handler for window close event.
    /// </summary>
    private static void OnClose()
    {
        Vao_Terrain?.Dispose();
        ShaderPrg?.Dispose();

        // Remember to dispose the textures.
        texture?.Dispose();
    }

    /// <summary>
    /// Shift counter (0 = no shift pressed).
    /// </summary>
    private static int shiftDown = 0;

    /// <summary>
    /// Ctrl counter (0 = no ctrl pressed).
    /// </summary>
    private static int ctrlDown = 0;

    /// <summary>
    /// Handler function for keyboard key up.
    /// </summary>
    /// <param name="arg1">Keyboard object.</param>
    /// <param name="arg2">Key identification.</param>
    /// <param name="arg3">Key scancode.</param>

    private static void SwitchCameraMode()
    {
        Debug.Assert(tb != null);
        switch (cameraState)
        {
            case CameraState.NormalTrackBall:
                FirstObject.Reset();
                hovercraftSpeed = 30f;
                hovercraft.risingSpeedCoefficient = 5;
                hovercraft.fallingSpeedCoefficient = 2;
                cameraState = CameraState.ThirdPerson;
                hovercraft.ChangeHeight(terrainData[(int)(centerPoint / spacing), (int)(centerPoint / spacing)] + 5);
                break;

            case CameraState.ThirdPerson:
                hovercraftSpeed = 10f;
                hovercraft.risingSpeedCoefficient = 2;
                hovercraft.fallingSpeedCoefficient = 2;
                cameraState = CameraState.FirstPerson;
                HideCursor();
                tb.isFirstPerson = true;
                break;

            case CameraState.FirstPerson:
                cameraState = CameraState.NormalTrackBall;
                tb.isFirstPerson = false;
                ShowCursor();
                tb.ChangeCenter(sceneCenter);
                break;
        }
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        if (tb != null &&
            tb.KeyDown(arg1, arg2, arg3))
        {
            SetWindowTitle();
            //return;
        }

        switch (arg2)
        {
            case Key.ShiftLeft:
            case Key.ShiftRight:
                shiftDown++;
                break;

            case Key.ControlLeft:
            case Key.ControlRight:
                ctrlDown++;
                break;

            case Key.Home:
                // Reset object transformation.
                if (Objects.Count > 0)
                {
                    LastObject.Reset();
                }
                break;

            // Example: WSAD keys control forward, backward, left, and right movement
            // Handle WASD keys
            case Key.W:
                wDown = true;
                break;
            case Key.S:
                sDown = true;
                break;
            case Key.A:
                aDown = true;
                break;
            case Key.D:
                dDown = true;
                break;
            case Key.Q:
                if(cameraState != CameraState.NormalTrackBall && currentHoverHeight-0.5f > 0)
                {
                    currentHoverHeight -= 0.5f;
                    Ut.MessageInvariant($"Hover height: {currentHoverHeight}");
                    hovercraft.ChangeHoverHeight(currentHoverHeight);
                }
                break;
            case Key.E:
                if (cameraState != CameraState.NormalTrackBall)
                {
                    currentHoverHeight += 0.5f;
                    Ut.MessageInvariant($"Hover height: {currentHoverHeight}");
                    hovercraft.ChangeHoverHeight(currentHoverHeight);
                }
                break;

            case Key.T:
                // Toggle texture.
                useTexture = !useTexture;
                if (useTexture)
                    Ut.Message($"Texture: {texture?.name}");
                else
                    Ut.Message("Texturing off");
                SetWindowTitle();
                break;

            case Key.H:
                SwitchCameraMode();
                break;

            case Key.I:
                // Toggle Phong shading.
                usePhong = !usePhong;
                Ut.Message("Phong shading: " + (usePhong ? "on" : "off"));
                SetWindowTitle();
                break;

            case Key.P:
                // Perspective <-> orthographic.
                if (tb != null)
                {
                    tb.UsePerspective = !tb.UsePerspective;
                    SetWindowTitle();
                }
                break;

            case Key.C:
                // Reset view.
                if (tb != null)
                {
                    tb.Reset();
                    Ut.Message("Camera reset");
                }
                break;

            case Key.V:
                // Toggle VSync.
                if (window != null)
                {
                    window.VSync = !window.VSync;
                    if (window.VSync)
                    {
                        Ut.Message("VSync on");
                        fps.Reset();
                    }
                    else
                        Ut.Message("VSync off");
                }
                break;

            case Key.Up:
                if (currentLevelOfRecursion < maxRecursion)
                {
                    currentLevelOfRecursion++;
                    indexBuffer = indexBuffers[currentLevelOfRecursion];
                    lock (renderLock)
                    {
                        terrainData = TerrainGeneration.DiamondSquare.Subdivide(terrainData, currentLevelOfRecursion - 1, gridSize, roughness);
                        vertexBuffer.Clear();
                        ConvertTerrainDataToVertexBuffer();
                    }
                    UpdateNormals();
                }
                break;

            case Key.Down:
                if (currentLevelOfRecursion > 0)
                {
                    currentLevelOfRecursion--;
                    indexBuffer = indexBuffers[currentLevelOfRecursion];
                    UpdateNormals();
                }
                else
                {
                    lock (renderLock)
                    {
                        terrainData = TerrainGeneration.DiamondSquare.ChangeCorners(gridSize, minHeight, maxHeight);
                        vertexBuffer.Clear();
                        ConvertTerrainDataToVertexBuffer();
                    }
                    UpdateNormals();
                }
                break;

            case Key.Left:
                if (Objects.Count > 0 && cameraState == CameraState.NormalTrackBall)
                {
                    FirstObject.Rotate(-0.1f);
                }
                break;

            case Key.Right:
                if (Objects.Count > 0 && cameraState == CameraState.NormalTrackBall)
                {
                    FirstObject.Rotate(0.1f);
                }
                break;

            case Key.PageDown:
                roughness-=10;
                Ut.MessageInvariant($"Hausdorff coefficient: {roughness}");
                break;
            case Key.PageUp:
                Ut.MessageInvariant($"Hausdorff coefficient: {roughness}");
                roughness+=10;
                break;

            case Key.F1:
                // Help.
                Ut.Message("T           toggle texture CURRENTLY NOT WORKING", true);
                Ut.Message("I           toggle Phong shading", true);
                Ut.Message("P           toggle perspective", true);
                Ut.Message("V           toggle VSync", true);
                Ut.Message("C           camera reset", true);
                Ut.Message("Left, Right rotate the object", true);
                Ut.Message("Home        reset the object", true);
                Ut.Message("F1          print help", true);
                Ut.Message("Esc         quit the program", true);
                Ut.Message("Mouse.left  Trackball rotation", true);
                Ut.Message("Mouse.right drag current object", true);
                Ut.Message("Mouse.wheel zoom in/out", true);
                Ut.Message("--------------------------------------------", true);
                Ut.Message("Up, Down    Updivide/Subdivide", true);
                Ut.Message("PageUp      increase Hausdorff coefficient", true);
                Ut.Message("PageDown    decrease Hausdorff coefficient", true);
                Ut.Message("--------------------------------------------", true);
                Ut.Message("H           switch between camera modes", true);
                Ut.Message("W,A,S,D     move hovercraft", true);
                Ut.Message("Mouse       look around in First-person mode", true);
                Ut.Message("Q           lower hovercraft hover", true);
                Ut.Message("E           higher hovercraft hover", true);
                break;

            case Key.Escape:
                // Close the application.
                window?.Close();
                break;
        }
    }

    // Method to update acceleration based on WASD key state
    private static void UpdateAcceleration()
    {
        acceleration = System.Numerics.Vector3.Zero;
        Debug.Assert(tb != null);
        if (cameraState != CameraState.NormalTrackBall)
        {
            // Get the normalized direction vector of the camera
            System.Numerics.Vector3 cameraFront;
            if (cameraState == CameraState.FirstPerson)
            {
                cameraFront = new System.Numerics.Vector3(tb.Front.X, tb.Front.Y, tb.Front.Z);
            }
            else
            {
                cameraFront = new System.Numerics.Vector3(tb.Direction.X, tb.Direction.Y, tb.Direction.Z);
            }

            if (wDown)
                acceleration += hovercraftSpeed * cameraFront; // Move forward
            if (sDown)
                acceleration += -hovercraftSpeed * cameraFront; // Move backward
            if (aDown)
                acceleration += System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(cameraFront, new System.Numerics.Vector3(0, 1, 0))) * -hovercraftSpeed; // Move left
            if (dDown)
                acceleration += System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(cameraFront, new System.Numerics.Vector3(0, 1, 0))) * hovercraftSpeed; // Move right
        }
    }


    /// <summary>
    /// Handler function for keyboard key up.
    /// </summary>
    /// <param name="arg1">Keyboard object.</param>
    /// <param name="arg2">Key identification.</param>
    /// <param name="arg3">Key scancode.</param>
    private static void KeyUp(IKeyboard arg1, Key arg2, int arg3)
{
    if (tb != null &&
        tb.KeyUp(arg1, arg2, arg3))
        return;

    switch (arg2)
    {
        case Key.ShiftLeft:
        case Key.ShiftRight:
            shiftDown--;
            break;

        case Key.ControlLeft:
        case Key.ControlRight:
            ctrlDown--;
            break;
        // Handle WASD keys
        case Key.W:
            wDown = false;
            break;
        case Key.S:
            sDown = false;
            break;
        case Key.A:
            aDown = false;
            break;
        case Key.D:
            dDown = false;
            break;
        }
}

/// <summary>
/// Mouse dragging - current X coordinate in pixels.
/// </summary>
private static float currentX = 0.0f;

/// <summary>
/// Mouse dragging - current Y coordinate in pixels.
/// </summary>
private static float currentY = 0.0f;

/// <summary>
/// True if dragging mode is active.
/// </summary>
private static bool dragging = false;

/// <summary>
/// Handler function for mouse button down.
/// </summary>
/// <param name="mouse">Mouse object.</param>
/// <param name="btn">Button identification.</param>
private static void MouseDown(IMouse mouse, MouseButton btn)
{
    tb?.MouseDown(mouse, btn);

    if (btn == MouseButton.Right && cameraState == CameraState.NormalTrackBall)
    {
        Ut.MessageInvariant($"Right button down: {mouse.Position}");

        // Start dragging.
        dragging = true;
        currentX = mouse.Position.X;
        currentY = mouse.Position.Y;
    }
}

/// <summary>
/// Handler function for mouse button up.
/// </summary>
/// <param name="mouse">Mouse object.</param>
/// <param name="btn">Button identification.</param>
private static void MouseUp(IMouse mouse, MouseButton btn)
{
    tb?.MouseUp(mouse, btn);

    if (btn == MouseButton.Right)
    {
        Ut.MessageInvariant($"Right button up: {mouse.Position}");

        // Stop dragging.
        dragging = false;
    }
}

/// <summary>
/// Handler function for mouse move.
/// </summary>
/// <param name="mouse">Mouse object.</param>
/// <param name="xy">New mouse position in pixels.</param>
private static void MouseMove(IMouse mouse, System.Numerics.Vector2 xy)
{
    tb?.MouseMove(mouse, xy);

    if (mouse.IsButtonPressed(MouseButton.Right))
    {
        Ut.MessageInvariant($"Mouse drag: {xy}");
    }

    // Object dragging.
    if (dragging)
    {
        float newX = mouse.Position.X;
        float newY = mouse.Position.Y;

        if (newX != currentX || newY != currentY)
        {
            if (Objects.Count > 0)
            {
                Objects[0].Translate(new((newX - currentX) * mouseCx, (newY - currentY) * mouseCy, 0.0f));
            }

            currentX = newX;
            currentY = newY;
        }
    }
}

/// <summary>
/// Handler function for mouse button double click.
/// </summary>
/// <param name="mouse">Mouse object.</param>
/// <param name="btn">Button identification.</param>
/// <param name="xy">Double click position in pixels.</param>
private static void MouseDoubleClick(IMouse mouse, MouseButton btn, System.Numerics.Vector2 xy)
{
    if (btn == MouseButton.Right)
    {
        Ut.Message("Closed by double-click.", true);
        window?.Close();
    }
}

/// <summary>
/// Handler function for mouse wheel rotation.
/// </summary>
/// <param name="mouse">Mouse object.</param>
/// <param name="btn">Mouse wheel object (Y coordinate is used here).</param>
private static void MouseScroll(IMouse mouse, ScrollWheel wheel)
{
    if (tb != null)
    {
        tb.MouseWheel(mouse, wheel);
        SetWindowTitle();
    }

    // wheel.Y is -1 or 1
    Ut.MessageInvariant($"Mouse scroll: {wheel.Y}");
}
}