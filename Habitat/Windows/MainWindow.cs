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
    private int dropboxSelected = 0;
    private int habitatMenu = 0;
    private int gothikaMenu = 0;
    private bool merchDisclaimer = false;


    public MainWindow(Plugin plugin, string habitatLogoPath)
        : base("Habitat Nightclub Plugin##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(650, 500),
            MaximumSize = new Vector2(650, 500)
                //MathF.Min (ImGui.GetMainViewport().Size.X * 0.9f, 1600f),
                //MathF.Min (ImGui.GetMainViewport().Size.Y * 0.9f, 1000f))
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

    private void ListStaff(string role, bool gothikamode)
    {
        foreach (var s in plugin.DataServiceStaff.Data)
        {
            if (s == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} An entry of DataServiceStaff.Data is NULL!");
            if (s.Link == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Link NULL for {s.Character_name}");
            if (s.Hiatus == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Hiatus NULL for {s.Character_name}");
            if (s.Head_staff == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Head_staff NULL for {s.Character_name}");
            if (gothikamode && s.Is_Gothika == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Is_gothika NULL for {s.Character_name}");
            if (gothikamode && s.Gothika_Role == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Gothika_roll NULL for {s.Character_name}");
            if (gothikamode && s?.Gothika_dropdown == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Gothika_dropdown NULL for {s.Character_name}");
            if (!gothikamode && s?.Habitat_dropdown == null)
                Log.Information($"{Plugin.PluginInterface.Manifest.Name} Habitat_dropdown NULL for {s.Character_name}");
        }
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
                    ImGui.TextColored(new Vector4(0,1,0,1), "available");
                }
                else
                {
                    ImGui.TextDisabled("unavailable");
                }
                ImGui.TableSetColumnIndex(3);
                if (member.Status)
                {
                    if (ImGui.SmallButton("Send a Tell"))
                    {
                        Log.Information($"{Plugin.PluginInterface.Manifest.Name} Contact Button for {member.Character_name}@{member.World} clicked!");
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
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Contact");
            ImGui.TableSetupColumn("Status");
            ImGui.TableSetupColumn("World");
            ImGui.TableSetupColumn("Discord");
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
                    if (ImGui.SmallButton("Contact"))
                    {
                        // send tell?
                    }
                }
                ImGui.TableSetColumnIndex(3);
                if (isVisible)
                {
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "available");
                }
                else
                {
                    ImGui.TextDisabled("unavailable");
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
        var footerHeight = 30f * scale;

        ImGui.BeginChild("MainWindow", new Vector2(0, -footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        // --- Header ---
        var habitatLogo = Plugin.TextureProvider.GetFromFile(habitatLogoPath).GetWrapOrDefault();
        if (habitatLogo != null)
        {
            ImGui.Image(habitatLogo.Handle, habitatLogo.Size / 7.0f * scale);
        }
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Welcome {plugin.localPlayer.Name} from {plugin.localPlayer.World}");
        ImGui.SameLine();
        RightAlignedText("Climate Change",50f * scale);
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

                    // --- Sidebar ---
                    ImGui.TableSetColumnIndex(0);

                    if (ImGui.Selectable("About Habitat", habitatMenu == 0))
                        habitatMenu = 0;

                    if (ImGui.Selectable("Staff", habitatMenu == 1))
                        habitatMenu = 1;

                    if (ImGui.Selectable("Services", habitatMenu == 2))
                        habitatMenu = 2;

                    // --- Content ---
                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScoll", new System.Numerics.Vector2(0, 0), false);

                    if (habitatMenu == 0)
                    {
                        ImGui.TextWrapped("Habitat is a semi-immersive 18+ FFXIV nightclub located in The Mist, Raiden server, Light Data Center. Every Friday, the venue comes alive as a full night experience built around music, interaction, and energy on the floor.");
                        ImGui.NewLine();
                        ImGui.Separator();
                        ImGui.NewLine();
                        ImGui.Text("Location: (Light) Raiden, Mist W4 P4");
                        ImGui.NewLine();
                        ImGui.Text("Opening Time: Every Friday");
                        ImGui.Text("- From 16:00 to 02:00 ST (Summer Time)");
                        ImGui.Text("- From 17:00 to 03:00 ST (Winter Time)");
                        ImGui.NewLine();
                        ImGui.Separator();
                        ImGui.NewLine();
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
                        Dropdown("", dropdownItems, ref dropboxSelected);
                        ImGui.NewLine();
                        ListStaff(dropdownItems[dropboxSelected], false);
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
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), plugin.localPlayer.VipKind);
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "no VIP");
                    }
                    ImGui.Spacing();
                    if (ImGui.Button("Upgrade VIP Status"))
                    {
                        Log.Information($"{Plugin.PluginInterface.Manifest.Name} Upgrade VIP Status clicked!");
                    }
                    if (plugin.localPlayer.VipKind == "Lifetime VIP" || plugin.localPlayer.VipKind == "Booster VIP")
                    {
                        ImGui.NewLine();
                        ImGui.Text("VIP Syncshell");
                        ImGui.Spacing();
                        ImGui.Text("Lightless Syncshell ID:");
                        TextfieldToClipboard("feetsniffa");
                        ImGui.Spacing();
                        ImGui.Text("Lightless Syncshell Password:");
                        TextfieldToClipboard("sniffinggoodfeet!");
                        ImGui.NewLine();
                        ImGui.TextColoredWrapped(new Vector4(1, 0, 0, 1), "Habitat is not responsible for any issues that may occur while using the Syncshell.");
                    }
                    ImGui.NewLine();
                    if (ImGui.Button("Request VIP Host"))
                    {
                        Log.Information($"{Plugin.PluginInterface.Manifest.Name} VIP Host requested");
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Order a Drink"))
                    {
                        Log.Information($"{Plugin.PluginInterface.Manifest.Name} VIP Order a Drink");
                    }
                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScoll", new System.Numerics.Vector2(0, 0), false);
                    ImGui.Text("Active VIP Perks: (not working yet)");
                    ImGui.Spacing();

                    ImGui.BulletText("Access to Ocean's Edge");
                    ImGui.BulletText("Access to the Second Floor Reserved Area");
                    ImGui.BulletText("VIP Area Drink Service (Maid/Butler Services)");
                    ImGui.BulletText("VIP Area Dedicated Hosts");
                    ImGui.BulletText("VIP CWLS and Syncshell");
                    ImGui.BulletText("Server VIP Role");
                    ImGui.BulletText("Server Lifetime VIP Role");
                    ImGui.BulletText("5 Free Drinks per Night (Habitat Classics)");
                    ImGui.BulletText("1 Free Redeemable Scratchcard per Night");
                    ImGui.BulletText("Line Skip (When the Venue is Capped)");
                    ImGui.BulletText("Priority Queue for Photography Commissions");
                    ImGui.BulletText("Buy 20 Scratchcards Get 2 Bonus Scratchcards");
                    ImGui.BulletText("Ripple Shots Limit Increased From 20 To 30");
                    ImGui.BulletText("Raffle Tickets Limit Increased From 40 To 50");
                    ImGui.BulletText("Chance to unlock a Private Chamber for the full night (Up to 6 Guests)");
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

                    // --- Sidebar ---
                    ImGui.TableSetColumnIndex(0);

                    if (ImGui.Selectable("About Gothika", gothikaMenu == 0))
                        gothikaMenu = 0;

                    if (ImGui.Selectable("Staff", gothikaMenu == 1))
                        gothikaMenu = 1;

                    if (ImGui.Selectable("Services", gothikaMenu == 2))
                        gothikaMenu = 2;

                    // --- Content ---
                    ImGui.TableSetColumnIndex(1);
                    ImGui.BeginChild("ContentScoll", new System.Numerics.Vector2(0, 0), false);
                    if (gothikaMenu == 0)
                    {
                        ImGui.TextWrapped("Gothika is a monthly descent into the realm of dark and shadow, curated by Habitat. A sanctuary for those who live and breathe goth, metal, and the deeper shades of alternative music.");
                        ImGui.NewLine();
                        ImGui.Separator();
                        ImGui.NewLine();
                        ImGui.Text("Location: (Light) Shiva, Mist W29 P45");
                        ImGui.NewLine();
                        ImGui.Text("Opening Time: Monthly Saturdays");
                        ImGui.Text("- From 17:00 to 00:30 ST (Summer Time)");
                        ImGui.Text("- From 18:00 to 01:30 ST (Winter Time)");
                        ImGui.NewLine();
                        ImGui.Separator();
                        ImGui.NewLine();
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
                        Dropdown("", dropdownItems, ref dropboxSelected);
                        ImGui.NewLine();
                        ListStaff(dropdownItems[dropboxSelected], true);
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
                ImGui.TextColoredWrapped(new Vector4(1, 0, 0, 1), "By using modded content, you acknowledge that you are doing so at your own risk. Modded environments and third-party tools may be unstable and can lead to crashes, performance issues, or other unintended effects.");
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

        // --- Footer ---
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("v0.4.0.0");
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
