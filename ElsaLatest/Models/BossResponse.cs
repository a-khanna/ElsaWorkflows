using Elsa.Models;

namespace ElsaLatest.Models
{
    public class BossResponse
    {
        public string InstanceId { get; set; }

        public WorkflowStatus Status { get; set; }

        public dynamic Data { get; set; }
    }
}
