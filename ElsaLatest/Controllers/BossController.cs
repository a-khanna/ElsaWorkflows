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

            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionIdSpecification(workflowInstance.DefinitionId));
            var activityDefinition = workflowDefinition.Activities.First(a => a.ActivityId == currentActivityId);
            var userTaskValues = activityDefinition.Properties.ElementAt(0).Expressions["Json"];

            response.Data = userTaskValues;

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

        [HttpGet("execute-step")]
        public async Task<IActionResult> ExecuteStep(string instanceId, string userAction)
        {
            var workflowInstance = await TriggerUserTask(userAction, instanceId);
            return Ok(new BossResponse { InstanceId = instanceId, Status = workflowInstance.WorkflowStatus });
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

        private async Task<WorkflowInstance> TriggerUserTask(string userAction, string instanceId = null, string correlationId = null)
        {
            WorkflowInstance workflowInstance;

            if (!string.IsNullOrWhiteSpace(instanceId))
                workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);
            else
                workflowInstance = await _workflowInstanceStore.FindByCorrelationIdAsync(correlationId);

            var currentActivityId = workflowInstance?.BlockingActivities.Select(i => i.ActivityId).First();
            await _workflowInterruptor.InterruptActivityAsync(workflowInstance, currentActivityId, userAction);

            return workflowInstance;
        }
    }
}
