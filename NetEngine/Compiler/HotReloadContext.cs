using System.Reflection;
using System.Runtime.Loader;

public class HotReloadContext : AssemblyLoadContext
{
    public HotReloadContext() : base(isCollectible: true) { }

    protected override Assembly Load(AssemblyName assemblyName) => null!;
}
