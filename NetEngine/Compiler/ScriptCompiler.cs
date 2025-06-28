using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NetEngine;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public static class ScriptCompiler
{
    public static event Action<Assembly>? OnCompileSucceeded;
    public static event Action<IEnumerable<Diagnostic>>? OnCompileCompleted;
    public static event Action<Assembly>? OnAssemblyLoaded;

    private static readonly ConcurrentDictionary<string, string> _scriptHashes = new();
    private static HotReloadContext? _context;
    private static Assembly? _currentAssembly;
    public static Assembly? CurrentAssembly => _currentAssembly;

    public static Assembly? CompileAndLoadScripts(string[] scriptPaths)
    {
        if (scriptPaths == null || scriptPaths.Length == 0)
            throw new ArgumentException("No scripts provided");

        var hashesChanged = scriptPaths.Any(path =>
        {
            var content = File.ReadAllText(path);
            var newHash = ComputeHash(content);
            var changed = !_scriptHashes.TryGetValue(path, out var oldHash) || oldHash != newHash;
            _scriptHashes[path] = newHash;
            return changed;
        });

        var assemblyName = GetAssemblyName(scriptPaths);
        var assemblyPath = Path.Combine(GetTempPath(), $"{assemblyName}.dll");

        if (!hashesChanged && File.Exists(assemblyPath))
        {
            return LoadAssemblyFromFile(assemblyPath);
        }

        var syntaxTrees = scriptPaths
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .ToList();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        OnCompileCompleted?.Invoke(result.Diagnostics);
        if (!result.Success)
            return null;

        UnloadCurrentAssembly();

        ms.Seek(0, SeekOrigin.Begin);
        _context = new HotReloadContext();
        _currentAssembly = _context.LoadFromStream(ms);

        TrySaveDebugAssembly(assemblyName, ms.ToArray());

        OnCompileSucceeded?.Invoke(_currentAssembly);
        OnAssemblyLoaded?.Invoke(_currentAssembly);
        HotReloadManager.ReloadComponents(_currentAssembly);

        return _currentAssembly;
    }

    public static void UnloadCurrentAssembly()
    {
        if (_context == null)
            return;

        var weakRef = new WeakReference(_context);

        var temp = _currentAssembly;

        _context.Unload();
        _context = null;
        _currentAssembly = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (weakRef.IsAlive)
        {
            NetEngine.Console.EditorWarning("[HotReload] Сборка НЕ была выгружена! Есть висячие ссылки.");
            LogLeakedReferences(temp);
        }
        else
        {
            NetEngine.Console.EditorLog("[HotReload] Сборка успешно выгружена.");
        }
    }

    private static void LogLeakedReferences(Assembly? assembly)
    {
        if (assembly == null)
        {
            NetEngine.Console.EditorWarning("[HotReload] Нет информации о текущей сборке для анализа ссылок.");
            return;
        }

        // Предполагается, что HotReloadManager знает все GameObject
        foreach (var go in HotReloadManager.TrackedGameObjects)
        {
            foreach (var comp in go.GetComponents())
            {
                var compType = comp.GetType();
                if (compType.Assembly == assembly)
                {
                    NetEngine.Console.EditorWarning($"Объект '{go.Name}' содержит компонент '{compType.FullName}', ссылающийся на выгружаемую сборку.");
                }
            }
        }
    }



    private static Assembly LoadAssemblyFromFile(string path)
    {
        UnloadCurrentAssembly();
        _context = new HotReloadContext();
        _currentAssembly = _context.LoadFromAssemblyPath(path);
        OnAssemblyLoaded?.Invoke(_currentAssembly);
        return _currentAssembly;
    }

    private static void TrySaveDebugAssembly(string name, byte[] data)
    {
        try
        {
            var debugPath = Path.Combine(GetTempPath(), $"{name}_{DateTime.Now.Ticks}.dll");
            File.WriteAllBytes(debugPath, data);
        }
        catch (Exception ex)
        {
            NetEngine.Console.EditorError($"[Compiler Save Warning] {ex.Message}");
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha = SHA256.Create();
        return System.Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    private static string GetAssemblyName(string[] paths)
    {
        using var sha = SHA256.Create();
        var combined = string.Join("|", paths.OrderBy(p => p));
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return "ScriptAssembly_" + BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
    }

    private static string GetTempPath()
    {
        var basePath = string.IsNullOrEmpty(Project.ProjectFolderPath)
            ? Path.GetTempPath()
            : Path.Combine(Project.ProjectFolderPath, "obj");

        var path = Path.Combine(basePath, "NetEngine_ScriptCompilerAssemblies");
        Directory.CreateDirectory(path);
        return path;
    }
}
