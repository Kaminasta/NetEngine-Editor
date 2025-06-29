using ImGuiNET;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace NetEngine;
public enum LayoutType
{
    TwoByThree,
    Default,
    Tall,
    Wide
}

public enum EditorWindowType
{
    Hierarchy,
    Inspector,
    Browser,
    Scene,
    Game,
    Console,
    StyleEditor,
    None
}

public enum WindowTheme 
{ 
    Dark,
    Light
}

public static class EditorGui
{
    public static Object? selectedObject;

    public static bool HierarchyOpened = true;
    public static bool InspectorOpened = true;
    public static bool BrowserOpened = true;
    public static bool SceneViewportOpened = true;
    public static bool GameViewportOpened = true;
    public static bool ConsoleOpened = true;
    public static bool StyleEditorOpened = false;

    public static Vector2 SceneViewportSize { get; private set; }
    public static Vector2 GameViewportSize { get; private set; }

    public static Vector2 SceneViewportPosition { get; private set; }
    public static Vector2 GameViewportPosition { get; private set; }

    public static float SceneViewportSensitivity = 20f;

    public static LayoutType CurrentLayout { get; private set; }
    public static EditorWindowType FocusedWindow { get; private set; } = EditorWindowType.None;
    public static WindowTheme CurrentTheme { get; private set; } = WindowTheme.Dark;

    public static ImGuiIOPtr io;

    public static Action<GameObject> OnHierarchyDoubleClickAction;

    public static void Init()
    {
        LoadLayout(LayoutType.Default);

        io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        io.IniSavingRate = 0;

        ChangeTheme(CurrentTheme);
    }

