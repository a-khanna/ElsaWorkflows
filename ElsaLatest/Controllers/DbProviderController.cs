using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace ElsaLatest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbProviderController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new List<SelectListItem>
            {
                new SelectListItem {Text = "SQL Server", Value = "SQL Server"},
                new SelectListItem {Text = "MongoDb", Value = "MongoDb"}
            });
        }

        [HttpGet("databases")]
        public IActionResult GetDatabases(string dbProvider)
        {
            var dbList = new List<SelectListItem>
            {
                new SelectListItem {Text = "Db1", Value = "Db1"},
                new SelectListItem {Text = "Db2", Value = "Db2"},
                new SelectListItem {Text = "Db3", Value = "Db3"}
            };

            return Ok(dbList);
        }

        [HttpGet("db-schema")]
        public IActionResult GetDbSchema(string dbProvider, string database)
        {
            var schemaList = new List<SelectListItem>();

            if (dbProvider == "SQL Server")
            {
                schemaList.Add(new SelectListItem { Text = "Table1", Value = "Table1" });
                schemaList.Add(new SelectListItem { Text = "Table2", Value = "Table2" });
                schemaList.Add(new SelectListItem { Text = "Table3", Value = "Table3" });
            }
            else
            {
                schemaList.Add(new SelectListItem { Text = "Collection1", Value = "Collection1" });
                schemaList.Add(new SelectListItem { Text = "Collection2", Value = "Collection2" });
                schemaList.Add(new SelectListItem { Text = "Collection3", Value = "Collection3" });
            }
            return Ok(schemaList);
        }
    }

    public class SelectListItem
    {
        public string Text { get; set; }

        public string Value { get; set; }
    }
}
