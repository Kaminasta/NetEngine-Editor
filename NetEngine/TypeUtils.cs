using System.Reflection;

namespace NetEngine;

public static class TypeUtils
{
    public static IEnumerable<Type> GetAllSubclasses<TBase>()
    {
        var baseType = typeof(TBase);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(type => type != baseType && baseType.IsAssignableFrom(type));
    }

    public static IEnumerable<Type> GetSubclasses<TBase>(Assembly assembly)
    {
        var baseType = typeof(TBase);
        return SafeGetTypes(assembly)
            .Where(type => type != baseType && baseType.IsAssignableFrom(type));
    }

    public static IEnumerable<Type> GetSubclassesInAssemblies<TBase>(params Assembly[] assemblies)
    {
        var baseType = typeof(TBase);
        return assemblies
            .SelectMany(SafeGetTypes)
            .Where(type => type != null && type != baseType && baseType.IsAssignableFrom(type));
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e) {
            return e.Types.Where(t => t != null);
        }
    }
}
