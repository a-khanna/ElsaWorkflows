using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace ElsaLTS.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Get Services Level 1",
        Description = "Gets services stored in database at level 1",
        RuntimeDescription = "x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetServiceTypes : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;

        public GetServiceTypes(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpContextAccessor httpContextAccessor)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault("Response has already started");

            response.StatusCode = 200;
            response.ContentType = "text/html";

            var content = "Choose service type:<br>";
            foreach (var service in Constants.Level1Services)
            {
                content += $"<a href=\"{{{{ '{service}' | signal_url }}}}\">{service}</a><br>";
            }

            var contentWorkflowExp = new LiquidExpression<string>(content);

            var bodyText = await expressionEvaluator.EvaluateAsync(contentWorkflowExp, workflowContext, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Done();
        }
    }
}
