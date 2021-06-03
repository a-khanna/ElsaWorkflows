using System.Linq;
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
        DisplayName = "Get Services Level 2",
        Description = "Gets services stored in database at level 2",
        RuntimeDescription = "x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetLevel2Services : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;

        public GetLevel2Services(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpContextAccessor httpContextAccessor)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            var signal = context.Workflow.Input.GetVariable<string>("Signal");
            context.SetVariable("Level1", signal);
            return Constants.Level1Services.Any(s => s == signal);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault("Response has already started");

            response.StatusCode = 200;
            response.ContentType = "text/html";

            string content;
            var signal = workflowContext.GetVariable("Level1").ToString();
            if (signal.Equals("Admin"))
                content = GenerateContent(signal, Constants.Level2Services_Admin);
            else if (signal.Equals("IT"))
                content = GenerateContent(signal, Constants.Level2Services_IT);
            else
                content = GenerateContent(signal, Constants.Level2Services_HR);

            var contentWorkflowExp = new LiquidExpression<string>(content);

            var bodyText = await expressionEvaluator.EvaluateAsync(contentWorkflowExp, workflowContext, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
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
