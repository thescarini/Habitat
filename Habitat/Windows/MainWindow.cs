using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Habitat.Models;
using Habitat.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Habitat.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string habitatLogoPath;
    private readonly Plugin plugin;
    public bool climateChange = true;
    private int dropboxHabitatSelected = 0;
    private int dropboxGothikaSelected = 0;
    private int dropboxHabitatService = 0;
    private int dropboxGothikaService = 0;
    private int habitatMenu = 0;
    private int gothikaMenu = 0;
    private bool merchDisclaimer = false;


    public MainWindow(Plugin plugin, string habitatLogoPath)
        : base("Habitat Nightclub Plugin##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 715),
            MaximumSize = new Vector2(800, 715)
        };

        this.TitleBarButtons = new List<TitleBarButton>
        {
            new TitleBarButton
            {
                Icon = Dalamud.Interface.FontAwesomeIcon.Cog,
                IconOffset = new Vector2(2, 1),
                ShowTooltip = () => ImGui.SetTooltip("Settings"),
                Click = (button) =>
                {
                    plugin.ToggleConfigUi();
                },
                Priority = 0
            }
        };

        this.habitatLogoPath = habitatLogoPath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    private DateTime lastCheckStaff = DateTime.MinValue;
    private DateTime lastCheckVip = DateTime.MinValue;
    private DateTime lastCheckStaffPlayer = DateTime.MinValue;
    private string vipFilter = "";
    private bool showOnlineVipsOnly = false;


    private void LinkText(string text, string url)
    {
        ImGui.TextColored(HabitatStyle.Accent, text);
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

    private void ListServices(string servicetype, bool gothikamode = false)
    {
        var services = plugin.DataServiceServices.Data
            .Where(s => s.Type.Equals(servicetype, StringComparison.OrdinalIgnoreCase)
            && (gothikamode ? s.Is_gothika : s.Is_habitat))
            .OrderBy(s => s.Service_name)
            .ToList();

        if (ImGui.BeginTable($"Services ##{servicetype}", 3))
        {
            ImGui.TableSetupColumn("Service Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text(service.Service_name);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(service.Price);
                ImGui.TableSetColumnIndex(2);
                ImGui.TextWrapped(service.Description);
            }
            ImGui.EndTable();
        }
    }
    

    private void ListStaff(string role, bool gothikamode)
    {
        List<StaffMember> staff;
        if (gothikamode)
        {
            staff = plugin.DataServiceStaff.Data
            .Where(s => s.Gothika_dropdown.Contains(role, StringComparison.OrdinalIgnoreCase))
            //.OrderByDescending(s => s.Head_staff)
            .OrderBy(s => s.Character_name)
            .ToList();
        }
        else
        {
            staff = plugin.DataServiceStaff.Data
            .Where(s => s.Habitat_dropdown.Contains(role, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Head_staff)
            .ThenBy(s => s.Character_name)
            .ToList();
        }        
        
        if (ImGui.BeginTable(role, 5))
        {
            ImGui.TableSetupColumn("Role", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Contact", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Profile", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            for (int i = 0; i < staff.Count; i++)
            {
                var member = staff[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
                if (member.Head_staff && !gothikamode)
                {
                    ImGui.Text("Head ");
                    ImGui.SameLine();
                }
                if (!gothikamode) ImGui.Text(member.Role);
                if (gothikamode) ImGui.Text(member.Gothika_Role);
                ImGui.PopStyleVar();
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(member.Character_name);
                ImGui.TableSetColumnIndex(2);
                if (member.Status)
                {
                    ImGui.TextColored(HabitatStyle.Success, "available");
                }
                else
                {
                    ImGui.TextColored(HabitatStyle.TextMuted, "unavailable");
                }
                ImGui.TableSetColumnIndex(3);
                if (member.Status)
                {
                    if (ImGui.SmallButton($"Send a Tell##{member.Character_name}_{member.World}"))
                    {
                        plugin.SendTell("Hi! I'd like to request your service!", member.Character_name, member.World);
                    }
                }
                ImGui.TableSetColumnIndex(4);
                LinkText("Open Profile", member.Link);
            }
            ImGui.EndTable();
        }
    }

    public void DrawVipTable(List<VisiblePlayer> visiblePlayers)
    {
        var size = ImGui.GetContentRegionAvail();
        plugin.DataServiceVip.EnsureData();
        if (plugin.DataServiceVip?.Data == null)
            return;
        var visibleLookup = new HashSet<string>(
            visiblePlayers.Select(p => $"{p.Name}@{p.World}".ToLowerInvariant())
        );
        ImGui.TextColored(HabitatStyle.AccentHover, "Search:");
        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##vipFilter", "Find a VIP", ref vipFilter, 256);
        ImGui.SameLine();
        ImGui.Checkbox("Show visible only", ref showOnlineVipsOnly);
        ImGui.Spacing();
        var tableHeight = Math.Max(100f, size.Y);
        if (ImGui.BeginTable("VipTable", 6,ImGuiTableFlags.ScrollY,new Vector2(0, tableHeight)))
        {
            ImGui.TableSetupColumn("VIP Kind");
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            //ImGui.TableSetupColumn("Contact", ImGuiTableColumnFlags.WidthFixed, 65f * ImGui.GetIO().FontGlobalScale);
            ImGui.TableSetupColumn("Status");
            ImGui.TableSetupColumn("World");
            ImGui.TableSetupColumn("Discord", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("VIP Since");
            ImGui.TableHeadersRow();
            ImGui.TableSetupScrollFreeze(0, 1);
            var filter = vipFilter?.Trim();
            foreach (var vip in plugin.DataServiceVip.Data)
            {
                string key = $"{vip.Character_name}@{vip.World}".ToLowerInvariant();
                bool isVisible = visibleLookup.Contains(key);
                if (showOnlineVipsOnly && !isVisible)
                    continue;
                if (!string.IsNullOrEmpty(filter) &&
                    !vip.Character_name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                ImGui.TableNextRow();
                ImGui.PushID($"{vip.Character_name}@{vip.World}");
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(vip.Vip_kind);
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(vip.Character_name);
                /*ImGui.TableSetColumnIndex(2);
                if (isVisible)
                {
                    ImGui.PushID($"{vip.Character_name}@{vip.World}");
                    ImGui.BeginDisabled(!isVisible);
                    if (ImGui.SmallButton("Contact"))
                    {
                        plugin.SendTell($"Hi!", vip.Character_name, vip.World);
                    }
                    ImGui.EndDisabled();
                    ImGui.PopID();
                }*/
                ImGui.TableSetColumnIndex(2);
                if (isVisible)
                {
                    ImGui.TextColored(HabitatStyle.Success, "available");
                }
                else
                {
                    ImGui.TextColored(HabitatStyle.TextMuted, "not present");
                }
                ImGui.TableSetColumnIndex(3);
                ImGui.TextUnformatted(vip.World);
                ImGui.TableSetColumnIndex(4);
                ImGui.TextUnformatted(vip.Discord_handle);
                ImGui.TableSetColumnIndex(5);
                ImGui.TextUnformatted(vip.Vip_since.ToString("yyyy-MM-dd"));
                ImGui.PopID();
                
            }
            ImGui.EndTable();
        }
    }
        
    private void RightAlignedText(string text, float offset = 0f)
    {
        float avail = ImGui.GetContentRegionAvail().X;
        float textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - textWidth - offset);
        ImGui.Text(text);
    }

    private bool RightAlignedButton(string label, float offset = 0f)
    {
        var style = ImGui.GetStyle();
        float avail = ImGui.GetContentRegionAvail().X;
        float textWidth = ImGui.CalcTextSize(label).X;
        float buttonWidth = textWidth + style.FramePadding.X * 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail -  buttonWidth - offset);
        return DrawPrimaryButton(label, new Vector2(-1,0));
    }

    private bool DrawPrimaryButton(string label, Vector2 size)
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var style = HabitatStyle.PushPrimaryButtonStyle(scale);
        return ImGui.Button(label, size);
    }

    private bool DrawSecondaryButton(string label, Vector2 size)
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var style = HabitatStyle.PushSecondaryButtonStyle(scale);
        return ImGui.Button(label, size);
    }

    private static bool Dropdown(string label, string[] items, ref int selected)
    {
        bool changed = false;
        ImGui.SetNextItemWidth(150f * (ImGui.GetIO().FontGlobalScale));
        if (ImGui.BeginCombo(label, items[selected]))
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (ImGui.Selectable(items[i], selected == i))
                {
                    selected = i;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    private void DisabledLinkButtonWithTooltip(string label, string url, string tooltip, bool enabled)
    {
        ImGui.BeginDisabled(!enabled);
        if (DrawPrimaryButton(label, new Vector2(0,0)) && enabled)
        {
            Dalamud.Utility.Util.OpenLink(url);
        }
        ImGui.EndDisabled();
        if (!enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(tooltip);
        }
    }

    private bool DrawSidebarButton(string label, bool selected)
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var style = HabitatStyle.PushSidebarButtonStyle(selected, scale);
        var clicked = ImGui.Button(label, new Vector2(-1, 34f * scale));

        if (selected)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddLine(
                new Vector2(min.X + 8f * scale, min.Y + 7f * scale),
                new Vector2(min.X + 8f * scale, max.Y - 7f * scale),
                ImGui.GetColorU32(HabitatStyle.AccentHover),
                2f * scale);
        }

        return clicked;
    }

    public static void BulletColoredText(Vector4 bulletcolor, Vector4 textcolor, string text)
    {
        var style = ImGui.GetStyle();
        var scale = ImGui.GetIO().FontGlobalScale;
        ImGui.PushStyleColor(ImGuiCol.Text, bulletcolor);
        ImGui.Bullet();
        ImGui.PopStyleColor();
        ImGui.SameLine(0, HabitatStyle.ItemSpacingX * 2 * scale);
        ImGui.TextColored(textcolor, text);
    }

    private void TextfieldToClipboard(string text)
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using (HabitatStyle.PushFieldButtonStyle(scale))
        {
            if (ImGui.Button(text, new Vector2(-1, 0)))
            {
                ImGui.SetClipboardText(text);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("copy to clipboard");
        }
    }

    
    public override void Draw()
    {
        var availableWindowsize = ImGui.GetContentRegionAvail();
        float scale = ImGui.GetIO().FontGlobalScale;
        using var theme = HabitatStyle.PushTheme(scale);
        var footerHeight = 25f * scale;
        using (HabitatStyle.BeginPanel("MainBody", new Vector2(0, -footerHeight), scale, HabitatStyle.PanelRaised, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar))
        {
            var habitatLogo = Plugin.TextureProvider.GetFromFile(habitatLogoPath).GetWrapOrDefault();
            if (habitatLogo != null)
            {
                ImGui.Image(habitatLogo.Handle, habitatLogo.Size / 6.0f * scale);
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Welcome {plugin.localPlayer.Name} from {plugin.localPlayer.World}");
            ImGui.SameLine();
            RightAlignedText("Climate Change", 60f * scale);
            ImGui.SameLine();
            if (ImGuiComponents.ToggleButton("ClimateChangeButton", ref climateChange))
            {
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Climate Change Button changed to {climateChange}");
            }
            ImGui.Spacing();


            if (ImGui.BeginTabBar("MainTabs"))
            {
                if (ImGui.BeginTabItem("Habitat"))
                {
                    if (ImGui.BeginTable("HabitatTable", 2))
                    {
                        ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 140f * scale);
                        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);
                        using (HabitatStyle.BeginPanel("HabitatMenuPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {
                            if (habitatMenu == 0)
                            {
                                if (DrawSidebarButton("About Habitat", true))
                                    habitatMenu = 0;
                            }
                            else
                            {
                                if (DrawSidebarButton("About Habitat", false))
                                    habitatMenu = 0;
                            }
                            if (habitatMenu == 1)
                            {
                                if (DrawSidebarButton("Staff", true))
                                    habitatMenu = 1;
                            }
                            else
                            {
                                if (DrawSidebarButton("Staff", false))
                                    habitatMenu = 1;
                            }
                            if (habitatMenu == 2)
                            {
                                if (DrawSidebarButton("Services", true))
                                    habitatMenu = 2;
                            }
                            else
                            {
                                if (DrawSidebarButton("Services", false))
                                    habitatMenu = 2;
                            }
                            if (plugin.IsPluginAvailable("Lifestream"))
                            {
                                if (DrawSidebarButton("Teleport", false))
                                    Plugin.CommandManager.ProcessCommand("/li Raiden Mist 4 4");
                            }
                        }

                        ImGui.TableSetColumnIndex(1);
                        using (HabitatStyle.BeginPanel("HabitatPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {

                            if (habitatMenu == 0)
                            {
                                ImGui.TextColored(HabitatStyle.AccentHover, "About Habitat");
                                ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Habitat is a semi-immersive 18+ FFXIV nightclub located in The Mist, Raiden server, Light Data Center. Every Friday, the venue comes alive as a full night experience built around music, interaction, and energy on the floor.");
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Location");
                                ImGui.TextColored(HabitatStyle.TextDim, "Light - Raiden, Mist Ward 4 Plot 4");
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Opening");
                                ImGui.TextColored(HabitatStyle.TextDim, "Every Friday from 16:00 to 02:00 ST (Summer Time)");
                                ImGui.TextColored(HabitatStyle.TextDim, "or from 17:00 to 03:00 ST (Winter Time)");
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Habitat Links");
                                ImGui.TextColored(HabitatStyle.TextDim, "Discord:"); ImGui.SameLine();
                                LinkText("https://discord.gg/habitatxiv", "https://discord.gg/habitatxiv");

                                ImGui.TextColored(HabitatStyle.TextDim, "Website:"); ImGui.SameLine();
                                LinkText("https://habitatnightclub.com", "https://habitatnightclub.com");

                                ImGui.TextColored(HabitatStyle.TextDim, "Instagram:"); ImGui.SameLine();
                                LinkText("https://www.instagram.com/habitatxiv", "https://www.instagram.com/habitatxiv");

                                ImGui.TextColored(HabitatStyle.TextDim, "Partake:"); ImGui.SameLine();
                                LinkText("https://www.partake.gg/teams/757", "https://www.partake.gg/teams/757");
                            }

                            if (habitatMenu == 1)
                            {
                                if ((DateTime.UtcNow - lastCheckStaff).TotalSeconds > 1)
                                {
                                    lastCheckStaff = DateTime.UtcNow;
                                    var visiblePlayers = plugin.GetVisiblePlayers();
                                    plugin.UpdateStaffStatus(visiblePlayers);
                                }
                                ImGui.TextColored(HabitatStyle.AccentHover, "Staff role");
                                string[] dropdownItems = { "Venue Owners", "Bartenders", "Photographers", "VIP Hosts", "Dealers", "Receptionists", "Hypers", "Security", "Shout Runners" };
                                Dropdown("", dropdownItems, ref dropboxHabitatSelected);
                                ImGui.NewLine();
                                ListStaff(dropdownItems[dropboxHabitatSelected], false);
                            }

                            if (habitatMenu == 2)
                            {
                                ImGui.TextColored(HabitatStyle.AccentHover, "Service Category:");
                                string[] dropdownItems = { "Bar Menu", "Chambers", "Games", "Photography" };
                                Dropdown("", dropdownItems, ref dropboxHabitatService);
                                HabitatStyle.DrawDivider(scale);
                                if (dropboxHabitatService == 0)
                                {
                                    ImGui.TextColored(HabitatStyle.AccentHover, "General:");
                                    ListServices("bar");
                                    ImGui.NewLine();
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Habitat Classics:");
                                    ListServices("bar_classics");
                                    ImGui.NewLine();
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Habitat Specials:");
                                    ListServices("bar_specials");
                                }

                                if (dropboxHabitatService == 1)
                                {
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Looking for a more private space during the night? Chambers are available for booking during opening hours.");
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Prices:");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "400.000 Gil / Hour");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "2.000.000 Gil / Full Night");
                                    if (RightAlignedButton("Rent a Room"))
                                    {
                                        plugin.SendTell("Hi! I like to rent a private chamber.", "Taniri Danolnith", "Raiden");
                                    }
                                    HabitatStyle.DrawDivider(scale);
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Notes:");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Chambers are available while supplies last");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Access is valid for the selected duration only");
                                    if (RightAlignedButton("View Chambers"))
                                    {
                                        Dalamud.Utility.Util.OpenLink("https://discord.com/channels/1180989907930468392/1188025857340604466");
                                    }
                                }

                                if (dropboxHabitatService == 2)
                                {
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Games");
                                    ListServices("games");
                                }

                                if (dropboxHabitatService == 3)
                                {
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Our sessions offer more than just a picture; they are a chance to express yourself, embrace your confidence, and capture the spark that makes you unforgettable.");
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Prices:");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "SFW 200.000 Gil per Model");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "NSFW 500.000 Gil per Model");
                                    if (RightAlignedButton("Visit Gallery"))
                                    {
                                        Dalamud.Utility.Util.OpenLink("https://discord.com/channels/1180989907930468392/1182449343421222932");
                                    }
                                
                                }

                            }
                        }
                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();

                }

                if (ImGui.BeginTabItem("VIP Area"))
                {
                    if (ImGui.BeginTable("VIP Area", 2))
                    {
                        if ((DateTime.UtcNow - lastCheckVip).TotalSeconds > 1)
                        {
                            lastCheckVip = DateTime.UtcNow;
                            plugin.UpdateLocalPlayerVip();
                        }
                        ImGui.TableSetupColumn("LeftSide", ImGuiTableColumnFlags.WidthFixed, 280f * scale);
                        ImGui.TableSetupColumn("RightSide", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        using (HabitatStyle.BeginPanel("VIPLeftPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset, ImGuiWindowFlags.NoScrollbar))
                        {
                            ImGui.TextColored(HabitatStyle.AccentHover, "Your current VIP Status:");
                            ImGui.SameLine();
                            if (plugin.localPlayer.IsVip)
                            {
                                ImGui.TextColored(HabitatStyle.Success, plugin.localPlayer.VipKind);
                            }
                            else
                            {
                                ImGui.TextColored(HabitatStyle.TextDim, "no VIP");
                            }
                            if (DrawPrimaryButton("Upgrade VIP Status", new Vector2(-1,0)))
                            {
                                plugin.SendTell("Hi, im interested in an upgrade for my VIP status.", "Taniri Danolnith", "Raiden");
                            }
                            if (plugin.localPlayer.VipKind == "Lifetime VIP" || plugin.localPlayer.VipKind == "Booster VIP")
                            {
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "VIP Syncshell");
                                ImGui.TextColored(HabitatStyle.TextDim, "Lightless ID:");
                                TextfieldToClipboard("unavailable");
                                ImGui.Spacing();
                                ImGui.TextColored(HabitatStyle.TextDim, "Lightless Password:");
                                TextfieldToClipboard("waiting for lightless");
                                ImGui.Spacing();
                                ImGui.TextColoredWrapped(HabitatStyle.Danger, "Habitat is not responsible for any issues that may occur while using the Syncshell.");
                            }
                            HabitatStyle.DrawDivider(scale);
                            if (plugin.localPlayer.IsVip)
                            {
                                ImGui.TextColored(HabitatStyle.AccentHover, "Request VIP Services");
                                if (ImGui.Button("Request VIP Host"))
                                {
                                    plugin.SendTell("Hi, I'd like to request a VIP Host.", "Taniri Danolnith", "Raiden");
                                }
                                ImGui.SameLine();
                                if (ImGui.Button("Order a Drink"))
                                {
                                    plugin.SendTell("Hi, I'd like to request a Maid or Butler.", "Taniri Danolnith", "Raiden");
                                }
                            }
                        }
                        ImGui.TableSetColumnIndex(1);
                        using (HabitatStyle.BeginPanel("VIPRightPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {
                            ImGui.TextColored(HabitatStyle.AccentHover, "Active VIP Perks:");
                            ImGui.Spacing();

                            foreach (var perk in plugin.DataServiceVipPerks.Data
                                .OrderByDescending(p => plugin.IsPerkAllowed(p)))
                            {
                                bool allowed = plugin.IsPerkAllowed(perk);
                                if (allowed)
                                {
                                    BulletColoredText(HabitatStyle.Success, HabitatStyle.TextDim, perk.Perk_name);
                                }
                                else
                                {
                                    BulletColoredText(HabitatStyle.TextDim, HabitatStyle.TextDim, perk.Perk_name);
                                }
                            }
                        }
                        ImGui.EndTable();
                    }
                    ImGui.EndTabItem();
                }

                if ((DateTime.UtcNow - lastCheckStaffPlayer).TotalSeconds > 1)
                {
                    lastCheckStaffPlayer = DateTime.UtcNow;
                    plugin.UpdateLocalPlayerStaff();
                }
                if (plugin.localPlayer.IsStaff)
                {
                    if (ImGui.BeginTabItem("VIP List"))
                    {
                        using (HabitatStyle.BeginPanel("VIPListPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {
                            ImGui.TextColored(HabitatStyle.Warning, "This list is sometimes unstable and costs fps!");
                            ImGui.TextColored(HabitatStyle.Warning, "Only leave it open as long as you need it.");
                            HabitatStyle.DrawDivider(scale);
                            DrawVipTable(plugin.GetVisiblePlayers());
                        }
                        ImGui.EndTabItem();
                    }
                }

                if (ImGui.BeginTabItem("Gothika"))
                {
                    if (ImGui.BeginTable("GothikaTable", 2))
                    {
                        ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 140f * scale);
                        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);

                        using (HabitatStyle.BeginPanel("GothikaMenuPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {
                            if (gothikaMenu == 0)
                            {
                                if (DrawSidebarButton("About Gothika", true))
                                    gothikaMenu = 0;
                            }
                            else
                            {
                                if (DrawSidebarButton("About Gothika", false))
                                    gothikaMenu = 0;
                            }
                            if (gothikaMenu == 1)
                            {
                                if (DrawSidebarButton("Staff", true))
                                    gothikaMenu = 1;
                            }
                            else
                            {
                                if (DrawSidebarButton("Staff", false))
                                    gothikaMenu = 1;
                            }
                            if (gothikaMenu == 2)
                            {
                                if (DrawSidebarButton("Services", true))
                                    gothikaMenu = 2;
                            }
                            else
                            {
                                if (DrawSidebarButton("Services", false))
                                    gothikaMenu = 2;
                            }
                            if (plugin.IsPluginAvailable("Lifestream"))
                            {
                                if (DrawSidebarButton("Teleport", false))
                                    Plugin.CommandManager.ProcessCommand("/li Shiva Mist 29 45");
                            }
                        }
                        ImGui.TableSetColumnIndex(1);
                        using (HabitatStyle.BeginPanel("GothikaPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                        {
                            if (gothikaMenu == 0)
                            {
                                ImGui.TextColored(HabitatStyle.AccentHover, "About Gothika");
                                ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Gothika is a monthly descent into the realm of dark and shadow, curated by Habitat. ");
                                ImGui.TextColoredWrapped(HabitatStyle.TextDim, "A sanctuary for those who live and breathe goth, metal, and the deeper shades of alternative music.");
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Location");
                                ImGui.TextColored(HabitatStyle.TextDim, "Light - Shiva, Mist, Ward 29 Plot 45");
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Openings");
                                ImGui.TextColored(HabitatStyle.TextDim, "From 17:00 to 00:30 ST (Summer Time) / 18:00 to 01:30 ST (Winter Time)");
                                ImGui.Spacing();
                                if (ImGui.BeginTable("GothikaTimetable", 2)){
                                    ImGui.TableSetupColumn("left", ImGuiTableColumnFlags.WidthFixed, 200f * scale);
                                    ImGui.TableSetupColumn("right", ImGuiTableColumnFlags.WidthStretch);
                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "February, 28th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "April, 11th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "June, 18th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "August, 29th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "October, 24th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "December, 19th");
                                    ImGui.TableNextColumn();
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "March, 14th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "May, 09th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "July, 18th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "September, 26th");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "November, 21th");
                                    ImGui.EndTable();
                                }
                                HabitatStyle.DrawDivider(scale);
                                ImGui.TextColored(HabitatStyle.AccentHover, "Gothika Links");
                                ImGui.TextColored(HabitatStyle.TextDim, "Discord:"); ImGui.SameLine();
                                LinkText("https://discord.gg/habitatxiv", "https://discord.gg/habitatxiv");

                                ImGui.TextColored(HabitatStyle.TextDim, "Website:"); ImGui.SameLine();
                                LinkText("https://habitatnightclub.com/gothika", "https://habitatnightclub.com/gothika");

                                ImGui.TextColored(HabitatStyle.TextDim, "Instagram:"); ImGui.SameLine();
                                LinkText("https://www.instagram.com/habitatxiv", "https://www.instagram.com/habitatxiv");

                                ImGui.TextColored(HabitatStyle.TextDim, "Partake: "); ImGui.SameLine();
                                LinkText("https://www.partake.gg/teams/1020", "https://www.partake.gg/teams/1020");
                            }

                            if (gothikaMenu == 1)
                            {
                                if ((DateTime.UtcNow - lastCheckStaff).TotalSeconds > 1)
                                {
                                    lastCheckStaff = DateTime.UtcNow;
                                    var visiblePlayers = plugin.GetVisiblePlayers();
                                    plugin.UpdateStaffStatus(visiblePlayers);
                                }
                                ImGui.Text("Staff role");
                                string[] dropdownItems = { "Floor Hosts", "Photographers", "Tarot Ceremony", "Security", "Shout Runners" };
                                Dropdown("", dropdownItems, ref dropboxGothikaSelected);
                                ImGui.NewLine();
                                ListStaff(dropdownItems[dropboxGothikaSelected], true);
                            }

                            if (gothikaMenu == 2)
                            {
                                ImGui.TextColored(HabitatStyle.AccentHover, "Service Category:");
                                string[] dropdownItems = { "GPose Contest", "Phoenix Nights Bingo", "Phoenix Nights Blackjack", "Tarot Ceremony" };
                                Dropdown("", dropdownItems, ref dropboxGothikaService);
                                HabitatStyle.DrawDivider(scale);


                                if (dropboxGothikaService == 0)
                                {
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Post your entry in the contests-entries channel before the deadline. Winners are chosen live during the night... be there to claim it.");
                                    ImGui.Spacing();
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Prizes:");
                                    if (ImGui.BeginTable("Gothika Gpose1", 1))
                                    {
                                        ImGui.TableSetupColumn("1st Place", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableNextRow();
                                        ImGui.TableSetColumnIndex(0);
                                        ImGui.TextColored(HabitatStyle.AccentHover, "1st Place");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "3.000.000 Gil");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Discord Badge & Role");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Featured on next Event Visuals");
                                        ImGui.EndTable();
                                    }
                                    if (ImGui.BeginTable("Gothika Gpose2", 2))
                                    {
                                        
                                        ImGui.TableSetupColumn("2nd Place", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableSetupColumn("3nd Place", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableNextRow();
                                        ImGui.TableSetColumnIndex(0);
                                        ImGui.TextColored(HabitatStyle.AccentHover, "2nd Place");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "2.000.000 Gil");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Discord Badge & Role");
                                        ImGui.TableSetColumnIndex(1);
                                        ImGui.TextColored(HabitatStyle.AccentHover, "3rd Place");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "1.000.000 Gil");
                                        BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Discord Badge & Role");
                                    }
                                    ImGui.EndTable();
                                    ImGui.Spacing();
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Rules:");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "One entry per person. Group poses allowed. Modded and Vanilla welcome.");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "NSFW is fine, if cloaked in spoilers");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Edits are welcome but let the truth of your Character remain.");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Light background animations are allowed as long as your Character remains the focus");
                                    if (RightAlignedButton("Post Your Entry"))
                                    {

                                    }
                                }
                                if (dropboxGothikaService == 1)
                                {
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Rules:");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "The goal is to cross out 5 numbers on your ticket.");
                                    ImGui.Spacing();
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Buy your ticket from the game host, who will provide a link (QR available for mobile). Once the game starts, numbers (1–25) are rolled in-game using /random and called in /yell. They will appear automatically on your ticket. If a number is repeated, it will be re-rolled.");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "When you cross out 5 numbers, call “Bingo” in /yell to claim your win (other chats won’t count).");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Winning rules may vary. The prize may go to the first player or be split between multiple winners, depending on the round.");
                                }

                                if (dropboxGothikaService == 2)
                                {
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Rules:");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Get as close to 21 as possible without going over. Beat the dealer to win.");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Busting (over 21) is an automatic loss.");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Cards are dealt via /dice 13");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "Ace = 1 or 11");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "11-13 = 10");
                                    ImGui.NewLine();
                                    ImGui.TextColored(HabitatStyle.AccentHover, "On your turn:");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Hit: draw a card");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Stand: end your turn");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Double Down (first turn): double bet, draw once, then stand");
                                    ImGui.TextColored(HabitatStyle.TextDim, "Split (matching cards): play two hands (extra bet required)");
                                    ImGui.Spacing();
                                    ImGui.TextColored(HabitatStyle.TextDim, "Dealer draws after all players, stopping at 16/17+ (rules may vary).");
                                    ImGui.TextColored(HabitatStyle.TextDim, "5+ cards = Charlie (may grant bonus or auto-win depending on the table)");
                                }

                                if (dropboxGothikaService == 3)
                                {
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Search for Answers. Away from the noise, the Tarot Lounge offers a moment to settle. You may bring a question, a feeling… or simply ask for a reading.");
                                    ImGui.TextColored(HabitatStyle.AccentHover, "Rules:");
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "I - The Omen");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "When you’re ready, The Oracle begins. A Major Arcana reading, quiet and intimate, shaped into a clear message.");
                                    ImGui.Spacing();
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "II - The Elixir");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "As the reading unfolds, The Apothecary prepares your elixir.");
                                    ImGui.Spacing();
                                    BulletColoredText(HabitatStyle.AccentHover, HabitatStyle.TextDim, "III - The Major Arcana");
                                    ImGui.TextColoredWrapped(HabitatStyle.TextDim, "The Tarot Ceremony is guided by the Major Arcana, the most powerful cards of the deck.");
                                }
                            }
                        }
                        ImGui.EndTable();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Merch & Venue Mods"))
                {
                    using (HabitatStyle.BeginPanel("MerchPanel", new Vector2(0, 0), scale, HabitatStyle.PanelInset))
                    {
                        ImGui.TextColored(HabitatStyle.AccentHover, "Merch & Venue Mods");
                        ImGui.TextColoredWrapped(HabitatStyle.TextDim, "Take a look around! We’ve put together a collection of downloads to bring Habitat closer to you. ");
                        ImGui.TextColoredWrapped(HabitatStyle.TextDim, "From clothing, minions, and animations... to the venue interior pack for the closest possible experience. All downloads are available on our Discord server, feel free to explore, try things out, and make it your own.");
                        ImGui.NewLine();
                        ImGui.TextColored(HabitatStyle.AccentHover, "Disclaimer");
                        ImGui.TextColoredWrapped(HabitatStyle.Danger, "By using modded content, you acknowledge that you are doing so at your own risk. Modded environments and third-party tools may be unstable and can lead to crashes, performance issues, or other unintended effects.");
                        ImGui.Spacing();
                        ImGui.Checkbox("I have read the disclaimer", ref merchDisclaimer);
                        DisabledLinkButtonWithTooltip("Download Venue ModPack", "https://cdn.discordapp.com/attachments/1192513250877776014/1495188103873433762/Habitat_Furniture_Pack_v2.pmp?ex=69ea9bef&is=69e94a6f&hm=8668b5cf47a236e808cd6beaeb09478d52c23e1a997cd78e49c9038771265812&", "You must have read the disclaimer to proceed!", merchDisclaimer);
                        ImGui.SameLine();
                        DisabledLinkButtonWithTooltip("Discover our Free Merch", "https://discord.com/channels/1180989907930468392/1192513250877776014", "You must have read the disclaimer to proceed!", merchDisclaimer);
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        
        ImGui.TextColored(HabitatStyle.TextDim, "v0.9.2.0");


        //ImGui.SameLine();
        /*if (plugin.IsPluginAvailable("Lifestream"))
        {
            if (ImGui.BeginTable("FooterTable", 3))
            {
                ImGui.TableSetupColumn("empty", ImGuiTableColumnFlags.WidthFixed, 400f * scale);
                ImGui.TableSetupColumn("Button1", ImGuiTableColumnFlags.WidthFixed, 200f * scale);
                ImGui.TableSetupColumn("Button2", ImGuiTableColumnFlags.WidthFixed, 200f * scale);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(HabitatStyle.TextDim, "v0.9.0.0");
                ImGui.TableSetColumnIndex(1);
                if (RightAlignedButton("Teleport to Habitat"))
                {
                    Plugin.CommandManager.ProcessCommand("/li Raiden Mist 4 4");
                }
                ImGui.TableSetColumnIndex(2);
                if (RightAlignedButton("Teleport to Gothika"))
                {
                    Plugin.CommandManager.ProcessCommand("/li Shiva Mist 29 45");
                }
                ImGui.EndTable();
                
            }
        }*/
    }
}
