using Concertable.B2B.Deal.Contracts.Errors;
using Concertable.Kernel.Errors;

namespace Concertable.B2B.Deal.UnitTests.Errors;

public sealed class DealErrorTests
{
    [Fact]
    public void Definition_CreateValidation_ReturnsStableValidationDefinition()
    {
        var errors = new ValidationErrors([new("Fee", "Fee must be greater than zero.")]);

        var definition = Assert.IsType<ValidationErrorDefinition>(
            CreateDealError.Validation(errors).Definition);

        Assert.Equal("deal.create.invalid", definition.Code);
        Assert.Equal("The deal is invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Equal(["Fee must be greater than zero."], definition.Errors["Fee"]);
    }

    [Fact]
    public void Definition_UpdateNotFound_ReturnsStableNotFoundDefinition()
    {
        var definition = UpdateDealError.NotFound(42).Definition;

        Assert.Equal("deal.update.not_found", definition.Code);
        Assert.Equal("Deal 42 was not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void Definition_UpdateValidation_ReturnsStableValidationDefinition()
    {
        var errors = new ValidationErrors([new("HireFee", "Hire fee must be greater than zero.")]);

        var definition = Assert.IsType<ValidationErrorDefinition>(
            UpdateDealError.Validation(errors).Definition);

        Assert.Equal("deal.update.invalid", definition.Code);
        Assert.Equal("The deal is invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Equal(["Hire fee must be greater than zero."], definition.Errors["HireFee"]);
    }
}
