using FluentValidation;

namespace Meeting_Project.Dtos.AccountDtos
{
    public class RegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }   
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string RepeatPass { get; set; }

    }
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Please correct email adress");
            RuleFor(r => r.Password).MinimumLength(8).WithMessage("8 Simvoldan yuxari olmalidir");
            RuleFor(r => r.RepeatPass).MinimumLength(8).WithMessage("8 Simvoldan yuxari olmalidir");
            RuleFor(transaction => transaction.PhoneNumber)
            .Matches(@"^\+994(?:\s*\d){9}$").WithMessage("Nomreni duzgun daxil edin");
            RuleFor(x => x.RepeatPass)
            .Equal(x => x.Password)
            .WithMessage("Parollar eyni deyil");
        }
    }
}
