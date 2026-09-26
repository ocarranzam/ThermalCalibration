using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Thermal.Api.Companies;
using Thermal.Api.Equipments;
using Thermal.Api.Settings;

namespace Thermal.UnitTests.Api;

/// <summary>Autorización por rol de empresas, equipos (HU-01) y parámetros (HU-02).</summary>
public sealed class CompanyAndSettingsAuthorizationTests
{
    private static string[] RolesOf<TController>(string actionName) =>
        [.. typeof(TController).GetMethod(actionName)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Roles)
            .OfType<string>()];

    [Theory] // HU-01 · Regla: el técnico y el administrador registran y editan empresas y equipos
    [Trait("Story", "HU-01")]
    [InlineData(nameof(CompaniesController.Create))]
    [InlineData(nameof(CompaniesController.Update))]
    [InlineData(nameof(CompaniesController.CreateEquipment))]
    public void CompanyWriteActions_AllowAdminAndTechnician(string actionName) =>
        RolesOf<CompaniesController>(actionName).Should().Equal("Admin,Technician");

    [Fact] // HU-01 · Regla: el técnico y el administrador editan equipos; el supervisor solo consulta
    [Trait("Story", "HU-01")]
    public void EquipmentUpdate_AllowsAdminAndTechnician()
    {
        RolesOf<EquipmentController>(nameof(EquipmentController.Update)).Should().Equal("Admin,Technician");
        RolesOf<EquipmentController>(nameof(EquipmentController.GetById)).Should().BeEmpty();
    }

    [Fact] // HU-02 · Regla: solo el administrador edita los parámetros; todos los roles los consultan
    [Trait("Story", "HU-02")]
    public void Settings_UpdateRequiresAdmin()
    {
        RolesOf<SettingsController>(nameof(SettingsController.Update)).Should().Equal("Admin");
        RolesOf<SettingsController>(nameof(SettingsController.Get)).Should().BeEmpty();
        typeof(SettingsController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }
}
