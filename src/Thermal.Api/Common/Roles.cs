namespace Thermal.Api.Common;

/// <summary>Roles de <c>dbo.AppUser.Role</c> (<c>CK_AppUser_Role</c>).</summary>
internal static class Roles
{
    public const string Admin = "Admin";
    public const string Technician = "Technician";
    public const string Supervisor = "Supervisor";

    /// <summary>Registran y editan empresas y equipos (HU-01); el supervisor solo consulta.</summary>
    public const string Editors = $"{Admin},{Technician}";
}
