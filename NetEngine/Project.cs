using NetEngine.Windows;
using Newtonsoft.Json;

namespace NetEngine
{
    public static class Project
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        private static string _projectFolderPath = string.Empty;
        public static string ProjectFolderPath
        {
            get => _projectFolderPath;
            private set
            {
                if (_projectFolderPath != value)
                {
                    _projectFolderPath = value;
                    InitializeWatcher();
                }
            }
        }
        public static string ProjectFilePath { get; private set; } = string.Empty;

        public static ProjectData Data = new();

        public static event EventHandler? ProjectLoaded;

        public static void InitializeWatcher()
        {
            Console.Log("InitializeWatcher: Удаление старого наблюдателя...");
            AssetWatcher.Dispose();

            if (string.IsNullOrEmpty(ProjectFolderPath))
            {
                Console.Log("InitializeWatcher: Путь к проекту пустой, прерываю.");
                return;
            }

            string assetsPath = Path.Combine(ProjectFolderPath, "assets");
            Console.Log($"InitializeWatcher: Путь к папке assets установлен: {assetsPath}");

            AssetWatcher.Initialize(assetsPath);
            AssetWatcher.ScriptsChanged += (s, e) =>
            {
                Console.Log("InitializeWatcher: Сработало событие изменения скриптов.");
                CompileAllScriptsInAssets();
            };
        }

        public static void CompileAllScriptsInAssets()
        {
            if (string.IsNullOrEmpty(ProjectFolderPath))
            {
                Console.Log("CompileAllScriptsInAssets: Путь к проекту пустой, прерываю.");
                return;
            }

            var assetsPath = Path.Combine(ProjectFolderPath, "assets");
            Console.Log($"CompileAllScriptsInAssets: Проверяю директорию {assetsPath}");

            if (!Directory.Exists(assetsPath))
            {
                Console.Log("CompileAllScriptsInAssets: Директория assets не найдена, прерываю.");
                return;
            }

            var allScripts = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);
            Console.Log($"CompileAllScriptsInAssets: Найдено скриптов: {allScripts.Length}");

            if (allScripts.Length == 0)
            {
                Console.Log("CompileAllScriptsInAssets: Скриптов для компиляции нет, прерываю.");
                return;
            }

            Console.Log("CompileAllScriptsInAssets: Запускаю компиляцию скриптов...");
            ScriptCompiler.CompileAndLoadScripts(allScripts);
            Console.Log("CompileAllScriptsInAssets: Компиляция завершена.");
        }



        public static void Load(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
                throw new FileNotFoundException($"Project file not found: " + projectFilePath);

            ProjectFilePath = projectFilePath;
            ProjectFolderPath = Path.GetDirectoryName(projectFilePath);

            string json = File.ReadAllText(ProjectFilePath);
            var loaded = JsonConvert.DeserializeObject<ProjectData>(json, settings);

            if (loaded == null)
                throw new Exception("Failed to deserialize project file");

            Data = loaded;

            ProjectLoaded?.Invoke(null, EventArgs.Empty);
        }

        public static void Save()
        {
            if (string.IsNullOrEmpty(ProjectFolderPath))
            {
                string filePath = FileSavePicker.PickFileToSave("Сохранение проекта NetEngine", $"NewProject.nproj", [("NetEngine Project file", "*.nproj")]);
                if (string.IsNullOrEmpty(filePath))
                    return; 

                string selectedFolder = Path.GetDirectoryName(filePath)!;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                var entries = Directory.GetFileSystemEntries(selectedFolder)
                    .Where(e => !string.Equals(e, filePath, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                string folder;

                if (entries.Length > 0)
                {
                    folder = Path.Combine(selectedFolder, fileNameWithoutExt);
                    Directory.CreateDirectory(folder);
                }
                else folder = selectedFolder;

                ProjectFolderPath = folder;
                ProjectFilePath = Path.Combine(ProjectFolderPath, $"{fileNameWithoutExt}.nproj");
            }

            string projectDir = Path.GetDirectoryName(ProjectFilePath)!;

            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
            Directory.CreateDirectory(Path.Combine(projectDir, "obj"));
            Directory.CreateDirectory(Path.Combine(projectDir, "logs"));

            var json = JsonConvert.SerializeObject(Data, settings);
            File.WriteAllText(ProjectFilePath, json);
        }

        public static void Open()
        {
            var filePath = FilePicker.PickFile("Выберите файл проекта", "nproj", [("NetEngine Project file", "*.nproj")]);

            if (!File.Exists(filePath))
                return;

            Load(filePath);
        }

        public static void New()
        {
            string? projectName = FileSavePicker.PickFileToSave("Создание нового проекта NetEngine", "NewProject.nproj", [("NetEngine Project file", "*.nproj")]);

            if (string.IsNullOrEmpty(projectName))
                return;

            string selectedFolder = Path.GetDirectoryName(projectName)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(projectName);

            string folderPath = Path.Combine(selectedFolder, fileNameWithoutExt);

            Directory.CreateDirectory(folderPath);

            ProjectFolderPath = folderPath;
            ProjectFilePath = Path.Combine(folderPath, $"{fileNameWithoutExt}.nproj");

            Data = new ProjectData
            {
                ProjectName = fileNameWithoutExt,
                Assets = new List<Asset>(),
                MainScene = new Scene()
            };

            Save();

            ProjectLoaded?.Invoke(null, EventArgs.Empty);
        }

    }

    public class ProjectData
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<Asset> Assets { get; set; } = new List<Asset>();
        public Scene MainScene { get; set; } = new Scene();
    }
}
