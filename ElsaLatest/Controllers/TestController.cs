using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElsaLatest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserTaskController : ControllerBase
    {
        private readonly IWorkflowTriggerInterruptor _workflowInterruptor;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

        public UserTaskController(
            IWorkflowTriggerInterruptor workflowInterruptor,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore)
        {
            _workflowInterruptor = workflowInterruptor;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowDefinitionStore = workflowDefinitionStore;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserTaskValues(string instanceId)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);
            var currentActivityId = workflowInstance?.BlockingActivities.Select(i => i.ActivityId).First();

            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionIdSpecification(workflowInstance.DefinitionId));
            var activityDefinition = workflowDefinition.Activities.First(a => a.ActivityId == currentActivityId);
            var userTaskValues = activityDefinition.Properties.ElementAt(0).Expressions["Json"];

            return Ok(userTaskValues);
        }

        [HttpGet("trigger")]
        public async Task<IActionResult> Trigger(string instanceId, string userAction)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId);

            var currentActivityId = workflowInstance?.BlockingActivities.Select(i => i.ActivityId).First();
            await _workflowInterruptor.InterruptActivityAsync(workflowInstance, currentActivityId, userAction);

            return Ok($"Interrupted {workflowInstance.Name} workflow.");
        }
    }
}
