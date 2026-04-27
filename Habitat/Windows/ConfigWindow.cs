using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Habitat.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Habitat Nightclub Plugin Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(420, 624);
        SizeCondition = ImGuiCond.Always;
        configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        /*if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }*/
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

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var theme = HabitatStyle.PushTheme(scale);

        _ = configuration;
        
        using (HabitatStyle.BeginPanel("ConfigBody", Vector2.Zero, scale, HabitatStyle.PanelRaised))
        {
            ImGui.TextColoredWrapped(HabitatStyle.Text, "Maintenance & Refresh");
            ImGui.TextColoredWrapped(HabitatStyle.TextMuted, "Lightweight venue tools for refreshing remote data and resetting cached local state.");
            ImGui.Spacing();
            HabitatStyle.DrawDivider(scale);

            using (HabitatStyle.BeginPanel("ConfigDataPanel", new Vector2(0, 264f * scale), scale, HabitatStyle.PanelInset, ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.TextColored(HabitatStyle.AccentHover, "Data Sources");
                ImGui.TextColoredWrapped(HabitatStyle.TextMuted, "Refresh the current Supabase-backed lists without changing any plugin behavior.");
                ImGui.Spacing();

                if (DrawPrimaryButton("Reload VIP Data", new Vector2(-1, 0)))
                {
                    plugin.DataServiceVip.Refresh();
                }

                if (DrawSecondaryButton("Reload Staff Data", new Vector2(-1, 0)))
                {
                    plugin.DataServiceStaff.Refresh();
                }

                if (DrawSecondaryButton("Reload Services Data", new Vector2(-1, 0)))
                {
                    plugin.DataServiceServices.Refresh();
                }

                if (DrawSecondaryButton("Reload VIP Perks", new Vector2(-1, 0)))
                {
                    plugin.DataServiceVipPerks.Refresh();
                }
            }

            ImGui.Spacing();

            using (HabitatStyle.BeginPanel("ConfigStatePanel", Vector2.Zero, scale, HabitatStyle.PanelInset))
            {
                ImGui.TextColored(HabitatStyle.AccentHover, "Local State");
                ImGui.TextColoredWrapped(HabitatStyle.TextMuted, "Reset cached player-side status values for quick debugging.");
                ImGui.Spacing();

                if (DrawSecondaryButton("Reset Loaded VIP Status", new Vector2(-1, 0)))
                {
                    plugin.localPlayer.IsVip = false;
                    plugin.localPlayer.VipKind = "";
                }

                if (DrawSecondaryButton("Reset Local Staff Status", new Vector2(-1, 0)))
                {
                    plugin.localPlayer.IsStaff = false;
                    plugin.localPlayer.IsStaffHead = false;
                    plugin.localPlayer.StaffRole = string.Empty;
                }
                
            }
        }
        

        //}


        // Can't ref a property, so use a local copy
        /*var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            configuration.SomePropertyToBeSavedAndWithADefault = configValue;
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }*/

        /*var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }*/

    }
}
