using ImGuiNET;
using Microsoft.CodeAnalysis;
using NetEngine.Components;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NetEngine;

public static class Consts
{
    public static readonly string CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string LogoPath = Path.Combine(CurrentPath, "Resources", "logo.png");
    public static readonly string OpenSansFontPath = Path.Combine(CurrentPath, "Fonts", "OpenSans.ttf");
    public static readonly string IconsFontPath = Path.Combine(CurrentPath, "Fonts", "fontawesome-webfont.ttf");

    public static readonly string[] SkyBoxTextures = {
        "Resources/skybox/right.png",
        "Resources/skybox/left.png",
        "Resources/skybox/top.png",
        "Resources/skybox/bottom.png",
        "Resources/skybox/front.png",
        "Resources/skybox/back.png"
    };
}

public unsafe class Editor
{
    private Version engineVer = new Version(0, 1, 1);
    public string ProjectFilePath = string.Empty;

    private GL gl;
    private ImGuiController imguiController;

    private IWindow _window;
    private IInputContext _input;

    public static FrameBuffer SceneBuffer;
    public static FrameBuffer GameBuffer;

    public static Material baseMaterial;
    public static Material gizmosMaterial;
    public static Material skyBoxMaterial;

    public Editor()
    {
        ScriptCompiler.OnCompileSucceeded += (asm) =>
        {
            Console.EditorLog($"Сборка успешно скомпилирована: {asm.FullName}");
        };

        ScriptCompiler.OnCompileCompleted += (diags) =>
        {
            foreach (var d in diags)
            {
                var msg = d.GetMessage();

                if (d.Severity == DiagnosticSeverity.Error)
                    Console.EditorError(msg);
                else if (d.Severity == DiagnosticSeverity.Warning)
                    Console.EditorWarning(msg);
                else if (d.Severity == DiagnosticSeverity.Info)
                    Console.EditorLog(msg);
            }
        };

        ScriptCompiler.OnAssemblyLoaded += (asm) =>
        {
            Console.EditorLog($"Сборка загружена в память: {asm.FullName}");
        };

        EditorGui.OnHierarchyDoubleClickAction += (o) =>
        {
            var editorCamera = EditorCamera.Camera;

            Vector3 cameraPos = editorCamera.Transform.Position;
            Vector3 objectPos = o.Transform.Position;
            Vector3 front = editorCamera.Transform.Front;
            Vector3 toObject = objectPos - cameraPos;

            float distanceOnFront = Vector3.Dot(toObject, front);
            float desiredDistance = Math.Clamp(distanceOnFront, 1.0f, 10.0f);

            editorCamera.Transform.Position = objectPos - front * desiredDistance;
        };

        //var allScriptTypes = TypeUtils.GetAllSubclasses<ScriptBehaviour>();
        //foreach (var type in allScriptTypes)
        //    Console.EditorLog($"ScriptBehaviour внутри сборки: {type.FullName}");
    }

