Employee
--------

src/Employee/CreateEmployee.cs - Lambda function for creating employee
src/Employee/QueryEmployees.cs - Lambda function for querying employee by day 
sam template - template.yaml

Example - Create Employee
-------------------------
`curl -XPOST -d '{"emp_id": "134","emp_name": "Jerry Smith", "emp_type": "Fulltime", "emp_dob": "13-12-1960", "emp_doj": "11-01-2001", "emp_department": "Finance"}' https://<rest_api_id>.execute-api.us-east-2.amazonaws.com/Prod/create_employee`


Example - Query Employees
-------------------------
`curl -XGET http://localhost:7091/query_employees?day=12`