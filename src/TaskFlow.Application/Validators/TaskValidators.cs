using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.AssigneeId).GreaterThan(0).When(x => x.AssigneeId is not null);
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.AssigneeId).GreaterThan(0).When(x => x.AssigneeId is not null);
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => TaskStatusMapper.TryParse(s, out _))
            .WithMessage("Status must be one of: TODO, IN_PROGRESS, DONE.");
    }
}
