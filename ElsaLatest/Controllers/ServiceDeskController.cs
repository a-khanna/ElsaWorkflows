using Microsoft.AspNetCore.Mvc;

namespace ElsaLatest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceDeskController : ControllerBase
    {
        [HttpPost("services")]
        public IActionResult GetServiceTypes()
        {
            return new JsonResult(Constants.Services);
        }

        [HttpPost("services/{subtype}")]
        public IActionResult GetServiceSubtypes(string subtype)
        {
            switch (subtype)
            {
                case "Admin":
                    return new JsonResult(Constants.Admin);
                case "IT":
                    return new JsonResult(Constants.IT);
                case "HR":
                    return new JsonResult(Constants.HR);
                default:
                    return BadRequest();
            }
        }
    }

    static class Constants
    {
        public static string[] Services = new string[] { "Admin", "IT", "HR" };

        public static string[] Admin = new string[] { "Desk Setup", "Stationary", "Resource Allocation" };

        public static string[] IT = new string[] { "Software Install", "OS Issue", "Network Issue" };

        public static string[] HR = new string[] { "Grievience", "Policies", "Appraisal" };
    }
}
