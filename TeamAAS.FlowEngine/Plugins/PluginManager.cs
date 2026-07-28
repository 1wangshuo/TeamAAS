using System.Collections.Generic;
using System.Linq;

namespace TeamAAS.FlowEditor.Plugins
{
    /// <summary>
    /// 插件管理器 - 注册和查找节点插件
    /// </summary>
    public static class PluginManager
    {
        private static readonly Dictionary<string, IFlowNodePlugin> _plugins = new Dictionary<string, IFlowNodePlugin>();

        /// <summary>
        /// 注册插件
        /// </summary>
        public static void Register(IFlowNodePlugin plugin)
        {
            _plugins[plugin.Info.PluginId] = plugin;
        }

        /// <summary>
        /// 获取插件
        /// </summary>
        public static IFlowNodePlugin GetPlugin(string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId)) return null;
            _plugins.TryGetValue(pluginId, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 获取插件信息
        /// </summary>
        public static NodePluginInfo GetPluginInfo(string pluginId)
        {
            var plugin = GetPlugin(pluginId);
            return plugin?.Info;
        }

        /// <summary>
        /// 获取所有已注册插件信息
        /// </summary>
        public static List<NodePluginInfo> GetAllPluginInfos()
        {
            return _plugins.Values.Select(p => p.Info).ToList();
        }
    }
}
