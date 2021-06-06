using System.Collections.Generic;
using Elsa;
using Elsa.Activities.UserTask.Activities;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services.Models;

namespace ElsaLatest.Activities
{
    [Trigger(
        Category = "User Tasks",
        Description = "Triggers when a user action along with input data is received.",
        Outcomes = new[] { OutcomeNames.Done, "x => x.state.actions" }
    )]
    public class UserInputTask : UserTask
    {
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.MultiText,
            Hint = "Provide a list of required fields",
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public ICollection<string> RequiredFields { get; set; } = new List<string>();

        public UserInputTask(IContentSerializer serializer) : base(serializer)
        {
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            return Done(1234567);
        }
    }
}
