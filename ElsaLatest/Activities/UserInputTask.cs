using System;
using System.Collections.Generic;
using System.Linq;
using Elsa;
using Elsa.Activities.UserTask.Activities;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services.Models;
using ElsaLatest.Models;

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

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            var userInput = context.Input! as UserInput;
            return Actions.Contains(userInput.UserAction, StringComparer.OrdinalIgnoreCase);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var userInput = context.Input! as UserInput;
            return Outcome(userInput.UserAction, userInput.Data);
        }
    }
}
