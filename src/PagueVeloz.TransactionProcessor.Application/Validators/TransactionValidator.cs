using FluentValidation;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Validators
{
    public class TransactionValidator : AbstractValidator<TransactionRequest>
    {
        private readonly string[] _validOperations = { "credit", "debit", "reserve", "capture", "reversal", "transfer" };
        private readonly string[] _validCurrencies = { "BRL", "USD", "EUR" };

        public TransactionValidator()
        {
            RuleFor(x => x.Operation)
                .NotEmpty().WithMessage("Operação é obrigatória")
                .Must(BeValidOperation).WithMessage("Tipo de operação inválido");

            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("ID da conta é obrigatório")
                .Must(BeValidGuid).WithMessage("Formato do ID da conta inválido");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Valor deve ser maior que zero");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Moeda é obrigatória")
                .Must(BeValidCurrency).WithMessage("Moeda inválida");

            RuleFor(x => x.ReferenceId)
                .NotEmpty().WithMessage("ID de referência é obrigatório")
                .MaximumLength(100).WithMessage("ID de referência muito longo");

            RuleFor(x => x.TargetAccountId)
                .Must(BeValidGuid)
                .When(x => !string.IsNullOrEmpty(x.TargetAccountId))
                .WithMessage("Formato do ID da conta destino inválido");

            RuleFor(x => x.TargetAccountId)
                .NotEmpty()
                .When(x => x.Operation?.ToLower() == "transfer")
                .WithMessage("Conta destino é obrigatória para transferências");
        }

        private bool BeValidOperation(string operation)
        {
            return !string.IsNullOrEmpty(operation) &&
                _validOperations.Contains(operation.ToLower());
        }

        private bool BeValidCurrency(string currency)
        {
            return !string.IsNullOrEmpty(currency) &&
                _validCurrencies.Contains(currency.ToUpper());
        }

        private bool BeValidGuid(string? guid)
        {
            return !string.IsNullOrEmpty(guid) && Guid.TryParse(guid, out _);
        }
    }
}
