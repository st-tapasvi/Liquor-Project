using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Api.Logging;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

/// <summary>Only Business's own services are wrapped; lifetimes stay as registered.</summary>
public sealed class MethodLoggingRegistrationTests
{
    private sealed class OutsideTokenService : IAccessTokenService
    {
        public string Create(USERS user, DateTime expiresAtUtc) => "eyJ.raw.jwt";
    }

    [Fact]
    public void BusinessService_IsReplacedByProxyFactory_SameLifetimeAndPosition()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUserService, UserService>();

        services.AddMethodLogging(typeof(UserService).Assembly);

        var descriptor = services.Single();
        descriptor.ServiceType.Should().Be(typeof(IUserService));
        descriptor.ImplementationFactory.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void InterfaceImplementedOutsideBusiness_IsNeverWrapped_WhateverTheOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAccessTokenService, OutsideTokenService>();   // registered before AddMethodLogging

        services.AddMethodLogging(typeof(UserService).Assembly);

        services.Single().ImplementationType.Should().Be(typeof(OutsideTokenService));
    }
}