    public void Run()
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "NetEngine Editor - v0.0.0 (? | ?)";
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6));
        options.Samples = 4;
        //options.VSync = false;

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Resize += OnResize;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.Closing += OnClose;

        _window.Run();
    }

    private void OnLoad()
    {
        SetupWindowAndInput();
        SetupOpenGL();
        SetupImGui();
        SetupShaders();
        InitClasses();
        SetupViewports();
        LoadGameObjects();
    }

    private void SetupWindowAndInput()
    {
        gl = OpenGL.GL = GL.GetApi(_window);
        _input = _window.CreateInput();

        _window.SetIcon(Consts.LogoPath);
        SetWindowTitle();
    }

    private void SetupOpenGL()
    {
        gl.Enable(GLEnum.Multisample);
        gl.Enable(GLEnum.DepthTest);
        gl.Enable(GLEnum.CullFace);
        gl.Enable(GLEnum.LineSmooth);
    }

    private void SetupImGui()
    {
        Input.Mouse = _input.Mice[0];
        Input.Keyboard = _input.Keyboards[0];

        var fontAtlasConfig = new ImGuiFontAtlasConfig();
        fontAtlasConfig.AddFont(Consts.OpenSansFontPath, 16, null, io =>
            io.Fonts.GetGlyphRangesCyrillic());

        // Иконки
        ushort[] icons_ranges = { 0xE745, 0xF47F, 0 };
        unsafe
        {
            ImFontConfigPtr config = ImGuiNative.ImFontConfig_ImFontConfig();
            config.MergeMode = true;
            config.GlyphRanges = Marshal.UnsafeAddrOfPinnedArrayElement(icons_ranges, 0);
            fontAtlasConfig.AddFont(Consts.IconsFontPath, 16, config);
        }

        imguiController = new ImGuiController(gl, _window, _input, fontAtlasConfig);
    }

    private void SetupShaders()
    {
        baseMaterial = new Material(new Shader(Shaders.BaseVertexShader, Shaders.BaseFragmentShader));
        gizmosMaterial = new Material(new Shader(Shaders.GizmosVertexShader, Shaders.GizmosFragmentShader));
        skyBoxMaterial = new Material(new Shader(Shaders.SkyBoxVertexShader, Shaders.SkyBoxFragmentShader));
    }

    private void InitClasses()
    {
        EditorGui.Init();
        GridMesh.Init();
        GizmoRenderer.Init();
        SkyBox.Init();
        SkyBox.LoadCubemap(Consts.SkyBoxTextures);
    }

    private void SetupViewports()
    {
        int w = _window.Size.X, h = _window.Size.Y;
        SceneBuffer = new FrameBuffer(w, h);
        GameBuffer = new FrameBuffer(w, h);
    }

    private void SetWindowTitle()
    {
        string? version = Marshal.PtrToStringAnsi((nint)gl.GetString(GLEnum.Version));
        string? renderer = Marshal.PtrToStringAnsi((nint)gl.GetString(GLEnum.Renderer));

        _window.Title = $"NetEngine Editor - v{engineVer.ToString()} ({version} | {renderer})";
    }

    private void LoadGameObjects()
    {
        EditorCamera.Init();

        if (!string.IsNullOrEmpty(ProjectFilePath))
            Project.Load(ProjectFilePath);
        else
        {
            // Создание камеры
            {
                GameObject camera1 = new GameObject();
                camera1.AddComponent<Camera>();
                camera1.AddComponent<AudioListener>();
                camera1.Transform.Position = new(0f, 0f, 10f);
                camera1.Name = "Main Camera";
                Project.Data.MainScene.GameObjects.Add(camera1);
            }

            // Создание объекта
            {
                var models = FBXImporter.Load(@"D:\cube.fbx");

                List<Material> materials = new List<Material>();
                foreach (var objMat in models.Materials)
                {
                    var material = new Material(new Shader(Shaders.DefaultVertexShader, Shaders.DefaultFragmentShader));
                    material.Use();
                    material["_color"] = objMat.DiffuseColor;
                    //material["_reflection"] = 1.0f;

                    gl.ActiveTexture(TextureUnit.Texture0); 
                    gl.BindTexture(TextureTarget.TextureCubeMap, SkyBox.skyBoxTextureId);

                    material["_skyBox"] = 0;

                    materials.Add(material);
                }

                foreach (var model in models.Models)
                {
                    Console.Log(model.Rotation.ToString());

                    GameObject object1 = new GameObject(model.Name);
                    object1.Transform.Position = model.Position / 100;
                    object1.Transform.Rotation = model.Rotation;
                    object1.Transform.Scale = model.Scale / 100;

                    MeshFilter meshFilter = object1.AddComponent<MeshFilter>();
                    meshFilter.mesh = new Mesh(model.Meshes);

                    MeshRenderer meshRenderer = object1.AddComponent<MeshRenderer>();
                    meshRenderer.materials = materials;

                    Project.Data.MainScene.GameObjects.Add(object1);
                }
                
            }
        }
    }

    private Camera? GetCameraWithLowestDepth()
    {
        Camera? lowestDepthCamera = null;

        foreach (var gameObject in Project.Data.MainScene.GameObjects)
        {
            if (!gameObject.IsActive)
                continue;

            var camera = gameObject.GetComponent<Camera>();

            if (camera == null || !camera.Enabled)
                continue;

            if (lowestDepthCamera == null || camera.Depth < lowestDepthCamera.Depth)
                lowestDepthCamera = camera;
        }

        return lowestDepthCamera;
    }

    private void OnResize(Vector2D<int> newSize)
    {
        gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);

        GameBuffer.RescaleFrameBuffer(newSize.X, newSize.Y);
        SceneBuffer.RescaleFrameBuffer(newSize.X, newSize.Y);
    }

    private void OnUpdate(double delta)
    {
        EditorGui.KeyboardHandle();
        EditorCamera.HandleSceneCameraMovement();
    }

    private void OnRender(double delta)
    {
        if(EditorGui.CurrentTheme == WindowTheme.Dark)
            gl.ClearColor(0.10f, 0.10f, 0.11f, 1f);
        else
            gl.ClearColor(0.90f, 0.90f, 0.90f, 1f);

        Time.DeltaTime = (float)delta;
        FpsCounter.Update();

        // Рендер Scene
        SceneBuffer.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GameObjectRender(false);
        SceneBuffer.Unbind();

        // Рендер Game
        GameBuffer.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GameObjectRender(true);
        GameBuffer.Unbind();

        // Рендер ImGui
        imguiController.Update((float)delta);
        EditorGui.RenderImGui();
        imguiController.Render();
    }

    private void GameObjectRender(bool gameMode)
    {
        Camera? activeCamera = gameMode ? GetCameraWithLowestDepth() : EditorCamera.Camera.GetComponent<Camera>();
        if (activeCamera == null)
            return;

        // Update aspect ratio
        activeCamera.AspectRatio = gameMode ?
            EditorGui.GameViewportSize.X / EditorGui.GameViewportSize.Y :
            EditorGui.SceneViewportSize.X / EditorGui.SceneViewportSize.Y;

        var view = activeCamera.GetViewMatrix();
        var projection = activeCamera.GetProjectionMatrix();

        // Redner SkyBox
        SkyBox.Render(view, projection);

        // Render Grid
        if(!gameMode) GridMesh.Render(view, projection);

        foreach (var gameObject in Project.Data.MainScene.GameObjects)
        {
            if (!gameObject.IsActive)
                continue;

            // Render gizmos
            if (!gameMode && EditorGui.selectedObject == gameObject)
            {
                var camera = gameObject.GetComponent<Camera>();
                if (camera != null && camera.Enabled)
                {
                    gl.Disable(GLEnum.DepthTest);
                    camera.RenderGizmos(gizmosMaterial, view, projection);
                    gl.Enable(GLEnum.DepthTest);
                }

                gl.Disable(GLEnum.DepthTest);
                GizmoRenderer.RenderGizmo(GizmoType.Scale, gameObject.Transform, view, projection, EditorCamera.Camera.Transform.Position);
                GizmoRenderer.RenderGizmo(GizmoType.Position, gameObject.Transform, view, projection, EditorCamera.Camera.Transform.Position);
                GizmoRenderer.RenderGizmo(GizmoType.Rotation, gameObject.Transform, view, projection, EditorCamera.Camera.Transform.Position);
                gl.Enable(GLEnum.DepthTest);
            }

            // Update compnents
            foreach (var component in gameObject.GetComponents())
            {
                if (component is Behaviour behaviour)
                {
                    if (!behaviour.Enabled)
                        continue;

                    if (!behaviour.HasStarted)
                    {
                        behaviour.Start();
                        behaviour.HasStarted = true;
                    }
                    behaviour.Update();
                }

                if (component is MeshRenderer meshRenderer)
                {
                    if (!meshRenderer.Enabled)
                        return;
                    meshRenderer.Render(view, projection);
                }
            }            
        }
    }

    private void OnClose()
    {
        SkyBox.Dispose();
        GridMesh.Dispose();
        GizmoRenderer.Dispose();
    }
}