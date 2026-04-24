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
    public LocalPlayer localPlayer { get; set; }
    private List<VisiblePlayer> cachedVisiblePlayers = new();
    private DateTime lastVisiblePlayersUpdate = DateTime.MinValue;
    private readonly TimeSpan visiblePlayersCacheDuration = TimeSpan.FromSeconds(1);

    public bool IsPluginAvailable(string name)
    {
        foreach (var plug in PluginInterface.InstalledPlugins)
        {
            if (plug.InternalName == name && plug.IsLoaded)
                return true;
        }
        return false;
    }

    public void UpdateLocalPlayerStaff()
    {
        if (localPlayer == null)
            return;

        if (DataServiceStaff == null || DataServiceStaff.Data == null)
        {
            localPlayer.IsStaff = false;
            localPlayer.IsStaffHead = false;
            localPlayer.StaffRole = string.Empty;
            return;
        }

        DataServiceStaff.EnsureData();

        var match = DataServiceStaff.Data.FirstOrDefault(x =>
            x.Character_name.Equals(localPlayer.Name, StringComparison.OrdinalIgnoreCase) &&
            x.World.Equals(localPlayer.World, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            localPlayer.IsStaff = true;
            localPlayer.StaffRole = match.Role ?? string.Empty;
            localPlayer.IsStaffHead = match.Head_staff;
        }
        else
        {
            localPlayer.IsStaff = false;
            localPlayer.IsStaffHead = false;
            localPlayer.StaffRole = string.Empty;
        }
    }

    public void UpdateLocalPlayerVip()
    {
        if (localPlayer == null)
            return;
        if (DataServiceVip == null || DataServiceVip.Data == null)
        {
            localPlayer.IsVip = false;
            localPlayer.VipKind = string.Empty;
            return;
        }

        DataServiceVip.EnsureData();
        var match = DataServiceVip.Data.FirstOrDefault(x =>
            x.Character_name.Equals(localPlayer.Name, StringComparison.OrdinalIgnoreCase) &&
            x.World.Equals(localPlayer.World, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            localPlayer.IsVip = true;
            localPlayer.VipKind = match.Vip_kind ?? string.Empty;
        }
        else
        {
            localPlayer.IsVip = false;
            localPlayer.VipKind = string.Empty;
        }
    }

    public List<VisiblePlayer> GetVisiblePlayers()
    {
        if (DateTime.Now - lastVisiblePlayersUpdate < visiblePlayersCacheDuration)
            return cachedVisiblePlayers;
        var players = new List<VisiblePlayer>();
        foreach (var obj in ObjectTable.PlayerObjects)
        {
            if (obj is not IPlayerCharacter player)
                continue;
            var playerName = player.Name.TextValue;
            var playerWorld = player.HomeWorld.Value.Name.ToString();
            players.Add(new VisiblePlayer
            {
                Name = playerName,
                World = playerWorld
            });
        }
        cachedVisiblePlayers = players;
        lastVisiblePlayersUpdate = DateTime.Now;
        return cachedVisiblePlayers;
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

        localPlayer = new LocalPlayer();
        localPlayer.Name = PlayerState.CharacterName;
        var playerHomeworldId = PlayerState.HomeWorld.RowId;
        if (DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>().TryGetRow(playerHomeworldId, out var playerHomeworld))
        {
            localPlayer.World = playerHomeworld.Name.ToString();
            Log.Information($"{PluginInterface.Manifest.Name} local player name and homeworld resolved");
        }
        else
        {
            Log.Information($"{PluginInterface.Manifest.Name} Error resolving player name and homeworld");
        }
        localPlayer.FullName = (localPlayer.Name + "@" + localPlayer.World);

        DataServiceVip = new SupabaseDataService<VipList>(
            supabaseProjectUrl,
            supabaseAnonKey,
            "vip_list",
            "character_name",
            "world",
            "vip_kind",
            "vip_since",
            "discord_handle"
            );

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
        Log.Information($"{PluginInterface.Manifest.Name} loaded");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CommandManager.RemoveHandler(CommandName);
        DataServiceVip.Dispose();
        DataServiceStaff.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
