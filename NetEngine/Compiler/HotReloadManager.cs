using NetEngine;
using System.Reflection;

public static class HotReloadManager
{
    public static List<GameObject> TrackedGameObjects = new();

    public static void RegisterGameObject(GameObject obj)
    {
        if (!TrackedGameObjects.Contains(obj))
            TrackedGameObjects.Add(obj);
    }

    public static void UnregisterGameObject(GameObject obj)
    {
        TrackedGameObjects.Remove(obj);
    }

    public static void ReloadComponents(Assembly newAssembly)
    {
        foreach (var obj in TrackedGameObjects)
            obj.ReloadComponents(newAssembly);
    }
}
