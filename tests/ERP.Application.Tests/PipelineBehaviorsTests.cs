using ERP.Application.Common.Behaviors;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Exceptions;
using ERP.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ERP.Application.Tests;

public class PipelineBehaviorsTests
{
    [RequirePermission("sales.order.create")]
    public record SampleProtectedCommand(string Name) : IRequest<Result<Guid>>;

    [Fact]
    public async Task AuthorizationPipelineBehavior_ShouldReject_UnauthenticatedUser()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);

        var behavior = new AuthorizationPipelineBehavior<SampleProtectedCommand, Result<Guid>>(currentUser);
        var command = new SampleProtectedCommand("Test SO");

        // Act
        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Unauthorized");
    }

    [Fact]
    public async Task AuthorizationPipelineBehavior_ShouldReject_AuthenticatedUser_WithoutPermission()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.HasPermission("sales.order.create").Returns(false);

        var behavior = new AuthorizationPipelineBehavior<SampleProtectedCommand, Result<Guid>>(currentUser);
        var command = new SampleProtectedCommand("Test SO");

        // Act
        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task AuthorizationPipelineBehavior_ShouldAllow_AuthenticatedUser_WithRequiredPermission()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.HasPermission("sales.order.create").Returns(true);

        var expectedId = Guid.NewGuid();
        var behavior = new AuthorizationPipelineBehavior<SampleProtectedCommand, Result<Guid>>(currentUser);
        var command = new SampleProtectedCommand("Test SO");

        // Act
        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(Result<Guid>.Success(expectedId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task ExceptionHandlingPipelineBehavior_ShouldCatch_DomainException_AndReturnFailureResult()
    {
        // Arrange
        var logger = NullLogger<ExceptionHandlingPipelineBehavior<SampleProtectedCommand, Result<Guid>>>.Instance;
        var behavior = new ExceptionHandlingPipelineBehavior<SampleProtectedCommand, Result<Guid>>(logger);

        // Act
        var result = await behavior.Handle(
            new SampleProtectedCommand("Test"),
            _ => throw new SampleDomainException("BR-INV-001: Stok tidak mencukupi."),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Domain.RuleViolation");
        result.Error.Description.Should().Contain("BR-INV-001");
    }

    private sealed class SampleDomainException(string message) : DomainException(message);

    [Fact]
    public async Task ExceptionHandlingPipelineBehavior_ShouldCatch_DbUpdateConcurrencyException_AndReturnConflictResult()
    {
        // Arrange
        var logger = NullLogger<ExceptionHandlingPipelineBehavior<SampleProtectedCommand, Result<Guid>>>.Instance;
        var behavior = new ExceptionHandlingPipelineBehavior<SampleProtectedCommand, Result<Guid>>(logger);

        // Act
        var result = await behavior.Handle(
            new SampleProtectedCommand("Test"),
            _ => throw new DbUpdateConcurrencyException("Concurrent update conflict on xmin."),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.ConcurrencyConflict");
    }
}

