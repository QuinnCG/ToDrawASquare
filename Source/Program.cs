using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbiSharp;

namespace ToDrawASquare;

static class Program
{
	static unsafe void Main()
	{
		// Initialize GLFW.
		GLFW.Init();

		// Describe window.
		GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
		GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 4);
		GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);
		GLFW.WindowHint(WindowHintBool.Resizable, true);
		GLFW.WindowHint(WindowHintInt.Samples, 4);

		// Create window.
		var window = GLFW.CreateWindow(1200, 800, "To Draw a Square", null, null);
		GLFW.SetWindowSizeCallback(window, OnWindowResize);

		// Initialize OpenGL.
		GLFW.MakeContextCurrent(window);
		GL.LoadBindings(new GLFWBindingsContext());

		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		// Create buffers.
		int vao = GL.GenVertexArray();
		GL.BindVertexArray(vao);

		var vertices = new float[]
		{
			// Position			// Color			// UV
			-0.5f, -0.5f,		1f, 0.2f, 0.2f,		0f, 0f,
			-0.5f,  0.5f,		0.2f, 1f, 0.2f,		0f, 1f,
			 0.5f,  0.5f,		0.2f, 0.2f, 1f,		1f, 1f,
			 0.5f, -0.5f,		1f, 1f, 1f,			1f, 0f,
		};

		int vbo = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
		GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);

		var indices = new uint[]
		{
			0, 1, 2,
			3, 0, 2
		};

		int ibo = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
		GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint) * indices.Length, indices, BufferUsageHint.StaticDraw);

		int stride = sizeof(float) * 7;

		GL.EnableVertexAttribArray(0);
		GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);

		GL.EnableVertexAttribArray(1);
		GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, sizeof(float) * 2);

		GL.EnableVertexAttribArray(2);
		GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, sizeof(float) * 5);

		// Create shader.
		string shaderSource = File.ReadAllText("Resources/BasicShader.glsl");
		string[] shaderParts = shaderSource.Split("// Fragment Shader");

		string vertexShaderSource = shaderParts[0];
		string fragmentShaderSource = shaderParts[1];

		// Vertex Shader Object
		int vso = GL.CreateShader(ShaderType.VertexShader);
		GL.ShaderSource(vso, vertexShaderSource);
		GL.CompileShader(vso);

		Console.WriteLine("Vertex Shader Error: " + GL.GetShaderInfoLog(vso));

		// Fragment Shader Object
		int fso = GL.CreateShader(ShaderType.FragmentShader);
		GL.ShaderSource(fso, fragmentShaderSource);
		GL.CompileShader(fso);

		Console.WriteLine("Fragment Shader Error: " + GL.GetShaderInfoLog(fso));

		// Combined Shader (called a "program").
		int shader = GL.CreateProgram();
		GL.AttachShader(shader, vso);
		GL.AttachShader(shader, fso);
		GL.LinkProgram(shader);
		GL.ValidateProgram(shader);

		Console.WriteLine("Program Shader Error: " + GL.GetProgramInfoLog(shader));

		// Create Texture.
		byte[] imageRaw = File.ReadAllBytes("Resources/MyImage.png");
		var imageMem = new MemoryStream(imageRaw);

		Stbi.SetFlipVerticallyOnLoad(true);
		var image = Stbi.LoadFromMemory(imageRaw, 4);

		int texture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, texture);

		GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data.ToArray());

		// Start application loop.
		while (!GLFW.WindowShouldClose(window))
		{
			// Input.
			GLFW.PollEvents();

			// Clear color calculations.
			float time = (float)GLFW.GetTime();
			float speed = 0.1f;
			var color = Color4.FromHsv(new Vector4(time * speed % 1f, 1f, 1f, 1f));

			// Clear background.
			GL.ClearColor(color);
			GL.Clear(ClearBufferMask.ColorBufferBit);

			// Render.
			GL.BindVertexArray(vao);
			GL.UseProgram(shader);
			GL.BindTexture(TextureTarget.Texture2D, texture);

			GLFW.GetWindowSize(window, out int width, out int height);
			float ratio = (float)width / height;
			float scale = 3f;

			var mvp = Matrix4.Identity;
			mvp *= Matrix4.CreateOrthographic(ratio * scale, scale, 0f, 1f);
			mvp *= Matrix4.CreateRotationZ(MathF.PI / 180f * time * 360f);

			GL.UniformMatrix4(GL.GetUniformLocation(shader, "u_mvp"), true, ref mvp);
			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

			// Swap buffers.
			GLFW.SwapBuffers(window);
		}

		// Clean up resources.
		GL.DeleteVertexArray(vao);
		GL.DeleteBuffer(vbo);
		GL.DeleteBuffer(ibo);

		GL.DeleteProgram(shader);
		GL.DeleteShader(vso);
		GL.DeleteShader(fso);

		GL.DeleteTexture(texture);

		GLFW.Terminate();
	}

	private static unsafe void OnWindowResize(Window* window, int width, int height)
	{
		GL.Viewport(0, 0, width, height);
	}
}
