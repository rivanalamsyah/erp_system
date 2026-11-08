using FluentAssertions;
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
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("All MediatR request handlers must follow naming convention '*Handler'.");
    }
}

