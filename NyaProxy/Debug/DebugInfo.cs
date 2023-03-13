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

namespace NyaProxy.Debug
{
    public static class DebugHelper
    {
        public static StringTable CreateBridgeTable()
        {
            var bridges = NyaProxy.Bridges?.ToArray();
            StringTable table = new StringTable("Session Id", "Host", "Player", "Source", "Destination");
            foreach (var host in bridges)
            {
                foreach (var bridge in host.Value.Values)
                {
                    IPlayer player = (bridge as BlockingBridge)?.Player;
                    string playerInfo = player is not null ? $"{player.Name}({player.Id})" : "";
                    table.AddRow(bridge.SessionId.ToString($"D{Bridge.Sequence.ToString().Length}"), host.Key, playerInfo,
                         $"{bridge.Source._remoteEndPoint()} ({(NetworkUtils.CheckConnect(bridge.Source) ? "Online" : "Offline")})",
                         $"{bridge.Destination._remoteEndPoint()} ({(NetworkUtils.CheckConnect(bridge.Destination) ? "Online" : "Offline")})");

                }
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
            foreach (var pair in NyaProxy.Plugin.Plugins)
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
            foreach (HostConfig host in NyaProxy.Hosts.Values.ToList())
            {
                table.AddRow(host.Name, string.Join('>', host.ServerEndPoints), host.SelectMode, host.ForwardMode, host.ProtocolVersion, host.CompressionThreshold, host.Flags);
            }
            return table;
        }



    }
}
