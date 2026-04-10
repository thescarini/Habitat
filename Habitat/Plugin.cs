using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Habitat.Windows;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Habitat;

public class StaffMember
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string Role { get; set; } = "";
    public string Link { get; set; } = "";
    public bool Status { get; set; } = false;
}

public class Service
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Price { get; set; } = "";
    public string Description { get; set; } = "";
}

public class VisiblePlayer
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
}

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    //[PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/habitat";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Habitat");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    

    public bool IsPluginAvailable(string name)
    {
        foreach (var plug in PluginInterface.InstalledPlugins)
        {
            if (plug.InternalName == name && plug.IsLoaded)
                return true;
        }
        return false;
    }

    public List<StaffMember> StaffList { get; set; } = new();
    public List<Service> ServiceList { get; set; } = new();
    
    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    public bool LoadStaffFromCsv()
    {
        var dataDir = PluginInterface.AssemblyLocation.Directory?.FullName!;
        var filePath = Path.Combine(dataDir, "staff.csv");
        if (!File.Exists(filePath))
        {
            Log.Information($"{PluginInterface.Manifest.Name} staff.csv not found in {filePath}!");
            return false;
        }
        var lines = File.ReadAllLines(filePath);
        StaffList.Clear();
        foreach (var line in lines.Skip(1))
        {
            var parts = ParseCsvLine(line);
            if (parts.Count < 4)
                continue;
            StaffList.Add(new StaffMember
            {
                Name = parts[0].Trim(),
                World = parts[1].Trim(),
                Role = parts[2].Trim(),
                Link = parts[3].Trim(),
                Status = false
            });
        }
        Log.Information($"{PluginInterface.Manifest.Name} Stafflist loaded");
        return true;
    }
    public bool LoadServicesFromCsv()
    {
        var dataDir = PluginInterface.AssemblyLocation.Directory?.FullName!;
        var filePath = Path.Combine(dataDir, "services.csv");
        if (!File.Exists(filePath))
        {
            Log.Information($"{PluginInterface.Manifest.Name} services.csv not found in {filePath}!");
            return false;
        }
        var lines = File.ReadAllLines(filePath);
        ServiceList.Clear();
        foreach (var line in lines.Skip(1))
        {
            var parts = ParseCsvLine(line);
            if (parts.Count < 4)
                continue;
            ServiceList.Add(new Service
            {
                Name = parts[0].Trim(),
                Type = parts[1].Trim(),
                Price = parts[2].Trim(),
                Description = parts[3].Trim()
            });
        }
        Log.Information($"{PluginInterface.Manifest.Name} Services loaded");
        return true;
    }

/*    public List<VisiblePlayer> GetVisiblePlayers()
    {
        var players = new List<VisiblePlayer>();
        foreach (var obj in ObjectTable.PlayerObjects)
        {
            string playerName = "";
            string playerWorld = "";
            if (obj == null) continue;
            if (obj.ObjectKind != ObjectKind.Player) continue;
            var player = obj.Name.TextValue;
            if (player == null) continue;
            if (player.Contains('@'))
            {
                var split = player.Split('@');
                playerName = split[0];
                playerWorld = split.Length > 1 ? split[1] : "";
            }else
            {
                playerName = player;
                playerWorld = PlayerState.CurrentWorld.Value.Name.ToString();
            }
            players.Add(new VisiblePlayer
            {
                Name = playerName,
                World = playerWorld
            });
            Log.Information($"{PluginInterface.Manifest.Name} Seeing {playerName}@{playerWorld}");
        }
        Log.Information($"{PluginInterface.Manifest.Name} Visible Players Updated");
        return players;
    }*/
    
    public List<VisiblePlayer> GetVisiblePlayers()
    {
        var players = new List<VisiblePlayer>();
        foreach (var obj in ObjectTable.PlayerObjects)
        {
            string playerName = "";
            string playerWorld = "";
            if (obj == null) continue;
            if (obj is IPlayerCharacter player)
            {
                playerName = player.Name.TextValue;
                playerWorld = player.HomeWorld.Value.Name.ToString();
                players.Add(new VisiblePlayer
                {
                    Name = playerName,
                    World = playerWorld
                });
                Log.Information($"{PluginInterface.Manifest.Name} Seeing {playerName}@{playerWorld}");
            }
        }
        Log.Information($"{PluginInterface.Manifest.Name} Visible Players Updated");
        return players;
    }

    public void UpdateStaffStatus(List<VisiblePlayer> visiblePlayers)
    {
        foreach (var staff in StaffList)
        {
            staff.Status = visiblePlayers.Any(p =>
                p.Name.Equals(staff.Name, StringComparison.OrdinalIgnoreCase) &&
                p.World.Equals(staff.World, StringComparison.OrdinalIgnoreCase)
            );
        }
        Log.Information($"{PluginInterface.Manifest.Name} Staff Status Updated");
    }

    public Plugin()
    {
        Log.Information($"loading {PluginInterface.Manifest.Name}");
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        

        // You might normally want to embed resources and load them from the manifest stream
        var axoImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "axo1.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, axoImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open or closes Habitat Nightclub Plugin"
        });

        // Load playerstate and make it available
        //var playerState = Plugin.PlayerState;

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        
        LoadStaffFromCsv();
        LoadServicesFromCsv();
        Log.Information($"{PluginInterface.Manifest.Name} loaded");
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
