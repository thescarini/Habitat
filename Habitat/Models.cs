using System;

namespace Habitat.Models;

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
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Price { get; set; } = "";
    public string Description { get; set; } = "";
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
}