    public static void ChangeTheme(WindowTheme theme)
    {
        CurrentTheme = theme;

        ImGuiStylePtr style = ImGui.GetStyle();

        style.WindowMenuButtonPosition = ImGuiDir.None;
        style.FrameBorderSize = 0;
        style.TabRounding = 0f;
        style.TabBarOverlineSize = 1f;

        style.ScrollbarSize = 10f;

        style.DockingSeparatorSize = 1;

        style.FrameRounding = 3.0f;
        style.WindowRounding = 2.0f;
        style.PopupRounding = 4.0f;

        IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
        Program.ChangeTitleBarTheme(hwnd, theme);

        if (theme == WindowTheme.Dark)
        {
            var colors = style.Colors;

            colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.00f, 0.00f, 0.00f, 0.30f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.16f, 0.16f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.14f, 0.14f, 0.15f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.08f, 0.08f, 0.08f, 0.51f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.15f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.09f, 0.09f, 0.09f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.16f, 0.16f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.82f, 0.32f, 0.12f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.82f, 0.32f, 0.11f, 1.00f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.62f, 0.26f, 0.11f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.82f, 0.32f, 0.11f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0.39f, 0.39f, 0.39f, 0.62f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.14f, 0.44f, 0.80f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.14f, 0.44f, 0.80f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.35f, 0.35f, 0.35f, 0.17f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.82f, 0.32f, 0.11f, 0.67f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.82f, 0.32f, 0.11f, 0.95f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.82f, 0.32f, 0.12f, 0.06f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.TabSelected] = new Vector4(0.82f, 0.32f, 0.12f, 0.06f);
            colors[(int)ImGuiCol.TabSelectedOverline] = new Vector4(0.82f, 0.32f, 0.12f, 1.00f);
            colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.TabDimmedSelectedOverline] = new Vector4(1.00f, 0.38f, 0.14f, 1.00f);
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.62f, 0.26f, 0.11f, 1.00f);
            colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.PlotLines] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.45f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.78f, 0.87f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.57f, 0.57f, 0.64f, 1.00f);
            colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.68f, 0.68f, 0.74f, 1.00f);
            colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.30f, 0.30f, 0.30f, 0.09f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            colors[(int)ImGuiCol.TextLink] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
            colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(0.70f, 0.70f, 0.70f, 0.70f);
            colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.20f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
        }
        else ImGui.StyleColorsLight();
    }

    public static void LoadLayout(LayoutType layoutType)
    {
        CurrentLayout = layoutType;

        string layoutString = layoutType switch
        {
            LayoutType.Default => UiPosition.Default,
            LayoutType.Tall => UiPosition.Tall,
            LayoutType.Wide => UiPosition.Wide,
            LayoutType.TwoByThree => UiPosition.TwoByThree,
            _ => UiPosition.Default
        };

        ImGui.LoadIniSettingsFromMemory(layoutString);
    }

    public static void KeyboardHandle()
    {
        bool isСontrol = ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);

        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._1, false))
            HierarchyOpened = !HierarchyOpened;
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._2, false))
            InspectorOpened = !InspectorOpened;
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._3, false))
            BrowserOpened = !BrowserOpened;
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._4, false))
            SceneViewportOpened = !SceneViewportOpened;
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._5, false))
            GameViewportOpened = !GameViewportOpened;
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey._6, false))
            ConsoleOpened = !ConsoleOpened;

        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey.N, false))
            Project.New();
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey.O, false))
            Project.Open();
        if (isСontrol && ImGui.IsKeyPressed(ImGuiKey.S, false))
            Project.Save();
    }

    public static void ShowHierarchyWindow()
    {
        if (!HierarchyOpened)
            return;
        
        ImGui.PushID("HierarchyWindowID");
        ImGui.Begin("Hierarchy", ref HierarchyOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Hierarchy;

        ImGui.BeginMenuBar();

        if (ImGui.TreeNodeEx("Scene", ImGuiTreeNodeFlags.DefaultOpen))
        {
            for (int i = 0; i < Project.Data.MainScene.GameObjects.Count; i++)
            {
                var go = Project.Data.MainScene.GameObjects[i];
                var name = (go.Name ?? $"GameObject_{i}") + (go.IsActive ? string.Empty : " (Inactive)");

                if (ImGui.Selectable(name, selectedObject == go))
                    selectedObject = go;

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    OnHierarchyDoubleClickAction.Invoke(go);

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ImGui.OpenPopup($"context_menu_{i}");

                if (ImGui.BeginPopup($"context_menu_{i}"))
                {
                    if (ImGui.MenuItem("Delete", "Del"))
                    {
                        Project.Data.MainScene.GameObjects.RemoveAt(i);
                        if (selectedObject == go)
                            selectedObject = null;
                        ImGui.EndPopup();
                        break;
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.TreePop();
        }

        ImGui.End();

        ImGui.PopID();
    }


    public static void ShowGameViewportWindow()
    {
        if (!GameViewportOpened)
            return;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.PushID("GameWindowID");
        ImGui.Begin("Game", ref GameViewportOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Game;

        ImGui.PopStyleVar();

        GameViewportSize = ImGui.GetContentRegionAvail();
        GameViewportPosition = ImGui.GetCursorScreenPos();

        ImGui.Image(
            (nint)Editor.GameBuffer.FrameTexture,
            GameViewportSize,
            new Vector2(0, 1),
            new Vector2(1, 0)
        );

        ImGui.End();

        ImGui.PopID();
    }

    public static void ShowSceneViewportWindow()
    {
        if (!SceneViewportOpened)
            return;

        ImGui.PushID("SceneWindowID");
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("Scene", ref SceneViewportOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Scene;

        ImGui.PopStyleVar();

        SceneViewportSize = ImGui.GetContentRegionAvail();
        SceneViewportPosition = ImGui.GetCursorScreenPos();

        ImGui.Image(
            (nint)Editor.SceneBuffer.FrameTexture,
            SceneViewportSize,
            new Vector2(0, 1),
            new Vector2(1, 0)
        );

        ImGui.End();
        ImGui.PopID();
    }

    //public class BrowserItem
    //{
    //    public string Path;
    //}

    //public class FolderItem : BrowserItem
    //{
    //    public List<BrowserItem> Items = new();
    //}

    //public class FileItem : BrowserItem
    //{
    //    // Можно добавить тип, размер, расширение и т.п.
    //}



    //private static string selectedFolder = "Assets";
    //private static FolderItem rootFolder = new FolderItem
    //{
    //    Path = "Assets",
    //    Items = new List<BrowserItem>
    //    {
    //        new FolderItem
    //        {
    //            Path = "Assets/Scenes",
    //            Items = new List<BrowserItem>()
    //        },
    //        new FolderItem
    //        {
    //            Path = "Assets/SceneTemplates",
    //            Items = new List<BrowserItem>()
    //        },
    //        new FileItem { Path = "Assets/New Shader" },
    //        new FileItem { Path = "Assets/NewMonoBehaviour" },
    //        new FileItem { Path = "Assets/NewSurfaceShader" },
    //    }
    //};

    //private void DrawFolder(FolderItem folder)
    //{
    //    string folderName = System.IO.Path.GetFileName(folder.Path);
    //    bool open = ImGui.TreeNodeEx(folderName, ImGuiTreeNodeFlags.DefaultOpen);

    //    if (ImGui.IsItemClicked())
    //    {
    //        selectedFolder = folder.Path;
    //    }

    //    if (open)
    //    {
    //        foreach (var item in folder.Items)
    //        {
    //            if (item is FolderItem subfolder)
    //            {
    //                DrawFolder(subfolder); // Рекурсия
    //            }
    //            else if (item is FileItem file)
    //            {
    //                string fileName = System.IO.Path.GetFileName(file.Path);
    //                if (ImGui.Selectable(fileName, selectedFolder == file.Path))
    //                {
    //                    selectedFolder = file.Path;
    //                }
    //            }
    //        }

    //        ImGui.TreePop();
    //    }
    //}


    public static void ShowBrowserWindow()
    {
        if (!BrowserOpened)
            return;

        ImGui.PushID("BrowserWindowID");
        ImGui.Begin("Browser", ref BrowserOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Browser;

        //if (ImGui.BeginTable("BrowserSplitTable", 2))
        //{
        //    // Устанавливаем ширину столбцов в долях доступного размера
        //    ImGui.TableSetupColumn("Tree", ImGuiTableColumnFlags.WidthStretch, 0.3f);
        //    ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch, 0.7f);

        //    ImGui.TableNextRow();

        //    // Левая колонка — дерево
        //    ImGui.TableSetColumnIndex(0);
        //    DrawFolder(rootFolder);

        //    // Правая колонка — содержимое
        //    ImGui.TableSetColumnIndex(1);
        //    var currentFolder = FindFolderByPath(rootFolder, selectedFolder);
        //    if (currentFolder != null)
        //    {
        //        int columns = 4;
        //        if (ImGui.BeginTable("ContentTable", columns, ImGuiTableFlags.SizingStretchSame))
        //        {
        //            int count = 0;
        //            foreach (var item in currentFolder.Items)
        //            {
        //                if (count % columns == 0)
        //                    ImGui.TableNextRow();

        //                ImGui.TableSetColumnIndex(count % columns);

        //                string name = System.IO.Path.GetFileName(item.Path);
        //                if (ImGui.Button(name, new System.Numerics.Vector2(100, 80)))
        //                {
        //                    // Логика по нажатию
        //                }

        //                count++;
        //            }
        //            ImGui.EndTable();
        //        }
        //    }

        //    ImGui.EndTable();
        //}

        ImGui.End();
        ImGui.PopID();
    }


    //private FolderItem? FindFolderByPath(FolderItem folder, string path)
    //{
    //    if (folder.Path == path)
    //        return folder;

    //    foreach (var item in folder.Items)
    //    {
    //        if (item is FolderItem subfolder)
    //        {
    //            var result = FindFolderByPath(subfolder, path);
    //            if (result != null)
    //                return result;
    //        }
    //    }

    //    return null;
    //}


    private static bool _autoScroll = true;
    private static bool _showInfo = true;
    private static bool _showWarnings = true;
    private static bool _showErrors = true;
    private static bool _showEditorMsg = true;

    public static void ShowConsoleWindow()
    {
        if (!ConsoleOpened)
            return;

        ImGui.PushID("ConsoleWindowID");
        ImGui.Begin("Console", ref ConsoleOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Console;

        if (ImGui.Button("Clear console")) Console.Clear(); ImGui.SameLine();
        ImGui.Checkbox("Info", ref _showInfo); ImGui.SameLine();
        ImGui.Checkbox("Warnings", ref _showWarnings); ImGui.SameLine();
        ImGui.Checkbox("Errors", ref _showErrors); ImGui.SameLine();
        ImGui.Checkbox("Editor logs", ref _showEditorMsg); ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);

        ImGui.Separator();

        if (ImGui.BeginChild("ConsoleScrollRegion"))
        {
            if (ImGui.BeginTable("ConsoleTable", 4, ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("From", ImGuiTableColumnFlags.WidthFixed, 30);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);

                var logs = Console.GetLogs().ToList();

                for (int i = 0; i < logs.Count; i++)
                {
                    var entry = logs[i];

                    if ((entry.Type == LogType.Info && !_showInfo) ||
                        (entry.Type == LogType.Warn && !_showWarnings) ||
                        (entry.Type == LogType.Error && !_showErrors) ||
                        (entry.IsEditorMsg && !_showEditorMsg))
                        continue;

                    Vector4? bgColor = entry.Type switch
                    {
                        LogType.Warn => new Vector4(1f, 1f, 0.25f, 0.2f),
                        LogType.Error => new Vector4(1f, 0.25f, 0.25f, 0.2f),
                        _ => null
                    };

                    ImGui.TableNextRow();

                    if (bgColor.HasValue)
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(bgColor.Value));

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(entry.Timestamp.ToString("HH:mm:ss"));

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(entry.Type.ToString());

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(entry.IsEditorMsg ? "Editor" : "Game");

                    ImGui.TableSetColumnIndex(3);
                    string message = entry.Message;
                    ImGui.Selectable(message, false, ImGuiSelectableFlags.SpanAllColumns);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        ImGui.OpenPopup($"Console_cm_" + i);

                    if (ImGui.BeginPopup($"Console_cm_" + i))
                    {
                        if (ImGui.MenuItem("Copy to clipboard"))
                        {
                            ImGui.SetClipboardText($"[{entry.Type}] [{entry.Timestamp:HH:mm:ss}] {message}");
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }

                    if (_autoScroll && i == logs.Count - 1)
                        ImGui.SetScrollHereY(1.0f);
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();
        }

        ImGui.End();
        ImGui.PopID();
    }


    public static void ShowStyleEditor()
    {
        if (!StyleEditorOpened)
            return;

        ImGui.PushID("StyleEditorWindowID");
        ImGui.Begin("Style Editor", ref StyleEditorOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.StyleEditor;

        ImGui.ShowStyleEditor();

        ImGui.End();
        ImGui.PopID();
    }

    public static void ShowInspectorWindow()
    {   
        if (!InspectorOpened)
            return;

        ImGui.PushID("InspectorWindowID");
        ImGui.Begin("Inspector", ref InspectorOpened);

        if (ImGui.IsWindowFocused())
            FocusedWindow = EditorWindowType.Inspector;

        if (selectedObject != null)
        {
            if (selectedObject is GameObject selectedGameObject)
            {
                // Статус активности
                bool isActive = selectedGameObject.IsActive;
                if (ImGui.Checkbox("##ActiveCheckbox", ref isActive))
                    selectedGameObject.SetActive(isActive);
                ImGui.SameLine();

                // Имя объекта
                string name = selectedGameObject.Name ?? "";
                byte[] buffer = new byte[100];
                Encoding.UTF8.GetBytes(name, 0, name.Length, buffer, 0);
                ImGui.PushItemWidth(-1);
                if (ImGui.InputText("##Name", buffer, (uint)buffer.Length))
                    selectedGameObject.Name = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                ImGui.PopItemWidth();

                if (ImGui.BeginTable("TagLayerTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody))
                {
                    ImGui.TableSetupColumn("LabelTag", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("ComboTag", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("LabelLayer", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("ComboLayer", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();

                    // --- Tag Label ---
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Tag");

                    // --- Tag Combo ---
                    ImGui.TableSetColumnIndex(1);
                    string[] availableTags = ["Test Tag 1", "Test Tag 2"];
                    int currentTagIndex = Array.IndexOf(availableTags, selectedGameObject.Tag);
                    if (currentTagIndex < 0) currentTagIndex = 0;

                    ImGui.PushItemWidth(-1); // Автоматически заполнит ячейку
                    if (ImGui.Combo("##TagCombo", ref currentTagIndex, availableTags, availableTags.Length))
                        selectedGameObject.Tag = availableTags[currentTagIndex];
                    ImGui.PopItemWidth();

                    // --- Layer Label ---
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text("Layer");

                    // --- Layer Combo ---
                    ImGui.TableSetColumnIndex(3);
                    string[] availableLayers = ["Test Layer 1", "Test Layer 2"];
                    int currentLayerIndex = Array.IndexOf(availableLayers, selectedGameObject.Layer);
                    if (currentLayerIndex < 0) currentLayerIndex = 0;

                    ImGui.PushItemWidth(-1);
                    if (ImGui.Combo("##LayerCombo", ref currentLayerIndex, availableLayers, availableLayers.Length))
                        selectedGameObject.Layer = availableLayers[currentLayerIndex];
                    ImGui.PopItemWidth();

                    ImGui.EndTable();
                }

                ImGui.Separator();


                // Отображение компонентов
                var components = selectedGameObject.GetComponents().ToList();

                foreach (var component in components)
                {
                    var type = component.GetType();

                    bool opened = ImGui.CollapsingHeader($"{type.Name} ({type.BaseType?.Name})", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowOverlap);

                    if (component is Behaviour behaviour)
                    {
                        ImGui.SameLine(ImGui.GetWindowWidth() - 40);
                        if (ImGui.Button("More##" + type.Name))
                            ImGui.OpenPopup("ContextMenu##" + type.Name);

                        if (ImGui.BeginPopup("ContextMenu##" + type.Name))
                        {
                            if (ImGui.MenuItem("Delete component##" + type.Name))
                                selectedGameObject.RemoveComponent(type);
                            if (ImGui.MenuItem("Enabled component", null, behaviour.Enabled))
                                behaviour.Enabled = !behaviour.Enabled;
                            ImGui.EndPopup();
                        }
                    }

                    if (opened)
                    {
                        if (ImGui.BeginTable("Table##" + type.Name, 2, ImGuiTableFlags.SizingStretchProp))
                        {
                            ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.3f);
                            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.7f);

                            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                            foreach (var field in type.GetFields(flags))
                            {
                                if (field.GetCustomAttribute<ShowInInspectorAttribute>() == null)
                                    continue;

                                object value = field.GetValue(component);

                                DrawFieldEditor(field, ref value);

                                field.SetValue(component, value);
                            }

                            ImGui.EndTable();
                        }
                    }
                }

                ImGui.Separator();

                float windowWidth = ImGui.GetContentRegionAvail().X;
                float buttonWidth = ImGui.CalcTextSize("Add component").X + ImGui.GetStyle().FramePadding.X * 2;

                ImGui.SetCursorPosX((windowWidth - buttonWidth) * 0.5f);
                if (ImGui.Button("Add component"))
                    ImGui.OpenPopup("AddComponentPopup");

                if (ImGui.BeginPopup("AddComponentPopup"))
                {
                    var assemblies = new List<Assembly>
                {
                    typeof(Behaviour).Assembly
                };

                    if (ScriptCompiler.CurrentAssembly != null)
                        assemblies.Add(ScriptCompiler.CurrentAssembly);

                    var behaviours = TypeUtils.GetSubclassesInAssemblies<Behaviour>(assemblies.ToArray());

                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour == typeof(ScriptBehaviour))
                            continue;

                        if (ImGui.Selectable(behaviour.Name))
                        {
                            selectedGameObject.AddComponent(behaviour);
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    ImGui.EndPopup();
                }
            }
        }

        ImGui.End();
        ImGui.PopID();
    }

    private const float stepMovement = 0.05f;

    private static void DrawFieldEditor(FieldInfo field, ref object value)
    {
        RangeAttribute range = field?.GetCustomAttribute<RangeAttribute>();

        var label = field.Name;
        var type = field.FieldType;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text(Convert.ToWords(label));
        ImGui.TableSetColumnIndex(1);
        ImGui.PushItemWidth(-1);

        if (type == typeof(int))
        {
            int v = (int)value;
            if (ImGui.InputInt("##" + label, ref v))
                value = v;
        }
        else if (type == typeof(float))
        {
            float f = (float)value;

            if (range != null)
            {
                if (ImGui.SliderFloat("##" + label, ref f, range.Min, range.Max))
                    value = f;
            }
            else if(ImGui.DragFloat("##" + label, ref f, stepMovement))
                value = f;
        }
        else if (type == typeof(double))
        {
            double d = (double)value;
            if (ImGui.InputDouble("##" + label, ref d, stepMovement))
                value = d;
        }
        else if (type == typeof(string))
        {
            string str = (string)value ?? "";
            byte[] buffer = new byte[256];
            Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            if (ImGui.InputText("##" + label, buffer, (uint)buffer.Length))
                value = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }
        else if (type == typeof(bool))
        {
            bool b = (bool)value;

            if (ImGui.Checkbox("##" + label, ref b))
                value = b;
        }
        else if (type == typeof(Vector4))
        {
            Vector4 v4 = (Vector4)value;
            if (ImGui.DragFloat4("##" + label, ref v4, stepMovement))
                value = v4;
        }
        else if (type == typeof(Vector3))
        {
            Vector3 v3 = (Vector3)value;
            if (ImGui.DragFloat3("##" + label, ref v3, stepMovement))
                value = v3;
        }
        else if (type == typeof(Vector2))
        {
            Vector2 v2 = (Vector2)value;
            if (ImGui.DragFloat2("##" + label, ref v2, stepMovement))
                value = v2;
        }
        else if (type == typeof(Quaternion))
        {
            Quaternion q = (Quaternion)value;
            Vector3 euler = Convert.QuaternionToVector3(q);

            if (ImGui.DragFloat3("##" + label, ref euler, stepMovement))
                value = Convert.VectorToQuaternion(euler);
        }
        else if (type.IsEnum)
        {
            string[] enumNames = Enum.GetNames(type);
            int currentIndex = Array.IndexOf(enumNames, value.ToString());

            if (ImGui.Combo("##" + label, ref currentIndex, enumNames, enumNames.Length))
                value = Enum.Parse(type, enumNames[currentIndex]);
        }
        else ImGui.Text($"Unsupported type: {type.Name}");

        ImGui.PopItemWidth();
    }

    public static void RenderImGui()
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
                        selectedGameObject.Transform.Position = EditorCamera.Camera.Transform.Position;
                        selectedGameObject.Transform.Rotation = EditorCamera.Camera.Transform.Rotation;
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
                    if (ImGui.MenuItem("Dark", null, EditorGui.CurrentTheme == WindowTheme.Dark))
                        EditorGui.ChangeTheme(WindowTheme.Dark);
                    if (ImGui.MenuItem("Light", null, EditorGui.CurrentTheme == WindowTheme.Light))
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

        ShowHierarchyWindow();
        ShowInspectorWindow();
        ShowGameViewportWindow();
        ShowSceneViewportWindow();
        ShowBrowserWindow();
        ShowConsoleWindow();
        ShowStyleEditor();

        ImGui.End();
    }
}
