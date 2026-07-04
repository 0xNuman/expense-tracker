using ExpenseTracker.Domain;

namespace ExpenseTracker.Domain.Tests;

public class TenantTests
{
    [Fact]
    public void Create_AddsOwnerMembershipAndRaisesEvent()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);

        tenant.Id.Value.Should().NotBeEmpty();
        tenant.Name.Should().Be("Personal");
        tenant.CreatedByUserId.Should().Be(owner.Id);

        tenant.Memberships.Should().ContainSingle()
            .Which.Role.Should().Be(TenantRole.Owner);
        tenant.HasEvents.Should().BeTrue();
        var events = tenant.Events.OfType<TenantCreated>().ToList();
        events.Should().ContainSingle();
    }

    [Fact]
    public void Invite_ExistingMember_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);
        var other = User.Register("other@example.com", "Other");

        tenant.Invite(other.Id, TenantRole.Admin, owner.Id);

        var act = () => tenant.Invite(other.Id, TenantRole.Member, owner.Id);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Invite_SecondOwner_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);
        var other = User.Register("other@example.com", "Other");

        var act = () => tenant.Invite(other.Id, TenantRole.Owner, owner.Id);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already has an Owner*");
    }

    [Fact]
    public void ChangeMemberRole_DemotingOwnerWithoutSuccessor_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);
        var other = User.Register("other@example.com", "Other");
        tenant.Invite(other.Id, TenantRole.Admin, owner.Id);

        var ownerMembership = tenant.Memberships.FirstOrDefault(m => m.Role == TenantRole.Owner)!;
        var act = () => tenant.ChangeMemberRole(ownerMembership, TenantRole.Admin);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*successor*");
    }

    [Fact]
    public void ChangeMemberRole_PromotesSuccessorWhenDemotingOwner()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);
        var other = User.Register("other@example.com", "Other");
        tenant.Invite(other.Id, TenantRole.Admin, owner.Id);

        var ownerMembership = tenant.Memberships.FirstOrDefault(m => m.UserId == owner.Id)!;
        tenant.ChangeMemberRole(ownerMembership, TenantRole.Admin, newOwnerUserId: other.Id);

        tenant.Memberships.Single(m => m.Role == TenantRole.Owner).UserId.Should().Be(other.Id);
    }

    [Fact]
    public void RemoveMember_OnTheLastOwner_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);

        var ownerMembership = tenant.Memberships.Single();
        var act = () => tenant.RemoveMember(ownerMembership);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rename_Blank_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);

        var act = () => tenant.Rename("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_LongerThan100_Rejects()
    {
        var owner = User.Register("owner@example.com", "Owner");
        var tenant = Tenant.Create("Personal", owner.Id);

        var act = () => tenant.Rename(new string('a', 101));
        act.Should().Throw<ArgumentException>();
    }
}