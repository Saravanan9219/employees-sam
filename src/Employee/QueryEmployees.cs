using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using Amazon;
using Amazon.Athena;
using Amazon.Athena.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;


namespace QueryEmployees
{

    public class QueryEmployees
    {

        private const string ATHENA_DB = "employee";
        private const string ATHENA_TEMP_PATH = "s3://test-employees-query-results";

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {

            LambdaLogger.Log("Post Data " + apigProxyEvent.Body);
            string day;
            apigProxyEvent.QueryStringParameters.TryGetValue("day", out day);
            List<Dictionary<String, String>> queryResults = await queryAthena(day.ToString());

            return new APIGatewayProxyResponse
            {
                Body = JsonConvert.SerializeObject(queryResults),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        private async Task<List<Dictionary<String, String>>> queryAthena(String date) {
            using (var athenaClient = new AmazonAthenaClient(Amazon.RegionEndpoint.USEast2)) {
                QueryExecutionContext qContext = new QueryExecutionContext();
                qContext.Database = ATHENA_DB;
                ResultConfiguration resConf = new ResultConfiguration();
                resConf.OutputLocation = ATHENA_TEMP_PATH;
                List<Dictionary<String, String>> items = await runQuery(athenaClient, qContext, resConf, date);
                return items;
            }
        }

        async static Task<List<Dictionary<String, String>>> runQuery(IAmazonAthena client, QueryExecutionContext qContext, ResultConfiguration resConf, String dateToQuery)
        {
            /* Execute a simple query on a table */
            StartQueryExecutionRequest qReq = new StartQueryExecutionRequest() {
                QueryString = $"SELECT * FROM employee where day(employee.emp_dob) = {dateToQuery}",
                QueryExecutionContext = qContext,
                ResultConfiguration = resConf
            };

            try {
                StartQueryExecutionResponse qRes = await client.StartQueryExecutionAsync(qReq);
                /* Call internal method to parse the results and return a list of key/value dictionaries */
                List<Dictionary<String, String>> items = await getQueryExecution(client, qRes.QueryExecutionId);
                return items;
            }
            catch (InvalidRequestException e) {
                LambdaLogger.Log($"Run Error: {e.Message}");
                return new List<Dictionary<String, String>>();
            }
        }
        async static Task<List<Dictionary<String, String>>> getQueryExecution(IAmazonAthena client, String id)
        {
            List<Dictionary<String, String>> items = new List<Dictionary<String, String>>();
            GetQueryExecutionResponse results = null;
            QueryExecution q = null;
            /* Declare query execution request object */
            GetQueryExecutionRequest qReq = new GetQueryExecutionRequest() {
                QueryExecutionId = id
            };
            /* Poll API to determine when the query completed */
            do {
                try {
                    results = await client.GetQueryExecutionAsync(qReq);
                    q = results.QueryExecution;
                    LambdaLogger.Log($"Status: {q.Status.State}... {q.Status.StateChangeReason}");
                    await Task.Delay(5000); //Wait for 5sec before polling again
                }
                catch (InvalidRequestException e)
                {
                    Console.WriteLine("GetQueryExec Error: {0}", e.Message);
                }
            } while(q.Status.State == "RUNNING" || q.Status.State == "QUEUED");

            LambdaLogger.Log($"Data Scanned for {id}: {q.Statistics.DataScannedInBytes} Bytes");
            
            /* Declare query results request object */
            GetQueryResultsRequest resReq = new GetQueryResultsRequest() {
                QueryExecutionId = id,
                MaxResults = 10
            };

            GetQueryResultsResponse resResp = null;
            /* Page through results and request additional pages if available */
            do {
                resResp = await client.GetQueryResultsAsync(resReq);
                /* Loop over result set and create a dictionary with column name for key and data for value */
                foreach(Row row in resResp.ResultSet.Rows.Skip(1))
                {
                    Dictionary<String, String> dict = new Dictionary<String, String>();
                    for (var i=0; i < resResp.ResultSet.ResultSetMetadata.ColumnInfo.Count; i++)
                    {
                        dict.Add(resResp.ResultSet.ResultSetMetadata.ColumnInfo[i].Name, row.Data[i].VarCharValue);
                    }
                    items.Add(dict);
                }

                if (resResp.NextToken != null) {
                    resReq.NextToken = resResp.NextToken;
                }
            } while(resResp.NextToken != null);

            /* Return List of dictionary per row containing column name and value */
            return items;
        }


    }
}