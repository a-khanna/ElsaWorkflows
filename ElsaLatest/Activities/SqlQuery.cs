using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Elsa;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace ElsaLatest.Activities
{
    [Action(
        Category = "Database",
        DisplayName = "Sql Query",
        Description = "Executes a sql query and writes an http response",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SqlQuery : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _sqlConnectionString;

        public SqlQuery(IHttpContextAccessor httpContextAccessor, IStringLocalizer<SqlQuery> localizer, IConfiguration configuration)
        {
            T = localizer;
            _httpContextAccessor = httpContextAccessor;
            _sqlConnectionString = configuration.GetConnectionString("ElsaDb");
        }

        private IStringLocalizer T { get; }

        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.Dropdown,
            Hint = "The HTTP status code to write.",
            Options = new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.Redirect, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Conflict },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DefaultValue = HttpStatusCode.OK
        )]
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// The content to send along with the response
        /// </summary>
        [ActivityProperty(Hint = "The HTTP content to write.", UIHint = ActivityPropertyUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Content { get; set; }

        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.Dropdown,
            Hint = "The SQL query to execute",
            Options = new[] { "text/plain", "text/html", "application/json", "application/xml" },
            DefaultValue = "text/plain",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ContentType { get; set; } = "text/plain";

        /// <summary>
        /// The headers to send along with the response.
        /// </summary>
        [ActivityProperty(Hint = "Additional headers to write.", UIHint = ActivityPropertyUIHints.Json)]
        public HttpResponseHeaders? ResponseHeaders { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = (int)StatusCode;
            response.ContentType = ContentType;

            var headers = ResponseHeaders;

            if (headers != null)
            {
                foreach (var header in headers)
                    response.Headers[header.Key] = header.Value;
            }

            string bodyText;
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                bodyText = JsonSerializer.Serialize(connection.Query(Content));
            }

            if (!string.IsNullOrWhiteSpace(bodyText))
                await response.WriteAsync(bodyText, context.CancellationToken);

            return Done();
        }
    }
}
