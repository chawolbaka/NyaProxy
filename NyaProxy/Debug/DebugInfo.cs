using MinecraftProtocol.Utils;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Configs;
using NyaProxy.Extension;
using StringTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NyaProxy.Bridges;
using NyaProxy.API.Command;

namespace NyaProxy.Debug
{
    public static class DebugHelper
    {
        public static StringTable CreateBridgeTable()
        {
            StringTable table = new StringTable("Session Id", "Host", "Player", "Source", "Destination");
            foreach (var bridge in NyaProxy.Bridges?.Values)
            {
                IPlayer player = (bridge as BlockingBridge)?.Player;
                string playerInfo = player is not null ? $"{player.Name}({player.Id})" : "";
                table.AddRow(bridge.SessionId.ToString($"D{Bridge.CurrentSequence.ToString().Length}"), bridge.Host.Name, playerInfo,
                     $"{(NetworkUtils.CheckConnect(bridge.Source) ? "§a" : "§c")}{bridge.Source._remoteEndPoint()}§r",
                     $"{(NetworkUtils.CheckConnect(bridge.Destination) ? "§a" : "§c")}{bridge.Destination._remoteEndPoint()}§r");
            }
            return table;
        }

        public static StringTable CreateCommandTable()
        {
            StringTable table = new StringTable("Command", "Minimum Args");
            List<Command> commands = new List<Command>(NyaProxy.CommandManager.RegisteredCommands.Values);
            commands.Sort((x, y) => string.Compare(x.Name, y.Name));
            foreach (var command in commands)
            {
                table.AddRow(command.Name, command.MinimumArgs);
            }
            return table;
        }

        public static StringTable CreatePluginTable()
        {
            StringTable table = new StringTable("Id", "Name", "Author", "Description", "Version");
            foreach (var pair in NyaProxy.Plugins.Plugins)
            {
                var plugin = pair.Value.Plugin.Manifest;
                table.AddRow(plugin.UniqueId, plugin.Name, plugin.Author, plugin.Description, plugin.Version);
            }
            return table;
        }

        public static StringTable CreateHostsTable()
        {
            //这边太长了稍微简化了一些名称
            StringTable table = new StringTable("Host", "Servers", "Select Mode", "Forward Mode", "Protocol", "Threshold", "Flags");
            foreach (var host in NyaProxy.Hosts.Values.ToList())
            {
                table.AddRow(host.Name, string.Join('>', host.ServerEndPoints), host.SelectMode, host.ForwardMode, host.ProtocolVersion, host.CompressionThreshold, host.Flags);
            }
            return table;
        }



    }
}
