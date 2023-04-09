using MinecraftProtocol.Utils;
using NyaProxy.API;
using NyaProxy.Extension;
using System.Collections.Generic;
using System.Linq;
using NyaProxy.Bridges;
using NyaProxy.API.Command;
using StringTable;

namespace NyaProxy.Debug
{
    public static class DebugHelper
    {
        public static StringTableBuilder CreateBridgeTable()
        {
            StringTableBuilder tableBuilder = new StringTableBuilder();
            tableBuilder.AddColumn("Session Id", "Host", "Player", "Source", "Destination");
            foreach (var bridge in NyaProxy.Bridges?.Values)
            {
                IPlayer player = (bridge as BlockingBridge)?.Player;
                string playerInfo = player is not null ? $"{player.Name}({player.Id})" : "";
                tableBuilder.AddRow(bridge.SessionId.ToString($"D{Bridge.CurrentSequence.ToString().Length}"), bridge.Host.Name, playerInfo,
                     $"{(NetworkUtils.CheckConnect(bridge.Source) ? "§a" : "§c")}{bridge.Source._remoteEndPoint()}§r",
                     $"{(NetworkUtils.CheckConnect(bridge.Destination) ? "§a" : "§c")}{bridge.Destination._remoteEndPoint()}§r");
            }
            return tableBuilder;
        }

        public static StringTableBuilder CreateCommandTable()
        {
            
            StringTableBuilder tableBuilder = new StringTableBuilder();
            tableBuilder.AddColumn("Command", "Minimum Args");
            List<Command> commands = new List<Command>(NyaProxy.CommandManager.RegisteredCommands.Values);
            commands.Sort((x, y) => string.Compare(x.Name, y.Name));
            foreach (var command in commands)
            {
                tableBuilder.AddRow(command.Name, command.MinimumArgs);
            }
            return tableBuilder;
        }

        public static StringTableBuilder CreatePluginTable()
        {
            StringTableBuilder tableBuilder = new StringTableBuilder();
            tableBuilder.AddColumn("Id", "Name", "Author", "Description", "Version");
            foreach (var pair in NyaProxy.Plugins.Plugins)
            {
                var plugin = pair.Value.Plugin.Manifest;
                tableBuilder.AddRow(plugin.UniqueId, plugin.Name, plugin.Author, plugin.Description, plugin.Version);
            }
            return tableBuilder;
        }

        public static StringTableBuilder CreateHostsTable()
        {
            //这边太长了稍微简化了一些名称
            StringTableBuilder tableBuilder = new StringTableBuilder();
            tableBuilder.AddColumn("Host", "Servers", "Select Mode", "Forward Mode", "Protocol", "Threshold", "Flags");
            foreach (var host in NyaProxy.Hosts.Values.ToList())
            {
                tableBuilder.AddRow(host.Name, string.Join('>', host.ServerEndPoints), host.SelectMode, host.ForwardMode, host.ProtocolVersion, host.CompressionThreshold, host.Flags);
            }
            return tableBuilder;
        }



    }
}
