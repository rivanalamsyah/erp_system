using System.Reflection;
using ERP.Application.Common.Security;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;
using Xunit;

namespace ERP.Architecture.Tests;

public class CleanArchitectureTests
{
    private const string DomainNamespace = "ERP.Domain";
    private const string ApplicationNamespace = "ERP.Application";
    private const string InfrastructureNamespace = "ERP.Infrastructure";
    private const string WebNamespace = "ERP.Web";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOn_Application_Or_Infrastructure_Or_Web()
    {
        // Arrange
        var domainAssembly = typeof(ERP.Domain.Common.BaseEntity<>).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain layer must be pure and independent of outer layers.");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOn_Infrastructure_Or_Web()
    {
        // Arrange
        var applicationAssembly = typeof(ERP.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Application layer must not depend on Infrastructure or Web UI.");
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependencyOn_Web()
    {
        // Arrange
        var infrastructureAssembly = typeof(ERP.Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Infrastructure layer must not depend on the Web layer.");
    }

    [Fact]
    public void MediatR_RequestHandlers_Should_ResideIn_Application_And_EndWith_Handler()
    {
        // Arrange
        var applicationAssembly = typeof(ERP.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("All MediatR request handlers must follow naming convention '*Handler'.");
    }

    [Fact]
    public void All_MediatR_Requests_In_Application_Should_Have_RequirePermissionAttribute()
    {
        // Arrange
        var applicationAssembly = typeof(ERP.Application.DependencyInjection).Assembly;

        var requestTypes = applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToList();

        // Act
        var unprotectedRequests = requestTypes
            .Where(t => !t.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).Any())
            .Select(t => t.FullName)
            .ToList();

        // Assert
        requestTypes.Should().NotBeEmpty();
        unprotectedRequests.Should().BeEmpty(
            "Every MediatR Command and Query in ERP.Application must be protected by at least one [RequirePermission] attribute.");
    }
}
