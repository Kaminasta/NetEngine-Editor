using ImGuiNET;
using Microsoft.CodeAnalysis;
using NetEngine.Components;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NetEngine;

public class Editor
{
    public static readonly string CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string LogoPath = Path.Combine(CurrentPath, "Resources", "logo.png");
    public static readonly string OpenSansFontPath = Path.Combine(CurrentPath, "Fonts", "OpenSans.ttf");
    public static readonly string IconsFontPath = Path.Combine(CurrentPath, "Fonts", "fontawesome-webfont.ttf");

    private GL gl;
    private ImGuiController imguiController;

    private IWindow _window;

    private IInputContext _input;

    private EditorGui EditorGui;

    private FrameBuffer SceneBuffer;
    private FrameBuffer GameBuffer;

    private GameObject editorCamera = new();

    public static Material baseMaterial;
    public static Material gizmosMaterial;
    public static Material skyBoxMaterial;

    public GridMesh? grid;
    public GizmoRenderer? gizmoRenderer;

    public SkyBox? skyBox;

    public string ProjectFilePath = string.Empty;

    public Sound3D sound3D = new Sound3D();

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

        //var allScriptTypes = TypeUtils.GetAllSubclasses<ScriptBehaviour>();
        //foreach (var type in allScriptTypes)
        //    Console.EditorLog($"ScriptBehaviour внутри сборки: {type.FullName}");
    }

    public void Run()
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "NetEngine Editor - v0.1.0";
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6));
        options.Samples = 4;

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
        SetWindowIcon(LogoPath);

        // Window and Input
        gl = OpenGL.GL = GL.GetApi(_window);

        _input = _window.CreateInput();

        // OpenGL
        gl.Enable(GLEnum.Multisample);
        gl.Enable(GLEnum.DepthTest);
        gl.Enable(GLEnum.CullFace);
        gl.Enable(GLEnum.LineSmooth);

        // ImGUI 

        Input.Mouse = _input.Mice[0];
        Input.Keyboard = _input.Keyboards[0];

        ImGuiFontAtlasConfig fontAtlasConfig = new();
        fontAtlasConfig.AddFont(OpenSansFontPath, 16, null, io => io.Fonts.GetGlyphRangesCyrillic());

        ushort[] icons_ranges = { 0xE745, 0xF47F, 0 };

        unsafe {
            ImFontConfigPtr config = ImGuiNative.ImFontConfig_ImFontConfig();
            config.MergeMode = true;

            config.GlyphRanges = Marshal.UnsafeAddrOfPinnedArrayElement(icons_ranges, 0);

            fontAtlasConfig.AddFont(IconsFontPath, 16, config);
        }

        imguiController = new ImGuiController(gl, _window, _input, fontAtlasConfig);

        // Shaders

        baseMaterial = new Material(new Shader(Shaders.BaseVertexShader, Shaders.BaseFragmentShader));
        gizmosMaterial = new Material(new Shader(Shaders.GizmosVertexShader, Shaders.GizmosFragmentShader));
        skyBoxMaterial = new Material(new Shader(Shaders.SkyBoxVertexShader, Shaders.SkyBoxFragmentShader));

        // Other classes

        EditorGui = new EditorGui();
        grid = new GridMesh(gizmosMaterial, 1000);
        gizmoRenderer = new GizmoRenderer(gizmosMaterial);

        this.EditorGui.OnHierarchyDoubleClickAction += (o) =>
        {
            Vector3 cameraPos = editorCamera.transform.Position;
            Vector3 objectPos = o.transform.Position;
            Vector3 front = editorCamera.transform.Front;
            Vector3 toObject = objectPos - cameraPos;

            float distanceOnFront = Vector3.Dot(toObject, front);
            float desiredDistance = Math.Clamp(distanceOnFront, 1.0f, 10.0f);

            editorCamera.transform.Position = objectPos - front * desiredDistance;
        };

        // Scene and Game viewports

        SceneBuffer = new FrameBuffer(_window.Size.X, _window.Size.Y);
        GameBuffer = new FrameBuffer(_window.Size.X, _window.Size.Y);

        string[] skyBoxTextures = {
            "Resources/skybox/right.png",
            "Resources/skybox/left.png",
            "Resources/skybox/top.png",
            "Resources/skybox/bottom.png",
            "Resources/skybox/front.png",
            "Resources/skybox/back.png"
        };

        skyBox = new SkyBox(skyBoxTextures);

        LoadGameObjects();

        sound3D.Init();
    }

    private void LoadGameObjects()
    {
        var cameraComp = editorCamera.AddComponent<Camera>();
        cameraComp.FarPlane = 10000;

        if (!string.IsNullOrEmpty(ProjectFilePath))
            Project.Load(ProjectFilePath);
        else
        {
            // Создание камеры
            {
                GameObject camera1 = new GameObject();
                camera1.AddComponent<Camera>();
                camera1.transform.Position = new(0f, 0f, 4f);
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
                    gl.BindTexture(TextureTarget.TextureCubeMap, skyBox.skyBoxTextureId);

                    material["_skyBox"] = 0;

                    materials.Add(material);
                }

                foreach (var model in models.Models)
                {
                    Console.Log(model.Rotation.ToString());

                    GameObject object1 = new GameObject(model.Name);
                    object1.transform.Position = model.Position / 100;
                    object1.transform.Rotation = model.Rotation;
                    object1.transform.Scale = model.Scale / 100;

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
        HandleSceneCameraMovement();
    }

    private void OnRender(double delta)
    {
        if(EditorGui.CurrentTheme == WindowTheme.Dark)
            gl.ClearColor(0.10f, 0.10f, 0.11f, 1f);
        else
            gl.ClearColor(0.90f, 0.90f, 0.90f, 1f);

        gl.Clear((uint)(GLEnum.ColorBufferBit | GLEnum.DepthBufferBit));

        Time.deltaTime = (float)delta;
        FpsCounter.Update();

        // Рендер Scene
        SceneBuffer.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GameObjectRender(false);
        SceneBuffer.Unbind();

        // Рендер Game
        GameBuffer.Bind();
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GameObjectRender(true);
        GameBuffer.Unbind();

        // Рендер ImGui
        imguiController.Update((float)delta);
        RenderImGui();
        imguiController.Render();
    }

    private void RenderImGui()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowViewport(viewport.ID);

        float menuBarHeight = 0.0f;
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New project", "Ctrl + N"))
                    Project.New();
                if (ImGui.MenuItem("Open project", "Ctrl + O"))
                    Project.Open();
                if (ImGui.MenuItem("Save project", "Ctrl + S"))
                    Project.Save();
                ImGui.Separator();
                if (ImGui.MenuItem("Exit"))
                {

                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("GameObject"))
            {
                bool isEnabled = EditorGui.selectedObject is GameObject && EditorGui.selectedObject != null;
                if (ImGui.MenuItem("Move to camera transform", isEnabled))
                {
                    if (EditorGui.selectedObject is GameObject selectedGameObject)
                    {
                        selectedGameObject.transform.Position = editorCamera.transform.Position;
                        selectedGameObject.transform.Rotation = editorCamera.transform.Rotation;
                    }
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Component"))
            {
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Window"))
            {
                if (ImGui.BeginMenu("Layouts"))
                {
                    if (ImGui.MenuItem("Copy this Layout to clipboard"))
                        ImGui.SetClipboardText(ImGui.SaveIniSettingsToMemory());

                    ImGui.Separator();

                    if (ImGui.MenuItem("Default", null, EditorGui.CurrentLayout == LayoutType.Default))
                        EditorGui.LoadLayout(LayoutType.Default);
                    if (ImGui.MenuItem("Tall", null, EditorGui.CurrentLayout == LayoutType.Tall))
                        EditorGui.LoadLayout(LayoutType.Tall);
                    if (ImGui.MenuItem("Wide", null, EditorGui.CurrentLayout == LayoutType.Wide))
                        EditorGui.LoadLayout(LayoutType.Wide);
                    if (ImGui.MenuItem("2 by 3", null, EditorGui.CurrentLayout == LayoutType.TwoByThree))
                        EditorGui.LoadLayout(LayoutType.TwoByThree);

                    ImGui.EndMenu();
                }
                ImGui.Separator();
                if (ImGui.BeginMenu("General"))
                {
                    ImGui.MenuItem("Hierarchy", "Ctrl + 1", ref EditorGui.HierarchyOpened);
                    ImGui.MenuItem("Inspector", "Ctrl + 2", ref EditorGui.InspectorOpened);
                    ImGui.MenuItem("Browser", "Ctrl + 3", ref EditorGui.BrowserOpened);
                    ImGui.MenuItem("Scene", "Ctrl + 4", ref EditorGui.SceneViewportOpened);
                    ImGui.MenuItem("Game", "Ctrl + 5", ref EditorGui.GameViewportOpened);
                    ImGui.MenuItem("Console", "Ctrl + 6", ref EditorGui.ConsoleOpened);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("ImGui"))
                {
                    ImGui.MenuItem("Style Editor", null, ref EditorGui.StyleEditorOpened);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Window Theme"))
                {
                    if(ImGui.MenuItem("Dark", null, EditorGui.CurrentTheme == WindowTheme.Dark))
                        EditorGui.ChangeTheme(WindowTheme.Dark);
                    if(ImGui.MenuItem("Light", null, EditorGui.CurrentTheme == WindowTheme.Light))
                        EditorGui.ChangeTheme(WindowTheme.Light);
                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }
            menuBarHeight = ImGui.GetWindowSize().Y;

            string fpsText = $"FPS: {FpsCounter.Fps:F0}";
            float textWidth = ImGui.CalcTextSize(fpsText).X;

            ImGui.SameLine(ImGui.GetWindowWidth() - textWidth - 10);
            ImGui.Text(fpsText);

            menuBarHeight = ImGui.GetWindowSize().Y;
            ImGui.EndMainMenuBar();
        }

        ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X, viewport.Pos.Y + menuBarHeight));
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, viewport.Size.Y - menuBarHeight));

        ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar |
                                        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                        ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                                        ImGuiWindowFlags.NoDocking;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        ImGui.Begin("MainDockspaceWindow", window_flags);

        ImGui.PopStyleVar(2);

        var dockspace_id = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(dockspace_id, Vector2.Zero, ImGuiDockNodeFlags.None);

        EditorGui.ShowHierarchyWindow();
        EditorGui.ShowInspectorWindow();
        EditorGui.ShowGameViewportWindow(GameBuffer);
        EditorGui.ShowSceneViewportWindow(SceneBuffer);
        EditorGui.ShowBrowserWindow();
        EditorGui.ShowConsoleWindow();
        EditorGui.ShowStyleEditor();

        ImGui.End();
    }

    private void GameObjectRender(bool gameMode)
    {
        Camera? activeCamera = gameMode ? GetCameraWithLowestDepth() : editorCamera.GetComponent<Camera>();
        if (activeCamera == null)
            return;

        activeCamera.AspectRatio = gameMode ?
            EditorGui.GameViewportSize.X / EditorGui.GameViewportSize.Y :
            EditorGui.SceneViewportSize.X / EditorGui.SceneViewportSize.Y;

        var view = activeCamera.GetViewMatrix();
        var projection = activeCamera.GetProjectionMatrix();

        skyBoxMaterial.Use();

        Matrix4x4 skyBoxView = new Matrix4x4(
            view.M11, view.M12, view.M13, 0,
            view.M21, view.M22, view.M23, 0,
            view.M31, view.M32, view.M33, 0,
            0, 0, 0, 1
        );

        skyBoxMaterial["_view"] = skyBoxView;
        skyBoxMaterial["_projection"] = projection;

        skyBox.DrawSky(skyBoxMaterial);

        foreach (var gameObject in Project.Data.MainScene.GameObjects)
        {
            if (!gameObject.IsActive)
                continue;

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
            }

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.Enabled)
            {
                meshRenderer.Render(view, projection);
            }

            //if (EditorGui.selectedObject == gameObject)
            //{
            //    var camera = gameObject.GetComponent<Camera>();
            //    if (camera != null && camera.Enabled)
            //    {
            //        if (!gameMode)
            //        {
            //            camera.RenderGizmos(GizmosShader, view, projection);
            //        }
            //    }
            //}
        }

        if (!gameMode && grid != null && gizmosMaterial != null)
        {
            grid.Render(view, projection);

            foreach (var gameObject in Project.Data.MainScene.GameObjects)
            {
                if (!gameObject.IsActive)
                    continue;

                if (EditorGui.selectedObject == gameObject)
                {
                    var camera = gameObject.GetComponent<Camera>();
                    if (camera != null && camera.Enabled)
                    {
                        gl.Disable(GLEnum.DepthTest);
                        camera.RenderGizmos(gizmosMaterial, view, projection);
                        gl.Enable(GLEnum.DepthTest);
                    }

                    gl.Disable(GLEnum.DepthTest);
                    gizmoRenderer.RenderGizmo(GizmoRenderer.GizmoType.Scale, gameObject.transform, view, projection, editorCamera.transform.Position);
                    gizmoRenderer.RenderGizmo(GizmoRenderer.GizmoType.Position, gameObject.transform, view, projection, editorCamera.transform.Position);
                    gizmoRenderer.RenderGizmo(GizmoRenderer.GizmoType.Rotation, gameObject.transform, view, projection, editorCamera.transform.Position);
                    gl.Enable(GLEnum.DepthTest);
                }
            }
        }
    }

    private Vector3 cameraRotation;
    private bool isCameraRotating = false;
    private bool isCameraMoving = false;
    private float acceleration = 1f;
    private float accelerationTimer = 0f;

    public float moveSpeed = 5f;
    public float accelerationDuration = 15f;

    private void HandleSceneCameraMovement()
    {
        if (editorCamera == null || !EditorGui.SceneViewportOpened) return;

        sound3D.UpdateListenerPosition(editorCamera.transform.Position, editorCamera.transform.Front, editorCamera.transform.Up);

        Vector2 viewportPos = EditorGui.SceneViewportPosition;
        Vector2 viewportSize = EditorGui.SceneViewportSize;
        Vector2 viewportCenter = new Vector2(
            (float)Math.Round(viewportPos.X + viewportSize.X * 0.5f),
            (float)Math.Round(viewportPos.Y + viewportSize.Y * 0.5f)
        );

        Vector2 mousePos = Input.GetMousePosition();
        bool isMouseInViewport = mousePos.X >= viewportPos.X && mousePos.X <= viewportPos.X + viewportSize.X &&
                                 mousePos.Y >= viewportPos.Y && mousePos.Y <= viewportPos.Y + viewportSize.Y;
        bool isRightMouseDown = Input.IsMouseButtonPressed(MouseButton.Right);

        if (isRightMouseDown && isMouseInViewport)
        {
            if (!isCameraRotating)
            {
                isCameraRotating = true;
                Input.Mouse.Cursor.CursorMode = CursorMode.Hidden;
                Input.Mouse.Position = viewportCenter;
                return;
            }

            Vector3 direction = Vector3.Zero;
            if (Input.IsKeyPressed(Key.W)) direction += editorCamera.transform.Front;
            if (Input.IsKeyPressed(Key.S)) direction -= editorCamera.transform.Front;
            if (Input.IsKeyPressed(Key.A)) direction -= editorCamera.transform.Right;
            if (Input.IsKeyPressed(Key.D)) direction += editorCamera.transform.Right;
            if (Input.IsKeyPressed(Key.E)) direction += editorCamera.transform.Up;
            if (Input.IsKeyPressed(Key.Q)) direction -= editorCamera.transform.Up;

            float scrollDelta = Input.GetScrollDelta();

            if (scrollDelta > 0 || scrollDelta < 0)
            {
                moveSpeed += scrollDelta > 0 ? 0.1f : -0.1f;
                moveSpeed = float.Clamp(moveSpeed, 0.5f, 10f);
                Console.EditorLog("Viewport camera move speed: " + moveSpeed);
            }

            if (direction != Vector3.Zero)
            {
                float baseSpeed = Input.IsKeyPressed(Key.ShiftLeft) ? moveSpeed * 2 : moveSpeed;

                if (!isCameraMoving)
                {
                    isCameraMoving = true;
                    accelerationTimer = 0f;
                    acceleration = 1f;
                }
                else
                {
                    accelerationTimer = Math.Min(accelerationTimer + Time.deltaTime, accelerationDuration);
                    float t = accelerationTimer / accelerationDuration;
                    acceleration = 1f + t * (moveSpeed * 3 - 1f);
                }

                editorCamera.transform.Position += Vector3.Normalize(direction) * baseSpeed * acceleration * Time.deltaTime;
            }
            else
            {
                isCameraMoving = false;
                acceleration = 1f;
                accelerationTimer = 0f;
            }

            Vector2 delta = Input.GetMousePosition() * Time.deltaTime - viewportCenter * Time.deltaTime;
            if (delta != Vector2.Zero)
            {
                cameraRotation.Y -= delta.X * EditorGui.SceneViewportSensitivity;
                cameraRotation.X = Math.Clamp(cameraRotation.X - delta.Y * EditorGui.SceneViewportSensitivity, -89.99f, 89.99f);

                editorCamera.transform.Rotation = Convert.VectorToQuaternion(new(cameraRotation.X, cameraRotation.Y, 0));
            }

            Input.Mouse.Position = viewportCenter;
        }
        else if (isCameraRotating)
        {
            isCameraRotating = false;
            Input.Mouse.Cursor.CursorMode = CursorMode.Normal;
        }
    }

    private void OnClose()
    {
        //mesh?.Dispose();
    }

    private unsafe void SetWindowIcon(string path)
    {
        using Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        var pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

        var rawImage = new RawImage(image.Width, image.Height, pixels);

        _window.SetWindowIcon(new ReadOnlySpan<RawImage>(new[] { rawImage }));
    }
}