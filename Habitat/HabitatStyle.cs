using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Habitat.Windows;

public static class HabitatStyle
{
    public static readonly Vector4 Accent = new(0.06f, 0.49f, 0.79f, 1.00f);
    public static readonly Vector4 AccentHover = new(0.14f, 0.57f, 0.88f, 1.00f);
    public static readonly Vector4 AccentActive = new(0.04f, 0.38f, 0.64f, 1.00f);
    public static readonly Vector4 AccentSoft = new(0.06f, 0.49f, 0.79f, 0.18f);
    public static readonly Vector4 AccentGlow = new(0.18f, 0.63f, 0.96f, 0.32f);

    public static readonly Vector4 WindowBg = new(0.05f, 0.06f, 0.08f, 0.98f);
    public static readonly Vector4 Panel = new(0.08f, 0.10f, 0.13f, 0.96f);
    public static readonly Vector4 PanelRaised = new(0.10f, 0.12f, 0.16f, 0.98f);
    public static readonly Vector4 PanelInset = new(0.06f, 0.08f, 0.11f, 0.94f);
    public static readonly Vector4 Border = new(0.15f, 0.20f, 0.27f, 0.95f);
    public static readonly Vector4 BorderStrong = new(0.23f, 0.34f, 0.47f, 0.95f);

    public static readonly Vector4 Text = new(0.92f, 0.95f, 0.99f, 1.00f);
    public static readonly Vector4 TextMuted = new(0.64f, 0.71f, 0.80f, 1.00f);
    public static readonly Vector4 TextDim = new(0.46f, 0.53f, 0.62f, 1.00f);

    public static readonly Vector4 Success = new(0.29f, 0.84f, 0.57f, 1.00f);
    public static readonly Vector4 Danger = new(0.93f, 0.36f, 0.41f, 1.00f);
    public static readonly Vector4 Warning = new(0.94f, 0.74f, 0.36f, 1.00f);

    public const float WindowRounding = 14f;
    public const float PanelRounding = 12f;
    public const float ControlRounding = 8f;
    public const float PillRounding = 999f;
    public const float WindowPaddingX = 16f;
    public const float WindowPaddingY = 16f;
    public const float PanelPaddingX = 16f;
    public const float PanelPaddingY = 14f;
    public const float CompactPanelPaddingX = 14f;
    public const float CompactPanelPaddingY = 12f;
    public const float ItemSpacingX = 10f;
    public const float ItemSpacingY = 10f;
    public const float CellPaddingX = 10f;
    public const float CellPaddingY = 8f;

