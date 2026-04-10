using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.ContentFinderConditionInterface.Delegates;

namespace Habitat.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string axoImagePath;
    private readonly Plugin plugin;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string axoImagePath)
        : base("Habitat##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.axoImagePath = axoImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    private DateTime lastCheck = DateTime.MinValue;

    private void LinkText(string text, string url)
    {
        ImGui.TextColored(new Vector4(0.2f, 0.6f, 1f, 1f), text);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip(url);
        }
        if (ImGui.IsItemClicked())
        {
            Dalamud.Utility.Util.OpenLink(url);
        }
    }

    private void ListStaff(string role)
    {
        var staff = plugin.StaffList.Where(s => s.Role.Contains(role, StringComparison.OrdinalIgnoreCase)).ToList();
        if (ImGui.BeginTable(role, 4))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Role", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Profile", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            for (int i = 0; i < staff.Count; i++)
            {
                var member = staff[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(1);
                ImGui.Selectable($"##row{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                bool isHovered = ImGui.IsItemHovered();
                if (isHovered)
                {
                    uint color = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.6f, 0.4f));
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                    //ImGui.SetTooltip();
                }
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(member.Name);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(member.Role);
                ImGui.TableSetColumnIndex(2);
                if (member.Status)
                {
                    ImGui.TextColored(new Vector4(0,1,0,1), "available");
                }
                else
                {
                    ImGui.TextColored(new Vector4(1,0,0,1), "unavailable");
                }
                ImGui.TableSetColumnIndex(3);
                LinkText("Open Profile", member.Link);
                ImGui.SameLine();
                if (member.Status)
                {
                    if (ImGui.Button("Contact"))
                    {
                        Plugin.CommandManager.ProcessCommand($"/t {member.Name}@{member.World} Hi!");
                    }
                }
            }
            ImGui.EndTable();
        }
    }


    private void ListService(string type)
    {
        var services = plugin.ServiceList.Where(s => s.Type == type).ToList();
        if (ImGui.BeginTable(type, 3))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(1);
                ImGui.Selectable($"##row{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                bool isHovered = ImGui.IsItemHovered();
                if (isHovered)
                {
                    uint color = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.6f, 0.4f));
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                    ImGui.SetTooltip(service.Description);
                }
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(service.Name);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(service.Price);
            }
            ImGui.EndTable();
        }
    }

    public override void Draw()
    {
        var footerHeight = 85f;
        var availableWindowsize = ImGui.GetContentRegionAvail();
        var player = Plugin.PlayerState;
        //ImGui.Text($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        ImGui.Text("HABITAT NIGHTCLUB");
        ImGui.Text("Welcome");
        ImGui.SameLine();
        ImGui.Text(player.CharacterName);
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 1f, 0, 1f), "(VIP)");
        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("BasicInfo", new Vector2(0, availableWindowsize.Y - footerHeight), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                /*
                ImGuiHelpers.ScaledDummy(20.0f);
                */

                if (ImGui.BeginTabBar("MainTabs"))
                {
                    if (ImGui.BeginTabItem("General"))
                    {
                        ImGui.Text("We never sleep on fridays!");
                        ImGui.Text("Open every Friday on Light Raiden at Mist W4 P4");
                        ImGui.SameLine();
                        if (plugin.IsPluginAvailable("Lifestream"))
                        {
                            if (ImGui.Button("LS Shuttleservice"))
                            {
                                Plugin.CommandManager.ProcessCommand("/li Raiden Mist 4 4");
                            }
                        }else
                        {
                            ImGui.NewLine();
                        }
                        
                        ImGui.Text("Discord: ");
                        ImGui.SameLine();
                        LinkText("https://discord.gg/habitatxiv", "https://discord.gg/habitatxiv");
                        ImGui.Text("Website: ");
                        ImGui.SameLine();
                        LinkText("https://habitatnightclub.com", "https://habitatnightclub.com");
                        ImGui.Text("Partake: ");
                        ImGui.SameLine();
                        LinkText("https://www.partake.gg/teams/757", "https://www.partake.gg/teams/757");
                        ImGui.Text("Instagram: ");
                        ImGui.SameLine();
                        LinkText("https://www.instagram.com/habitatxiv", "https://www.instagram.com/habitatxiv");
                        ImGui.Text("YouTube Music: ");
                        ImGui.SameLine();
                        LinkText("https://music.youtube.com/channel/UChKlNuZ_VoASK5ioYPIwuyQ", "https://music.youtube.com/channel/UChKlNuZ_VoASK5ioYPIwuyQ");
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Staff"))
                    {
                        if ((DateTime.UtcNow - lastCheck).TotalSeconds > 60)
                        {
                            lastCheck = DateTime.UtcNow;
                            var visiblePlayers = plugin.GetVisiblePlayers();
                            plugin.UpdateStaffStatus(visiblePlayers);
                        }
                        if (ImGui.CollapsingHeader("Owner"))
                        {
                            ListStaff("Owner");
                        }
                        if (ImGui.CollapsingHeader("Bartender"))
                        {
                            ListStaff("Bartender");
                        }
                        if (ImGui.CollapsingHeader("Photographer"))
                        {
                            ListStaff("Photographer");
                        }
                        if (ImGui.CollapsingHeader("Courtesans"))
                        {
                            ListStaff("Courtesan");
                        }
                        if (ImGui.CollapsingHeader("Gamba Dealer"))
                        {
                            ListStaff("Gamba Dealer");
                        }
                        if (ImGui.CollapsingHeader("Hyper"))
                        {
                            ListStaff("Hyper");
                        }
                        if (ImGui.CollapsingHeader("Receptionist"))
                        {
                            ListStaff("Receptionist");
                        }
                        if (ImGui.CollapsingHeader("Security"))
                        {
                            ListStaff("Security");
                        }
                        if (ImGui.CollapsingHeader("Shouter"))
                        {
                            ListStaff("Shouter");
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Services"))
                    {
                        if (ImGui.CollapsingHeader("Bar Menu"))
                        {
                            ListService("barmenu");
                        }
                        if(ImGui.CollapsingHeader("Companionship"))
                        {
                            ListService("companionship");
                        }
                        if (ImGui.CollapsingHeader("Photography"))
                        {
                            ListService("photography");
                        }
                        if (ImGui.CollapsingHeader("Private Events"))
                        {
                            ListService("privateevent");
                        }
                        if (ImGui.CollapsingHeader("VIP Services"))
                        {
                            ListService("vip");
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Resident DJs"))
                    {
                        ImGui.Text("Resident DJs");
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Gothika"))
                    {
                        ImGui.Text("Gothika");
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Free Merch"))
                    {
                        ImGui.Text("Free Merch");
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabItem();
                }


                /*
                // Example for other services that Dalamud provides.
                // PlayerState provides a wrapper filled with information about the player character.
                var playerState = Plugin.PlayerState;
                if (!playerState.IsLoaded)
                {
                    ImGui.Text("Our local player is currently not logged in.");
                    return;
                }
                
                if (!playerState.ClassJob.IsValid)
                {
                    ImGui.Text("Our current job is currently not valid.");
                    return;
                }
                
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Current job:");
                
                // Scaling hardcoded pixel values is important, as otherwise users with HUD scales above or below 100%
                // won't be able to see everything.
                ImGui.SameLine(120 * ImGuiHelpers.GlobalScale);
                
                // Get the icon id from a known offset + the class jobs id
                var jobIconId = 62100 + playerState.ClassJob.RowId;
                var iconTexture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(jobIconId)).GetWrapOrEmpty();
                ImGui.Image(iconTexture.Handle, new Vector2(28, 28) * ImGuiHelpers.GlobalScale);
                
                ImGui.SameLine();
                
                // If you want to see the Macro representation of this SeString use `.ToMacroString()`
                // More info about SeStrings: https://dalamud.dev/plugin-development/sestring/
                ImGui.Text(playerState.ClassJob.Value.Abbreviation.ToString());
                
                ImGui.SameLine();
                ImGui.Text($" [Level {playerState.Level}]");
                
                // Example for querying Lumina, getting the name of our current area.
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.Text($"Current location:");
                    ImGui.SameLine(120 * ImGuiHelpers.GlobalScale);
                    ImGui.Text(territoryRow.PlaceName.Value.Name.ToString());
                }
                else
                {
                    ImGui.Text("Invalid territory.");
                }
                */
            }
        }
        var axoImage = Plugin.TextureProvider.GetFromFile(axoImagePath).GetWrapOrDefault();
        if (axoImage != null)
        {
            //ImGui.Image(axoImage.Handle, axoImage.Size);
        }
        else
        {
            ImGui.Text("AxoImage missing!");
        }
        //ImGui.SameLine();
        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }
        ImGui.Spacing();
        var territoryId = Plugin.ClientState.TerritoryType;
        if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
        {
            ImGui.Text($"Current location:");
            ImGui.SameLine(120 * ImGuiHelpers.GlobalScale);
            ImGui.Text(territoryRow.PlaceName.Value.Name.ToString());
        }
        else
        {
            ImGui.Text("Invalid territory.");
        }
    }
}
