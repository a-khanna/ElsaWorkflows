using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace ElsaLTS.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Get ticket details",
        Description = "Gets title and description of the ticket",
        RuntimeDescription = "x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetTicketDetails : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;

        public GetTicketDetails(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpContextAccessor httpContextAccessor)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            var signal = context.Workflow.Input.GetVariable<string>("Signal");
            context.SetVariable("Level2", signal);
            var checklist = Constants.Level2Services_Admin.Concat(Constants.Level2Services_HR).Concat(Constants.Level2Services_IT);
            return checklist.Any(s => s == signal);
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

            var level1 = workflowContext.GetVariable("Level1").ToString();
            var level2 = workflowContext.GetVariable("Level2").ToString();
            var content = GenerateContent(level1, level2);

            var contentWorkflowExp = new LiteralExpression<string>(content);

            var bodyText = await expressionEvaluator.EvaluateAsync(contentWorkflowExp, workflowContext, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Done();
        }

        private string GenerateContent(string level1, string level2)
        {
            var content = new StringBuilder($"Category: {level1} > {level2}<br><br>Describe Ticket:<br><br>");
            content.Append("<form action=\"/v1/ticketdetails\" method=\"post\">");
            content.Append("<label for=\"title\">Title:</label><br>");
            content.Append("<input type=\"text\" id=\"title\" name=\"title\"><br>");
            content.Append("<label for=\"description\">Description:</label><br>");
            content.Append("<textarea id=\"description\" name=\"description\"></textarea><br><br>");
            content.Append("<input type=\"submit\" value=\"Submit\">\r\n</form>");
            return content.ToString();
        }
    }
}