    public static ThemeScope PushTheme(float scale)
    {
        var colorCount = 0;
        var varCount = 0;

        ImGui.PushStyleColor(ImGuiCol.Text, Text); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, TextDim); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, WindowBg); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Panel); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.PopupBg, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.FrameBg, PanelInset); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, AccentSoft); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TitleBg, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, Panel); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.MenuBarBg, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Panel); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.18f, 0.24f, 0.31f, 1.00f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.24f, 0.32f, 0.42f, 1.00f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, AccentHover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.CheckMark, AccentHover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, Accent); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, AccentHover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.10f, 0.13f, 0.18f, 0.96f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.14f, 0.18f, 0.24f, 1.00f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.18f, 0.24f, 0.31f, 1.00f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Header, AccentSoft); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, WithAlpha(AccentHover, 0.22f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, WithAlpha(Accent, 0.28f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Separator, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, BorderStrong); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.SeparatorActive, Accent); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.09f, 0.11f, 0.15f, 0.98f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TabHovered, WithAlpha(Accent, 0.35f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TabActive, WithAlpha(Accent, 0.24f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, BorderStrong); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableBorderLight, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(1f, 1f, 1f, 0.03f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, AccentSoft); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new Vector4(0f, 0f, 0f, 0.28f)); colorCount++;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(WindowPaddingX * scale, WindowPaddingY * scale)); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, WindowRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, PanelRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ItemSpacingX * scale, ItemSpacingY * scale)); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(8f * scale, 6f * scale)); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(CellPaddingX * scale, CellPaddingY * scale)); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12f * scale, 8f * scale)); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 12f * scale); varCount++;

        return new ThemeScope(colorCount, varCount);
    }

    public static PanelScope BeginPanel(
        string id,
        Vector2 size,
        float scale,
        Vector4 background,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None,
        float paddingX = PanelPaddingX,
        float paddingY = PanelPaddingY,
        bool accentLine = true,
        bool border = true)
    {
        var colorCount = 0;
        var varCount = 0;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, border ? Border : new Vector4(0f, 0f, 0f, 0f)); colorCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, PanelRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, border ? 1f : 0f); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(paddingX * scale, paddingY * scale)); varCount++;

        ImGui.BeginChild(id, size, border, flags);

        if (accentLine)
        {
            DrawPanelAccent(scale);
        }

        return new PanelScope(colorCount, varCount);
    }

    public static StyleScope PushPrimaryButtonStyle(float scale)
    {
        var colorCount = 0;
        var varCount = 0;

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.08f, 0.31f, 0.51f, 0.95f)); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Accent); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, AccentActive); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, WithAlpha(AccentHover, 0.75f)); colorCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f); varCount++;

        return new StyleScope(colorCount, varCount);
    }

    public static StyleScope PushSecondaryButtonStyle(float scale, bool enabled = true)
    {
        var colorCount = 0;
        var varCount = 0;
        var button = enabled ? PanelRaised : WithAlpha(PanelInset, 0.70f);
        var hovered = enabled ? new Vector4(0.14f, 0.18f, 0.24f, 1.00f) : WithAlpha(PanelInset, 0.70f);
        var active = enabled ? new Vector4(0.18f, 0.24f, 0.31f, 1.00f) : WithAlpha(PanelInset, 0.70f);
        var text = enabled ? Text : TextDim;

        ImGui.PushStyleColor(ImGuiCol.Button, button); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Text, text); colorCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f); varCount++;

        return new StyleScope(colorCount, varCount);
    }

    public static StyleScope PushSidebarButtonStyle(bool selected, float scale)
    {
        var colorCount = 0;
        var varCount = 0;
        var fill = selected ? WithAlpha(Accent, 0.20f) : new Vector4(0.10f, 0.12f, 0.16f, 0.55f);
        var hover = selected ? WithAlpha(AccentHover, 0.28f) : new Vector4(0.13f, 0.16f, 0.21f, 0.90f);
        var active = selected ? WithAlpha(Accent, 0.32f) : new Vector4(0.16f, 0.20f, 0.26f, 0.95f);
        var text = selected ? Text : TextMuted;

        ImGui.PushStyleColor(ImGuiCol.Button, fill); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, selected ? BorderStrong : Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Text, text); colorCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f); varCount++;

        return new StyleScope(colorCount, varCount);
    }

    public static StyleScope PushFieldButtonStyle(float scale)
    {
        var colorCount = 0;
        var varCount = 0;

        ImGui.PushStyleColor(ImGuiCol.Button, PanelInset); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, PanelRaised); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Border, BorderStrong); colorCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ControlRounding * scale); varCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f); varCount++;

        return new StyleScope(colorCount, varCount);
    }

    public static void DrawPanelAccent(float scale)
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var y = pos.Y + 1f;
        var inset = 14f * scale;

        drawList.AddLine(
            new Vector2(pos.X + inset, y),
            new Vector2(pos.X + size.X - inset, y),
            ImGui.GetColorU32(WithAlpha(AccentHover, 0.55f)),
            MathF.Max(1f, 2f * scale));
    }

    public static void DrawDivider(float scale, float alpha = 1f)
    {
        var drawList = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = start.Y + 3f * scale;
        drawList.AddLine(
            new Vector2(start.X, y),
            new Vector2(start.X + width, y),
            ImGui.GetColorU32(WithAlpha(BorderStrong, 0.40f * alpha)),
            1f);
        ImGui.Dummy(new Vector2(width, 6f * scale));
    }

    public static void DrawInlineBadge(string text, Vector4 background, Vector4 outline, Vector4 textColor, float scale)
    {
        var padding = new Vector2(10f * scale, 6f * scale);
        var textSize = ImGui.CalcTextSize(text);
        var size = new Vector2(textSize.X + padding.X * 2f, textSize.Y + padding.Y * 2f);
        var pos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(background), PillRounding);
        drawList.AddRect(pos, pos + size, ImGui.GetColorU32(outline), PillRounding, 0, 1f);
        drawList.AddText(pos + padding, ImGui.GetColorU32(textColor), text);
        ImGui.Dummy(size);
    }

    public static Vector4 WithAlpha(Vector4 color, float alpha)
        => new(color.X, color.Y, color.Z, alpha);

    public readonly struct ThemeScope : IDisposable
    {
        private readonly int colorCount;
        private readonly int varCount;

        public ThemeScope(int colorCount, int varCount)
        {
            this.colorCount = colorCount;
            this.varCount = varCount;
        }

        public void Dispose()
        {
            if (varCount > 0)
            {
                ImGui.PopStyleVar(varCount);
            }

            if (colorCount > 0)
            {
                ImGui.PopStyleColor(colorCount);
            }
        }
    }

    public readonly struct StyleScope : IDisposable
    {
        private readonly int colorCount;
        private readonly int varCount;

        public StyleScope(int colorCount, int varCount)
        {
            this.colorCount = colorCount;
            this.varCount = varCount;
        }

        public void Dispose()
        {
            if (varCount > 0)
            {
                ImGui.PopStyleVar(varCount);
            }

            if (colorCount > 0)
            {
                ImGui.PopStyleColor(colorCount);
            }
        }
    }

    public readonly struct PanelScope : IDisposable
    {
        private readonly int colorCount;
        private readonly int varCount;

        public PanelScope(int colorCount, int varCount)
        {
            this.colorCount = colorCount;
            this.varCount = varCount;
        }

        public void Dispose()
        {
            ImGui.EndChild();

            if (varCount > 0)
            {
                ImGui.PopStyleVar(varCount);
            }

            if (colorCount > 0)
            {
                ImGui.PopStyleColor(colorCount);
            }
        }
    }
}
