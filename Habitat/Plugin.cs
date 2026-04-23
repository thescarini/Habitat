using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Habitat.Windows;
using Habitat.Models;
using Habitat.Services;
using Dalamud.Interface.Textures.TextureWraps;

namespace Habitat;


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
            
    private const string CommandName = "/habitat";

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("Habitat");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public List<Service> ServiceList { get; set; } = new();
    public SupabaseDataService<VipList> DataServiceVip { get; private set; }
    public SupabaseDataService<StaffMember> DataServiceStaff { get; private set; }
    public string PlayerFullName { get; private set; } = string.Empty;
    

    public bool IsPluginAvailable(string name)
    {
        foreach (var plug in PluginInterface.InstalledPlugins)
        {
            if (plug.InternalName == name && plug.IsLoaded)
                return true;
        }
        return false;
    }
    public string GetPlayerFullname()
    {
        var playerName = PlayerState.CharacterName;
        var playerHomeworldId = PlayerState.HomeWorld.RowId;
        if (DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>().TryGetRow(playerHomeworldId, out var playerHomeworld))
        {
            var playerFullName = playerName + "@" + playerHomeworld.Name.ToString();
            Log.Information($"{PluginInterface.Manifest.Name} local player name and homeworld resolved");
            return playerFullName;
        }
        Log.Information($"{PluginInterface.Manifest.Name} Error resolving player name and homeworld");
        return "Unknown";
    }

    public bool IsPlayerVip(string playerFullName)
    {
        DataServiceVip.EnsureData();
        return DataServiceVip.Data.Any(x =>
        string.Equals($"{x.Character_name}@{x.World}", playerFullName, StringComparison.OrdinalIgnoreCase));
    }

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
        DataServiceStaff.EnsureData();
        foreach (var staff in DataServiceStaff.Data)
        {
            staff.Status = visiblePlayers.Any(p =>
                p.Name.Equals(staff.Character_name, StringComparison.OrdinalIgnoreCase) &&
                p.World.Equals(staff.World, StringComparison.OrdinalIgnoreCase)
            );
        }
        Log.Information($"{PluginInterface.Manifest.Name} Staff Status Updated");
    }

    public Plugin()
    {
        Log.Information($"loading {PluginInterface.Manifest.Name}");
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var habitatLogoPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "habitat-composite-horizontal.png");
        const string supabaseProjectUrl = "https://eqczptcbtqqqutliurql.supabase.co";
        const string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVxY3pwdGNidHFxcXV0bGl1cnFsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY1MjY4MzQsImV4cCI6MjA5MjEwMjgzNH0.bDUMzHKCb-p2CpFvYWhjQ9jiqlxiqRHcShW615oYq5c";
        

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, habitatLogoPath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open or closes Habitat Nightclub Plugin"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        //LoadStaffFromCsv();
        //LoadServicesFromCsv();
        PlayerFullName = GetPlayerFullname();

        DataServiceVip = new SupabaseDataService<VipList>(
            supabaseProjectUrl,
            supabaseAnonKey,
            "vip_list",
            "character_name",
            "world",
            "vip_kind",
            "vip_since"
            );
        //DataServiceVip.EnsureData();

        DataServiceStaff = new SupabaseDataService<StaffMember>(
            supabaseProjectUrl,
            supabaseAnonKey,
            "staff",
            "character_name",
            "world",
            "role",
            "link",
            "hiatus",
            "head_staff",
            "is_habitat",
            "is_gothika",
            "gothika_role",
            "habitat_dropdown",
            "gothika_dropdown"
            );
        //DataServiceStaff.EnsureData();

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
        DataServiceVip.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
