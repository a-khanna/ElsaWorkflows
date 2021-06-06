using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Signaling.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Services;
using ElsaLatest.Models;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace ElsaLatest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BossController : ControllerBase
    {
        private readonly IWorkflowTriggerInterruptor _workflowInterruptor;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly ISignaler _signaler;
        private readonly IElsaContextFactory _elsaContextFactory;

        public BossController(
            IWorkflowTriggerInterruptor workflowInterruptor,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore,
            ISignaler signaler,
            IElsaContextFactory elsaContextFactory)
        {
            _workflowInterruptor = workflowInterruptor;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowDefinitionStore = workflowDefinitionStore;
            _signaler = signaler;
            _elsaContextFactory = elsaContextFactory;
        }

        [HttpGet("invoke-flow")]
        public async Task<IActionResult> InvokeFlow(string keyword)
        {
            var startedWorkflows = await _signaler.TriggerSignalAsync(keyword);
            if (!startedWorkflows.Any())
                return BadRequest("No workflows found for the given keyword");

            var instanceId = startedWorkflows.ElementAt(0).WorkflowInstanceId;
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);

            return Ok(new BossResponse
            {
                InstanceId = workflowInstance.Id,
                Status = workflowInstance.WorkflowStatus
            });
        }

        [HttpGet("get-current-activity-values")]
        public async Task<IActionResult> GetCurrentActivityValues(string instanceId)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);
            var response = new BossResponse
            {
                InstanceId = instanceId,
                Status = workflowInstance.WorkflowStatus
            };
            if (workflowInstance.WorkflowStatus is WorkflowStatus.Cancelled or WorkflowStatus.Faulted or WorkflowStatus.Finished)
                return Ok(response);

            var currentActivityId = workflowInstance?.BlockingActivities.Select(i => i.ActivityId).First();

            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionIdSpecification(workflowInstance.DefinitionId).WithVersionOptions(VersionOptions.Latest));
            var activityDefinition = workflowDefinition.Activities.First(a => a.ActivityId == currentActivityId);

            object taskValues = null;
            if (activityDefinition.Type == "UserTask")
                taskValues = activityDefinition.Properties.ElementAt(0).Expressions["Json"];
            else
                taskValues = new
                {
                    RequiredFields = activityDefinition.Properties.First(p => p.Name == "RequiredFields").Expressions["Json"],
                    Actions = activityDefinition.Properties.First(p => p.Name == "Actions").Expressions["Json"]
                };

            response.Data = taskValues;

            return Ok(response);
        }

        [HttpGet("get-executed-activity-outcome")]
        public async Task<IActionResult> GetExecutedActivityOutcome(string instanceId)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);

            var response = new BossResponse
            {
                InstanceId = instanceId,
                Status = workflowInstance.WorkflowStatus,
                Data = workflowInstance.ActivityOutput.Last(a => a.Value != null).Value
            };

            return Ok(response);
        }

        [HttpPost("execute-step")]
        public async Task<IActionResult> ExecuteStep(UserInput userInput)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(userInput.InstanceId);
            if (workflowInstance.WorkflowStatus is WorkflowStatus.Cancelled or WorkflowStatus.Faulted or WorkflowStatus.Finished)
                return BadRequest("This instance is not active anymore");

            if (!workflowInstance.BlockingActivities.Any())
                return BadRequest("No blocked activities found to be executed in this instance.");

            var currentActivity = workflowInstance?.BlockingActivities.Select(i => new { ActivityId = i.ActivityId, ActivityType = i.ActivityType }).First();

            if (currentActivity.ActivityType == "UserTask")
                await _workflowInterruptor.InterruptActivityAsync(workflowInstance, currentActivity.ActivityId, userInput.UserAction);
            else
                await _workflowInterruptor.InterruptActivityAsync(workflowInstance, currentActivity.ActivityId, userInput);

            return Ok(new BossResponse { InstanceId = userInput.InstanceId, Status = workflowInstance.WorkflowStatus });
        }

        [HttpGet("cancel-flow")]
        public async Task<IActionResult> CancelFlow(string instanceId)
        {
            var dbContext = _elsaContextFactory.CreateDbContext();
            var workflowInstance = dbContext.WorkflowInstances.FirstOrDefault(i => i.Id == instanceId);
            if (workflowInstance == null)
                return BadRequest("No such instance found");

            workflowInstance.WorkflowStatus = WorkflowStatus.Cancelled;
            workflowInstance.CancelledAt = new Instant();
            
            await dbContext.SaveChangesAsync();

            return Ok($"Cancelled workflow with id {instanceId}.");
        }
    }
}
