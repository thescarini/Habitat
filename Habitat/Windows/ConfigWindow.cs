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

        Size = new Vector2(400, 300);
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

    public override void Draw()
    {
        //if (plugin.localPlayer.FullName == "A'scari Diamonds@Shiva" ||  plugin.localPlayer.FullName == "Taniri Danolnith@Raiden" || plugin.localPlayer.FullName == "Destiny Skyforged@Raiden")
        //{
            ImGui.Text("Dev Debug Menu");
            if (ImGui.Button("force reload DataServiceVip"))
            {
                plugin.DataServiceVip.Refresh();
            }
            if (ImGui.Button("force reload DataServiceStaff"))
            {
                plugin.DataServiceStaff.Refresh();
            }
            if (ImGui.Button("force reload DataServiceServices"))
            {
                plugin.DataServiceServices.Refresh();
            }
            if (ImGui.Button("force reload DataServiceVipPerks"))
            {
                plugin.DataServiceVipPerks.Refresh();
            }
            if (ImGui.Button("reset loaded VIP status"))
            {
                plugin.localPlayer.IsVip = false;
                plugin.localPlayer.VipKind = "";
            }
            if (ImGui.Button("reset loaded local player staff status"))
            {
                plugin.localPlayer.IsStaff = false;
                plugin.localPlayer.IsStaffHead = false;
                plugin.localPlayer.StaffRole = string.Empty;
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
