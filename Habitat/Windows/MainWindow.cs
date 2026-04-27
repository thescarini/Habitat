using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Habitat.Models;
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
            MinimumSize = new Vector2(800, 650),
            MaximumSize = new Vector2(800, 650)
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
        ImGui.Text("Search:");
        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##vipFilter", "Find a VIP", ref vipFilter, 100);
        ImGui.SameLine();
        ImGui.Checkbox("Show online only", ref showOnlineVipsOnly);
        ImGui.Spacing();
        if (ImGui.BeginTable("VipTable", 7,ImGuiTableFlags.ScrollY,new Vector2(0, size.Y)))
        {
            ImGui.TableSetupColumn("VIP Kind");
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Contact", ImGuiTableColumnFlags.WidthFixed, 65f * ImGui.GetIO().FontGlobalScale);
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
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(vip.Vip_kind);
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(vip.Character_name);
                ImGui.TableSetColumnIndex(2);
                if (isVisible)
                {
                    if (ImGui.SmallButton($"Contact##{vip.Character_name}_{vip.World}"))
                    {
                        plugin.SendTell($"Hi!", vip.Character_name, vip.World);
                    }
                }
                ImGui.TableSetColumnIndex(3);
                if (isVisible)
                {
                    ImGui.TextColored(HabitatStyle.Success, "available");
                }
                else
                {
                    ImGui.TextColored(HabitatStyle.TextMuted, "unavailable");
                }
                ImGui.TableSetColumnIndex(4);
                ImGui.TextUnformatted(vip.World);
                ImGui.TableSetColumnIndex(5);
                ImGui.TextUnformatted(vip.Discord_handle);
                ImGui.TableSetColumnIndex(6);
                ImGui.TextUnformatted(vip.Vip_since.ToString("yyyy-MM-dd"));
                
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
        return ImGui.Button(label);
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
        if (ImGui.Button(label) && enabled)
        {
            Dalamud.Utility.Util.OpenLink(url);
        }
        ImGui.EndDisabled();
        if (!enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(tooltip);
        }
    }

    private void TextfieldToClipboard(string text)
    {
        if (ImGui.Selectable(text))
            ImGui.SetClipboardText(text);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("copy to clipboard");
    }

    
    public override void Draw()
    {
        var availableWindowsize = ImGui.GetContentRegionAvail();
        float scale = ImGui.GetIO().FontGlobalScale;
        using var theme = HabitatStyle.PushTheme(scale);
        var footerHeight = 45f * scale;

        ImGui.BeginChild("MainWindow", new Vector2(0, -footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var habitatLogo = Plugin.TextureProvider.GetFromFile(habitatLogoPath).GetWrapOrDefault();
        if (habitatLogo != null)
        {
            ImGui.Image(habitatLogo.Handle, habitatLogo.Size / 6.0f * scale);
        }
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Welcome {plugin.localPlayer.Name} from {plugin.localPlayer.World}");
        ImGui.SameLine();
        RightAlignedText("Climate Change",60f * scale);
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
                    ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 110f * scale);
                    ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);

                    if (ImGui.Selectable("About Habitat", habitatMenu == 0))
                        habitatMenu = 0;

                    if (ImGui.Selectable("Staff", habitatMenu == 1))
                        habitatMenu = 1;

                    if (ImGui.Selectable("Services", habitatMenu == 2))
                        habitatMenu = 2;

                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScoll", new System.Numerics.Vector2(0, 0), false);

                    if (habitatMenu == 0)
                    {
                        ImGui.TextWrapped("Habitat is a semi-immersive 18+ FFXIV nightclub located in The Mist, Raiden server, Light Data Center. Every Friday, the venue comes alive as a full night experience built around music, interaction, and energy on the floor.");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Text("Location: (Light) Raiden, Mist W4 P4");
                        ImGui.Spacing();
                        ImGui.Text("Opening Time: Every Friday");
                        ImGui.BulletText("From 16:00 to 02:00 ST (Summer Time)");
                        ImGui.BulletText("From 17:00 to 03:00 ST (Winter Time)");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Text("Discord: "); ImGui.SameLine();
                        LinkText("https://discord.gg/habitatxiv", "https://discord.gg/habitatxiv");

                        ImGui.Text("Website: "); ImGui.SameLine();
                        LinkText("https://habitatnightclub.com", "https://habitatnightclub.com");

                        ImGui.Text("Instagram: "); ImGui.SameLine();
                        LinkText("https://www.instagram.com/habitatxiv", "https://www.instagram.com/habitatxiv");

                        ImGui.Text("Partake: "); ImGui.SameLine();
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
                        ImGui.Text("Staff role");
                        string[] dropdownItems = { "Venue Owners", "Bartenders", "Photographers", "VIP Hosts", "Dealers", "Receptionists", "Hypers", "Security", "Shout Runners"};
                        Dropdown("", dropdownItems, ref dropboxHabitatSelected);
                        ImGui.NewLine();
                        ListStaff(dropdownItems[dropboxHabitatSelected], false);
                    }

                    if (habitatMenu == 2)
                    {
                        ImGui.Text("Service Category:");
                        string[] dropdownItems = { "Bar Menu", "Chambers", "Games", "Photography"};
                        Dropdown("", dropdownItems, ref dropboxHabitatService);
                        ImGui.NewLine();
                        if (dropboxHabitatService == 0)
                        {
                            ImGui.Text("General:");
                            ListServices("bar");
                            ImGui.NewLine();
                            ImGui.Text("Habitat Classics:");
                            ListServices("bar_classics");
                            ImGui.NewLine();
                            ImGui.Text("Habitat Specials:");
                            ListServices("bar_specials");
                        }

                        if (dropboxHabitatService == 1)
                        {
                            ImGui.TextWrapped("Looking for a more private space during the night? Chambers are available for booking during opening hours.");
                            ImGui.NewLine();
                            ImGui.Text("Prices:");
                            ImGui.BulletText("400.000 Gil / Hour");
                            ImGui.BulletText("2.000.000 Gil / Full Night");
                            ImGui.NewLine();
                            ImGui.Text("Notes:");
                            ImGui.BulletText("Chambers are available while supplies last");
                            ImGui.BulletText("Access is valid for the selected duration only");
                            ImGui.NewLine();
                            if (RightAlignedButton("Rent a Room"))
                            {
                                plugin.SendTell("Hi! I like to rent a private chamber.", "Taniri Danolnith", "Raiden");
                            }
                            ImGui.SameLine();
                            if (RightAlignedButton("View Chambers", 130*scale))
                            {
                                Dalamud.Utility.Util.OpenLink("https://discord.com/channels/1180989907930468392/1188025857340604466");
                            }
                        }

                        if (dropboxHabitatService == 2)
                        {
                            ImGui.Text("Games");
                            ListServices("games");
                        }

                        if (dropboxHabitatService == 3)
                        {
                            ImGui.TextWrapped("Our sessions offer more than just a picture; they are a chance to express yourself, embrace your confidence, and capture the spark that makes you unforgettable.");
                            ImGui.NewLine();
                            ImGui.Text("Prices:");
                            ImGui.BulletText("SFW 200.000 Gil per Model");
                            ImGui.BulletText("NSFW 500.000 Gil per Model");
                        }

                    }
                    ImGui.EndChild();
                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
                        
            }

            if (ImGui.BeginTabItem("VIP Area"))
            {
                if (ImGui.BeginTable("VIP Area",2))
                {
                    if ((DateTime.UtcNow - lastCheckVip).TotalSeconds > 1)
                    {
                        lastCheckVip = DateTime.UtcNow;
                        plugin.UpdateLocalPlayerVip();
                    }
                    ImGui.TableSetupColumn("LeftSide", ImGuiTableColumnFlags.WidthFixed, 250f * scale);
                    ImGui.TableSetupColumn("RightSide", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    
                    ImGui.Text("Your current VIP Status:");
                    ImGui.SameLine();
                    if (plugin.localPlayer.IsVip)
                    {
                        ImGui.TextColored(HabitatStyle.Success, plugin.localPlayer.VipKind);
                    }
                    else
                    {
                        ImGui.TextColored(HabitatStyle.TextDim, "no VIP");
                    }
                    ImGui.Spacing();
                    if (ImGui.Button("Upgrade VIP Status"))
                    {
                        plugin.SendTell("Hi, im interested in an upgrade for my VIP status.", "Taniri Danolnith", "Raiden");
                    }
                    if (plugin.localPlayer.VipKind == "Lifetime VIP" || plugin.localPlayer.VipKind == "Booster VIP")
                    {
                        ImGui.NewLine();
                        ImGui.Text("VIP Syncshell");
                        ImGui.Text("Lightless Syncshell ID:");
                        TextfieldToClipboard("feetsniffa");
                        ImGui.Spacing();
                        ImGui.Text("Lightless Syncshell Password:");
                        TextfieldToClipboard("sniffinggoodfeet!");
                        ImGui.Spacing();
                        ImGui.TextColoredWrapped(HabitatStyle.Danger, "Habitat is not responsible for any issues that may occur while using the Syncshell.");
                    }
                    ImGui.NewLine();
                    if (plugin.localPlayer.IsVip)
                    {
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
                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScroll", new System.Numerics.Vector2(0, 0), false);
                    ImGui.Text("Active VIP Perks:");
                    ImGui.Spacing();

                    foreach (var perk in plugin.DataServiceVipPerks.Data)
                    {
                        ImGui.Bullet();
                        ImGui.SameLine();
                        bool allowed = plugin.IsPerkAllowed(perk);
                        if (allowed)
                        {
                            ImGui.TextColored(HabitatStyle.Success, perk.Perk_name);
                        }
                        else
                        {
                            ImGui.TextColored(HabitatStyle.TextDim, perk.Perk_name);
                        }
                    }
                    ImGui.EndChild();
                    ImGui.EndTable();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Gothika"))
            {
                if (ImGui.BeginTable("GothikaTable", 2))
                {
                    ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 110f * scale);
                    ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);

                    if (ImGui.Selectable("About Gothika", gothikaMenu == 0))
                        gothikaMenu = 0;

                    if (ImGui.Selectable("Staff", gothikaMenu == 1))
                        gothikaMenu = 1;

                    if (ImGui.Selectable("Services", gothikaMenu == 2))
                        gothikaMenu = 2;

                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScroll", new System.Numerics.Vector2(0, 0), false);
                    if (gothikaMenu == 0)
                    {
                        ImGui.TextWrapped("Gothika is a monthly descent into the realm of dark and shadow, curated by Habitat. A sanctuary for those who live and breathe goth, metal, and the deeper shades of alternative music.");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Text("Location: (Light) Shiva, Mist W29 P45");
                        ImGui.Spacing();
                        ImGui.Text("Opening Time: Monthly Saturdays");
                        ImGui.Text("- From 17:00 to 00:30 ST (Summer Time)");
                        ImGui.Text("- From 18:00 to 01:30 ST (Winter Time)");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Text("Discord: "); ImGui.SameLine();
                        LinkText("https://discord.gg/habitatxiv", "https://discord.gg/habitatxiv");

                        ImGui.Text("Website: "); ImGui.SameLine();
                        LinkText("https://habitatnightclub.com/gothika", "https://habitatnightclub.com/gothika");

                        ImGui.Text("Instagram: "); ImGui.SameLine();
                        LinkText("https://www.instagram.com/habitatxiv", "https://www.instagram.com/habitatxiv");

                        ImGui.Text("Partake: "); ImGui.SameLine();
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
                        ImGui.Text("Service Category:");
                        string[] dropdownItems = { "GPose Contest", "Phoenix Nights Bingo", "Phoenix Nights Blackjack", "Tarot Ceremony" };
                        Dropdown("", dropdownItems, ref dropboxGothikaService);
                        

                        if (dropboxGothikaService == 0)
                        {
                            ImGui.TextWrapped("Post your entry in the contests-entries channel before the deadline. Winners are chosen live during the night... be there to claim it.");
                            ImGui.Spacing();
                            ImGui.Text("Prizes:");
                            if (ImGui.BeginTable("Gothika Gpose",3))
                            {
                                ImGui.TableSetupColumn("1st Place", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("2nd Place", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn("3nd Place", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                ImGui.Text("1st Place");
                                ImGui.BulletText("3.000.000 Gil");
                                ImGui.BulletText("Discord Badge & Role");
                                ImGui.BulletText("Featured on next Event Visuals");
                                ImGui.TableSetColumnIndex(1);
                                ImGui.Text("2nd Place");
                                ImGui.BulletText("2.000.000 Gil");
                                ImGui.BulletText("Discord Badge & Role");
                                ImGui.TableSetColumnIndex(2);
                                ImGui.Text("3rd Place");
                                ImGui.BulletText("1.000.000 Gil");
                                ImGui.BulletText("Discord Badge & Role");
                            }
                            ImGui.EndTable();
                            ImGui.Spacing();
                            ImGui.Text("Rules:");
                            ImGui.BulletText("One entry per person. Group poses allowed. Modded and Vanilla welcome.");
                            ImGui.BulletText("NSFW is fine, if cloaked in spoilers");
                            ImGui.BulletText("Edits are welcome but let the truth of your Character remain.");
                            ImGui.BulletText("Light background animations are allowed as long as your Character remains the focus");
                            if (RightAlignedButton("Post Your Entry"))
                            {

                            }

                        }

                        if (dropboxGothikaService == 1)
                        {
                            ImGui.Text("Rules:");
                            ImGui.TextColored(HabitatStyle.Warning, "The goal is to cross out 5 numbers on your ticket.");
                            ImGui.Spacing();
                            ImGui.TextWrapped("Buy your ticket from the game host, who will provide a link (QR available for mobile). Once the game starts, numbers (1–25) are rolled in-game using /random and called in /yell. They will appear automatically on your ticket. If a number is repeated, it will be re-rolled.");
                            ImGui.TextWrapped("When you cross out 5 numbers, call “Bingo” in /yell to claim your win (other chats won’t count).");
                            ImGui.TextWrapped("Winning rules may vary. The prize may go to the first player or be split between multiple winners, depending on the round.");
                        }

                        if (dropboxGothikaService == 2)
                        {
                            ImGui.Text("Rules:");
                            ImGui.TextColored(HabitatStyle.Warning, "Get as close to 21 as possible without going over. Beat the dealer to win.");
                            ImGui.TextColored(HabitatStyle.Warning, "Busting (over 21) is an automatic loss.");
                            ImGui.Spacing();
                            ImGui.Text("Cards are dealt via /dice 13");
                            ImGui.BulletText("Ace = 1 or 11");
                            ImGui.BulletText("11-13 = 10");
                            ImGui.NewLine();
                            ImGui.Text("On your turn:");
                            ImGui.Spacing();
                            ImGui.Text("Hit: draw a card");
                            ImGui.Text("Stand: end your turn");
                            ImGui.Text("Double Down (first turn): double bet, draw once, then stand");
                            ImGui.Text("Split (matching cards): play two hands (extra bet required)");
                            ImGui.Spacing();
                            ImGui.Text("Dealer draws after all players, stopping at 16/17+ (rules may vary).");
                            ImGui.Text("5+ cards = Charlie (may grant bonus or auto-win depending on the table)");
                        }

                        if (dropboxGothikaService == 3)
                        {
                            ImGui.TextWrapped("Search for Answers. Away from the noise, the Tarot Lounge offers a moment to settle. You may bring a question, a feeling… or simply ask for a reading.");
                            ImGui.Spacing();
                            ImGui.Text("Rules:");
                            ImGui.Text("I - The Omen");
                            ImGui.TextWrapped("When you’re ready, The Oracle begins. A Major Arcana reading, quiet and intimate, shaped into a clear message.");
                            ImGui.Text("II - The Elixir");
                            ImGui.TextWrapped("As the reading unfolds, The Apothecary prepares your elixir.");
                            ImGui.Text("III - The Major Arcana");
                            ImGui.TextWrapped("The Tarot Ceremony is guided by the Major Arcana, the most powerful cards of the deck.");
                        }
                    }
                    


                    ImGui.EndChild();
                    ImGui.EndTable();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Merch & Venue Mods"))
            {
                ImGui.BeginChild("ContentScoll", new System.Numerics.Vector2(0, 0), false);
                ImGui.Text("Disclaimer");
                ImGui.NewLine();
                ImGui.TextWrapped("Take a look around! We’ve put together a collection of downloads to bring Habitat closer to you. From clothing, minions, and animations... to the venue interior pack for the closest possible experience. All downloads are available on our Discord server, feel free to explore, try things out, and make it your own.");
                ImGui.NewLine();
                ImGui.TextColoredWrapped(HabitatStyle.Danger, "By using modded content, you acknowledge that you are doing so at your own risk. Modded environments and third-party tools may be unstable and can lead to crashes, performance issues, or other unintended effects.");
                ImGui.NewLine();
                ImGui.Checkbox("I have read the disclaimer", ref merchDisclaimer);
                ImGui.NewLine();
                DisabledLinkButtonWithTooltip("Download Venue ModPack", "https://cdn.discordapp.com/attachments/1192513250877776014/1495188103873433762/Habitat_Furniture_Pack_v2.pmp?ex=69ea9bef&is=69e94a6f&hm=8668b5cf47a236e808cd6beaeb09478d52c23e1a997cd78e49c9038771265812&", "You must have read the disclaimer to proceed!", merchDisclaimer);
                ImGui.SameLine();
                DisabledLinkButtonWithTooltip("Discover our Free Merch", "https://discord.com/channels/1180989907930468392/1192513250877776014", "You must have read the disclaimer to proceed!", merchDisclaimer);
                ImGui.EndChild();
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
                    ImGui.Text("VIP List");
                    ImGui.Spacing();
                    DrawVipTable(plugin.GetVisiblePlayers());
                    ImGui.EndTabItem();
                }
            }
            

            ImGui.EndTabBar();
        }
        ImGui.EndChild();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("v0.8.0.4");
        ImGui.SameLine();
        if (plugin.IsPluginAvailable("Lifestream"))
        {
            if (RightAlignedButton("Teleport to Habitat"))
            {
                Plugin.CommandManager.ProcessCommand("/li Raiden Mist 4 4");
            }
            ImGui.SameLine();
            if (RightAlignedButton("Teleport to Gothika",150f * scale))
            {
                Plugin.CommandManager.ProcessCommand("/li Shiva Mist 29 45");
            }
        }
        else
        {
            ImGui.NewLine();
        }
    }
}
