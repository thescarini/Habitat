using System;

namespace Habitat.Models;

public class LocalPlayer
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsVip { get; set; } = false;
    public bool IsStaff { get; set; } = false;
    public bool IsStaffHead { get; set; } = false;
    public string StaffRole { get; set; } = "";
    public string VipKind { get; set; } = "";
}

public class StaffMember
{
    public string Character_name { get; set; } = "";
    public string World { get; set; } = "";
    public bool Is_Habitat { get; set; } = true;
    public string Role { get; set; } = "";
    public bool Is_Gothika { get; set; } = true;
    public string Gothika_Role { get; set; } = "";
    public string Link { get; set; } = "";
    public bool Hiatus { get; set; } = false;
    public bool Head_staff { get; set; } = false;
    public bool Status { get; set; } = false;
    public string Habitat_dropdown { get; set; } = "";
    public string Gothika_dropdown { get; set; } = "";
}

public class Service
{
    public string Service_name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Price { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Is_habitat { get; set; } = false;
    public bool Is_gothika { get; set; } = false;
}

public class VisiblePlayer
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
}

public class VipList
{
    public string Character_name { get; set; } = "";
    public string World { get; set; } = "";
    public string Vip_kind { get; set; } = "";
    public DateOnly Vip_since { get; set; } = DateOnly.Parse("1900-01-01");
    public string Discord_handle { get; set; } = "";
}

public class VipPerks
{
    public string Perk_name { get; set; } = "";
    public bool Is_vip { get; set; } = false;
    public bool Is_booster { get; set; } = false;
    public bool Is_lifetime { get; set; } = false;
    public bool Is_monthly { get; set; } = false;
}
