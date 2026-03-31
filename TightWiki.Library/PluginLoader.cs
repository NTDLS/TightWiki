using Microsoft.Extensions.Logging;
using System.Reflection;

namespace TightWiki.Library
{
    public static class PluginLoader
    {
        public static void LoadPlugins(ILogger logger, string pluginsDirectory )
        {
            try
            {
                if (!Directory.Exists(pluginsDirectory))
                {
                    Directory.CreateDirectory(pluginsDirectory);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while loading plugins: {ex.Message}");
            }

            if (Directory.Exists(pluginsDirectory))
            {
                foreach (var dllPath in Directory.GetFiles(pluginsDirectory, "*.dll"))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllPath);

                        var pluginType = assembly.GetTypes()
                            .FirstOrDefault(t => t.FullName == "TightWiki.Plugin.TwPluginModule");

                        if (pluginType == null)
                            continue;

                        var instance = Activator.CreateInstance(pluginType);
                        var method = pluginType.GetMethod("GetVersion");
                        var version = method?.Invoke(instance, null) as string;
                        logger.LogInformation($"Loaded plugin: {Path.GetFileName(dllPath)}, version: {version}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to load {Path.GetFileName(dllPath)}: {ex.Message}");
                    }
                }
            }
        }
    }
}
