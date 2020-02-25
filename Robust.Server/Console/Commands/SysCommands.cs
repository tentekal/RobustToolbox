﻿using System;
using System.Runtime;
using System.Text;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Robust.Server.Console.Commands
{
    class RestartCommand : IClientCommand
    {
        public string Command => "restart";
        public string Description => "Gracefully restarts the server (not just the round).";
        public string Help => "restart";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    class ShutdownCommand : IClientCommand
    {
        public string Command => "shutdown";
        public string Description => "Gracefully shuts down the server.";
        public string Help => "shutdown";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown(null);
        }
    }

    public class SaveConfig : IClientCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the server configuration to the config file";
        public string Help => "saveconfig";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

    class NetworkAuditCommand : IClientCommand
    {
        public string Command => "netaudit";
        public string Description => "Prints into about NetMsg security.";
        public string Help => "netaudit";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var network = (NetManager) IoCManager.Resolve<INetManager>();

            var callbacks = network.CallbackAudit;

            var sb = new StringBuilder();

            foreach (var kvCallback in callbacks)
            {
                var msgType = kvCallback.Key;
                var call = kvCallback.Value;

                sb.AppendLine($"Type: {msgType.Name.PadRight(16)} Call:{call.Target}");
            }

            shell.SendText(player, sb.ToString());
        }
    }

    class HelpCommand : IClientCommand
    {
        public string Command => "help";

        public string Description =>
            "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";

        public string Help => "Help";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.SendText(player,
                        "To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.");
                    break;

                case 1:
                    var commandName = args[0];
                    if (!shell.AvailableCommands.TryGetValue(commandName, out var cmd))
                    {
                        shell.SendText(player, $"Unknown command: {commandName}");
                        return;
                    }

                    shell.SendText(player, $"Use: {cmd.Help}\n{cmd.Description}");
                    break;

                default:
                    shell.SendText(player, "Invalid amount of arguments.");
                    break;
            }
        }
    }

    class ShowTimeCommand : IClientCommand
    {
        public string Command => "showtime";
        public string Description => "Shows the server time.";
        public string Help => "showtime";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            shell.SendText(player,
                $"Paused: {timing.Paused}, CurTick: {timing.CurTick}, CurTime: {timing.CurTime}, RealTime: {timing.RealTime}");
        }
    }

    internal class GcCommand : IClientCommand
    {
        public string Command => "gc";
        public string Description => "Run the GC.";
        public string Help => "gc [generation]";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length == 0)
            {
                GC.Collect();
            }
            else
            {
                if(int.TryParse(args[0], out int result))
                    GC.Collect(result);
                else
                    shell.SendText(player, "Failed to parse argument.");
            }
        }
    }

    internal class GcModeCommand : IClientCommand
    {

        public string Command => "gc_mode";

        public string Description => "Change the GC Latency mode.";

        public string Help => "gc_mode [type]";

        public void Execute(IConsoleShell console, IPlayerSession player, params string[] args)
        {
            var prevMode = GCSettings.LatencyMode;
            if (args.Length == 0)
            {
                console.SendText(player,$"current gc latency mode: {(int) prevMode} ({prevMode})");
                console.SendText(player,"possible modes:");
                foreach (int mode in Enum.GetValues(typeof(GCLatencyMode)))
                {
                    console.SendText(player,$" {mode}: {Enum.GetName(typeof(GCLatencyMode), mode)}");
                }
            }
            else
            {
                GCLatencyMode mode;
                if (char.IsDigit(args[0][0]) && int.TryParse(args[0], out var modeNum))
                {
                    mode = (GCLatencyMode) modeNum;
                }
                else if (!Enum.TryParse(args[0], true, out mode))
                {
                    console.SendText(player,$"unknown gc latency mode: {args[0]}");
                    return;
                }

                console.SendText(player,$"attempting gc latency mode change: {(int) prevMode} ({prevMode}) -> {(int) mode} ({mode})");
                GCSettings.LatencyMode = mode;
                console.SendText(player,$"resulting gc latency mode: {(int) GCSettings.LatencyMode} ({GCSettings.LatencyMode})");
            }

            return;
        }

    }

    internal class SerializeStatsCommand : IClientCommand
    {

        public string Command => "szr_stats";

        public string Description => "Report serializer statistics.";

        public string Help => "szr_stats";

        public void Execute(IConsoleShell console, IPlayerSession player, params string[] args)
        {

            console.SendText(player,$"serialized: {RobustSerializer.BytesSerialized} bytes, {RobustSerializer.ObjectsSerialized} objects");
            console.SendText(player,$"largest serialized: {RobustSerializer.LargestObjectSerializedBytes} bytes, {RobustSerializer.LargestObjectSerializedType} objects");
            console.SendText(player,$"deserialized: {RobustSerializer.BytesDeserialized} bytes, {RobustSerializer.ObjectsDeserialized} objects");
            console.SendText(player,$"largest serialized: {RobustSerializer.LargestObjectDeserializedBytes} bytes, {RobustSerializer.LargestObjectDeserializedType} objects");
        }

    }

    internal sealed class MemCommand : IClientCommand
    {
        public string Command => "mem";
        public string Description => "prints memory info";
        public string Help => "mem";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
#if !NETCOREAPP
            shell.SendText(player, "Memory info is only available on .NET Core");
#else
            var info = GC.GetGCMemoryInfo();

            shell.SendText(player, $@"Heap Size: {FormatBytes(info.HeapSizeBytes)} Total Allocated: {FormatBytes(GC.GetTotalMemory(false))}");
#endif
        }

#if NETCOREAPP
        private static string FormatBytes(long bytes)
        {
            return $"{bytes / 1024} KiB";
        }
#endif
    }
}
