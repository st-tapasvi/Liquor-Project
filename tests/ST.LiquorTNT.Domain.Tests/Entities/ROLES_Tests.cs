using System.Reflection;
using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class ROLES_Tests
{
    private static readonly DateTime Now = new(2026, 10, 7, 10, 0, 0);

    [Fact]
    public void Create_ForCompany_IsNotATemplate()
    {
        var role = ROLES.Create(companyId: 5, " Operator ", " works on the line ", isAdminRole: false, Now, 1);

        role.CompanyId.Should().Be(5);
        role.RoleName.Should().Be("Operator");
        role.Description.Should().Be("works on the line");
        role.IsTemplate.Should().BeFalse();
        role.IsSystem.Should().BeFalse();
        role.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutCompany_IsATemplate()
    {
        ROLES.Create(companyId: null, "Viewer", null, false, Now, 1).IsTemplate.Should().BeTrue();
    }

    [Fact]
    public void CopyOf_Template_BelongsToCompany_KeepsNameAndAdminFlag()
    {
        var template = ROLES.Create(null, "Plant Admin", "head", isAdminRole: true, Now, 1);

        var copy = ROLES.CopyOf(template, companyId: 9, Now, 2);

        copy.CompanyId.Should().Be(9);
        copy.RoleName.Should().Be("Plant Admin");
        copy.IsAdminRole.Should().BeTrue();
        copy.IsTemplate.Should().BeFalse();
        copy.CreatedBy.Should().Be(2);
    }

    [Fact]
    public void Create_ForASupplierCode_KeepsIt()
    {
        var role = ROLES.Create(companyId: 5, "Operator", null, isAdminRole: false, Now, 1, supplierCodeId: 30);

        role.SupplierCodeId.Should().Be(30);
        role.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public void Create_AdminRoleForASupplierCode_Throws()
    {
        var act = () => ROLES.Create(5, "Plant Admin", null, isAdminRole: true, Now, 1, supplierCodeId: 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TemplateWithASupplierCode_Throws()
    {
        var act = () => ROLES.Create(null, "Operator", null, false, Now, 1, supplierCodeId: 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PerSupplierCode_OnlyMeansSomethingForTemplates()
    {
        ROLES.Create(null, "Operator", null, false, Now, 1, perSupplierCode: true).PerSupplierCode.Should().BeTrue();
        ROLES.Create(5, "Operator", null, false, Now, 1, perSupplierCode: true).PerSupplierCode.Should().BeFalse();
    }

    [Fact]
    public void CopyOf_PerSupplierCodeTemplate_NeedsTheSupplierCode_CompanyLevelMustNotGetOne()
    {
        var perCode = ROLES.Create(null, "Operator", null, false, Now, 1, perSupplierCode: true);
        var companyLevel = ROLES.Create(null, "Agent Manager", null, false, Now, 1);

        ROLES.CopyOf(perCode, 9, Now, 2, supplierCodeId: 30).SupplierCodeId.Should().Be(30);
        ROLES.CopyOf(companyLevel, 9, Now, 2).SupplierCodeId.Should().BeNull();

        ((Action)(() => ROLES.CopyOf(perCode, 9, Now, 2))).Should().Throw<ArgumentException>();
        ((Action)(() => ROLES.CopyOf(companyLevel, 9, Now, 2, supplierCodeId: 30))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CopyOf_CompanyRole_Throws()
    {
        var companyRole = ROLES.Create(5, "Operator", null, false, Now, 1);

        var act = () => ROLES.CopyOf(companyRole, 9, Now, 2);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_SuperAdmin_Throws()
    {
        var superAdmin = ROLES.Create(null, "Super Admin", null, false, Now, 1);
        typeof(ROLES).GetProperty(nameof(ROLES.IsSystem), BindingFlags.Public | BindingFlags.Instance)!.SetValue(superAdmin, true);

        var act = () => superAdmin.Update("Renamed", null, false, Now, 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_BlankName_Throws()
    {
        var role = ROLES.Create(5, "Operator", null, false, Now, 1);

        var act = () => role.Update("  ", null, false, Now, 1);
        act.Should().Throw<ArgumentException>();
    }
}
