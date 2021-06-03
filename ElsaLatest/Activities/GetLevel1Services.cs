using System.Threading.Tasks;
using Elsa;
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
        DisplayName = "Get level 1 services",
        Description = "Get level 1 services",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetLevel1Services : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExpressionEvaluator _expressionEvaluator;

        private IStringLocalizer T { get; }

        public GetLevel1Services(IHttpContextAccessor httpContextAccessor, IExpressionEvaluator expressionEvaluator, IStringLocalizer<GetLevel1Services> localizer)
        {
            T = localizer;
            _expressionEvaluator = expressionEvaluator;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = 200;
            response.ContentType = "text/html";

            var content = "Choose service type:<br>";
            foreach (var service in Constants.Level1Services)
            {
                content += $"<a href=\"{{{{ '{service}' | signal_url }}}}\">{service}</a><br>";
            }

            var bodyText = await _expressionEvaluator.EvaluateAsync<string>(content, SyntaxNames.Liquid, context);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, context.CancellationToken);
            }

            return Done(bodyText);
        }
    }
}
