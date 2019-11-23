using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Newtonsoft.Json;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;


namespace Employee.Tests
{
    public class CreateEmployeeTest
    {
        private static readonly HttpClient client = new HttpClient();

        [Fact]
        public async Task TestCreateEmployeeFunctionHandler()
        {
            var request = new APIGatewayProxyRequest();
            var context = new TestLambdaContext();
            Environment.SetEnvironmentVariable("BucketName", "test-employees");
            Dictionary<string, string> inputEmployeeData = new Dictionary<string, string>
            {
              {"emp_id", "132"},
              {"emp_name", "Rick Sanchez"},
              {"emp_type", "Fulltime"},
              {"emp_dob", "12-10-1984"},
              {"emp_doj", "10-01-2001"},
              {"emp_department", "Finance"}
            };
            request.Body = JsonConvert.SerializeObject(inputEmployeeData);

            Dictionary<string, dynamic> responseData = new Dictionary<string, dynamic>
            {
              {"emp_id", 132},
              {"emp_name", "Rick Sanchez"},
              {"emp_type", "Fulltime"},
              {"emp_dob", "1984-10-12 00:00:00"},
              {"emp_doj", "2001-01-10 00:00:00"},
              {"emp_department", "Finance"}
            };

            var expectedResponse = new APIGatewayProxyResponse
            {
                Body = JsonConvert.SerializeObject(responseData),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            var function = new CreateEmployee.CreateEmployee();
            var response = await function.FunctionHandler(request, context);

            Console.WriteLine("Lambda Response: \n" + response.Body);
            Console.WriteLine("Expected Response: \n" + expectedResponse.Body);

            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateEmployeeInvalidJson()
        {
            var request = new APIGatewayProxyRequest();
            var context = new TestLambdaContext();
            Environment.SetEnvironmentVariable("BucketName", "test-employees");
            request.Body = "Invalid data";

            Dictionary<string, string> responseData = new Dictionary<string, string>
            {
              {"parseError", "valid json is required."}
            };

            var expectedResponse = new APIGatewayProxyResponse
            {
                Body = JsonConvert.SerializeObject(responseData),
                StatusCode = 400,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            var function = new CreateEmployee.CreateEmployee();
            var response = await function.FunctionHandler(request, context);

            Console.WriteLine("Lambda Response: \n" + response.Body);
            Console.WriteLine("Expected Response: \n" + expectedResponse.Body);

            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
        }

        [Fact]
        public void TestEmployeeDateValidation() {
            var data = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
              {"emp_id", "132"},
              {"emp_name", "Rick Sanchez"},
              {"emp_type", "Fulltime"},
              {"emp_dob", "12/10/1984"},
              {"emp_doj", "10/01/2001"},
              {"emp_department", "Finance"}
            });
            Models.Employee employee = JsonConvert.DeserializeObject<Models.Employee>(data);
            ICollection<ValidationResult> errors;
            Dictionary<string,dynamic> validationData;
            Assert.False(employee.isValid(out errors, out validationData));
            Assert.True(errors.Any(error => error.MemberNames.Contains("emp_dob") &&
                                             error.ErrorMessage == "Invalid date format. It should dd-MM-yyyy"));
            Assert.True(errors.Any(error => error.MemberNames.Contains("emp_doj") &&
                                             error.ErrorMessage == "Invalid date format. It should dd-MM-yyyy"));

        }

        [Fact]
        public void TestEmployeeDepartmentValidation() {
            var data = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
              {"emp_id", "132"},
              {"emp_name", "Rick Sanchez"},
              {"emp_type", "Fulltime"},
              {"emp_dob", "12-10-1984"},
              {"emp_doj", "10-01-2001"},
              {"emp_department", "Invalid Department"}
            });
            Models.Employee employee = JsonConvert.DeserializeObject<Models.Employee>(data);
            ICollection<ValidationResult> errors;
            Dictionary<string,dynamic> validationData;
            Assert.False(employee.isValid(out errors, out validationData));
            Assert.True(errors.Any(error => error.MemberNames.Contains("emp_department") &&
                                             error.ErrorMessage == "Valid choices are Finance, HR, IT, Administration"));

        }

        [Fact]
        public void TestEmployeeTypeValidation() {
            var data = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
              {"emp_id", "132"},
              {"emp_name", "Rick Sanchez"},
              {"emp_type", "Invalid Type"},
              {"emp_dob", "12-10-1984"},
              {"emp_doj", "10-01-2001"},
              {"emp_department", "Finance"}
            });
            Models.Employee employee = JsonConvert.DeserializeObject<Models.Employee>(data);
            ICollection<ValidationResult> errors;
            Dictionary<string,dynamic> validationData;
            Assert.False(employee.isValid(out errors, out validationData));
            Assert.True(errors.Any(error => error.MemberNames.Contains("emp_type") &&
                                             error.ErrorMessage == "Valid choices are Fulltime, Intern, Contractor"));

        }
    }
}