using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Models;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace CreateEmployee 
{
    public class CreateEmployee 
   {
        private string bucketName = Environment.GetEnvironmentVariable("BucketName");
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ICollection<ValidationResult> errors = null;
            Dictionary<string, string> parseError= null;
            Dictionary<string, dynamic> employeeData = null;
            Employee employee;
            string contentBody;
            int statusCode;

            LambdaLogger.Log("Post Data " + apigProxyEvent.Body);
            employee = getEmployee(apigProxyEvent.Body, out parseError);

            if(parseError != null) {
                return new APIGatewayProxyResponse
                {
                    Body = JsonConvert.SerializeObject(parseError),
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            if(employee.isValid(out errors, out employeeData))
            {
                string keyName = employee.emp_id.ToString();
                await SaveObjectAsync(keyName, employeeData);
                statusCode = 200;
                contentBody = JsonConvert.SerializeObject(employeeData);

            }
            else 
            {
                statusCode = 400;
                contentBody = JsonConvert.SerializeObject(errors);
            }

            return new APIGatewayProxyResponse
            {
                Body = contentBody,
                StatusCode = statusCode,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        private Employee getEmployee(string postData, out Dictionary<string, string> parseError)
        {
            Employee employee = null;
            parseError = null;
            try
            {
                employee = JsonConvert.DeserializeObject<Employee>(postData);
            }
            catch(Exception e) 
            {
                LambdaLogger.Log($"Parsing error: {e.Message}");
                parseError = new Dictionary<string, string>(){
                    {"parseError", "valid json is required."}
                };
            }
            return employee;
        } 

        private async Task SaveObjectAsync(string keyName, Dictionary<string, object> employeeData)
        {
            try {
                var contentBody = JsonConvert.SerializeObject(employeeData);
                var putRequest = new PutObjectRequest {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = contentBody
                };

                IAmazonS3 s3Client = new AmazonS3Client(RegionEndpoint.USEast2);
                PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    } 

}
