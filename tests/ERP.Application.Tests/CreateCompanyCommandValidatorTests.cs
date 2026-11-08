using FluentAssertions;
using ERP.Application.Features.Master.Companies;
using Xunit;

namespace ERP.Application.Tests;

public class CreateCompanyCommandValidatorTests
{
    private readonly CreateCompanyCommandValidator _validator = new();

    [Fact]
    public void Validator_WithValidData_ShouldNotHaveValidationErrors()
    {
        var command = new CreateCompanyCommand(
            Code: "CMP-001",
            Name: "PT Enterprise Sinergi Nusantara",
            TaxIdentificationNumber: "01.234.567.8-901.000",
            Address: "Jl. Sudirman No. 1",
            Phone: "+62215551234",
            Email: "contact@enterprise.erp",
            CurrencyCode: "IDR");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithInvalidCodeFormat_ShouldFailValidation()
    {
        var command = new CreateCompanyCommand(
            Code: "CMP 001 @#$",
            Name: "PT Enterprise",
            TaxIdentificationNumber: "123",
            Address: "Alamat",
            Phone: null,
            Email: null,
            CurrencyCode: "IDR");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.Code));
    }

    [Fact]
    public void Validator_WithInvalidEmail_ShouldFailValidation()
    {
        var command = new CreateCompanyCommand(
            Code: "CMP-002",
            Name: "PT Enterprise",
            TaxIdentificationNumber: "123",
            Address: "Alamat",
            Phone: null,
            Email: "invalid-email-format",
            CurrencyCode: "IDR");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.Email));
    }

    [Fact]
    public void Validator_WithInvalidCurrencyCodeLength_ShouldFailValidation()
    {
        var command = new CreateCompanyCommand(
            Code: "CMP-003",
            Name: "PT Enterprise",
            TaxIdentificationNumber: "123",
            Address: "Alamat",
            Phone: null,
            Email: null,
            CurrencyCode: "RUPIAH");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.CurrencyCode));
    }
}

