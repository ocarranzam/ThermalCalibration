using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Thermal.Api.EquipmentTypes;

namespace Thermal.UnitTests.Api;

/// <summary>Autorización por rol declarada en <see cref="EquipmentTypesController"/> (HU-02, RA-02).</summary>
[Trait("Story", "HU-02")]
public sealed class EquipmentTypesControllerAuthorizationTests
{
    private static string[] RolesOf(string actionName) =>
        [.. typeof(EquipmentTypesController).GetMethod(actionName)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Roles)
            .OfType<string>()];

    [Theory] // HU-02 · Scenario: Un técnico no puede editar límites (solo Admin crea o edita)
    [InlineData(nameof(EquipmentTypesController.Create))]
    [InlineData(nameof(EquipmentTypesController.Update))]
    public void WriteActions_RequireAdminRole(string actionName) =>
        RolesOf(actionName).Should().Equal("Admin");

    [Fact] // HU-02 · Regla: todos los roles autenticados pueden consultar un tipo de equipo
    public void GetById_RequiresOnlyAuthentication()
    {
        RolesOf(nameof(EquipmentTypesController.GetById)).Should().BeEmpty();
        typeof(EquipmentTypesController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }
}
