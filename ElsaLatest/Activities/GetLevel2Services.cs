using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Signaling.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace ElsaLatest.Activities
{
    [Action(
        Category = "Ginger",
        DisplayName = "Get level 2 services",
        Description = "Get level 2 services",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetLevel2Services : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExpressionEvaluator _expressionEvaluator;

        private IStringLocalizer T { get; }

        public GetLevel2Services(IHttpContextAccessor httpContextAccessor, IExpressionEvaluator expressionEvaluator, IStringLocalizer<GetLevel1Services> localizer)
        {
            T = localizer;
            _expressionEvaluator = expressionEvaluator;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override ValueTask<bool> OnCanExecuteAsync(ActivityExecutionContext context)
        {
            var result = false;
            if (context.Input is Signal triggeredSignal)
            {
                context.SetVariable("Level1", triggeredSignal.SignalName);
                result = Constants.Level1Services.Any(s => string.Equals(triggeredSignal.SignalName, s, StringComparison.OrdinalIgnoreCase));
            }

            return ValueTask.FromResult(result);
        }

        protected override ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => 
            context.WorkflowExecutionContext.IsFirstPass ? ValueTask.FromResult(OnResume(context)) : ValueTask.FromResult(Suspend() as IActivityExecutionResult);

        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = 200;
            response.ContentType = "text/html";

            string content;
            var signal = context.GetVariable("Level1").ToString();
            if (signal.Equals("Admin"))
                content = GenerateContent(signal, Constants.Level2Services_Admin);
            else if (signal.Equals("IT"))
                content = GenerateContent(signal, Constants.Level2Services_IT);
            else
                content = GenerateContent(signal, Constants.Level2Services_HR);

            var bodyText = await _expressionEvaluator.EvaluateAsync<string>(content, SyntaxNames.Liquid, context);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, context.CancellationToken);
            }

            return Done();
        }

        private string GenerateContent(string level1Service, string[] services)
        {
            var content = $"Choose an {level1Service} service:<br>";
            foreach (var service in services)
            {
                content += $"<a href=\"{{{{ '{service}' | signal_url }}}}\">{service}</a><br>";
            }

            return content;
        }
    }
}
