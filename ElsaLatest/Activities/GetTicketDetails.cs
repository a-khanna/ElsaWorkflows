using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Signaling.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace ElsaLatest.Activities
{
    [Action(
        Category = "Ginger",
        DisplayName = "Get ticket details",
        Description = "Gets title and description of the ticket from the user",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class GetTicketDetails : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private IStringLocalizer T { get; }

        public GetTicketDetails(IHttpContextAccessor httpContextAccessor, IStringLocalizer<GetLevel1Services> localizer)
        {
            T = localizer;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            if (context.Input is Signal triggeredSignal)
            {
                context.SetVariable("Level2", triggeredSignal.SignalName);
                var checklist = Constants.Level2Services_Admin.Concat(Constants.Level2Services_HR).Concat(Constants.Level2Services_IT);
                return checklist.Any(s => string.Equals(triggeredSignal.SignalName, s, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? OnResume(context) : Suspend();

        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = 200;
            response.ContentType = "text/html";

            var level1 = context.GetVariable("Level1").ToString();
            var level2 = context.GetVariable("Level2").ToString();
            var content = GenerateContent(level1, level2);

            if (!string.IsNullOrWhiteSpace(content))
            {
                await response.WriteAsync(content, context.CancellationToken);
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
