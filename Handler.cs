using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using CasinoLibrary.Models;
using CasinoLibrary.Cleaning;
using DynamoDBLibrary;
using DynamoDBLibrary.Methods;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace CasinoApi
{
    public class Handler
    {
        public async Task<APIGatewayProxyResponse> Hello(APIGatewayProxyRequest request, ILambdaContext context)
        {

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{ \"name\":\"stop\", \"age\":30, \"car\":null }",
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };

            return response;
        }
        public async Task<APIGatewayProxyResponse> InsertEntryDate(APIGatewayProxyRequest request)
        {
            //grab userid
            var jwtHandler = new JwtSecurityTokenHandler();
            var token = jwtHandler.ReadToken(request.Headers["Authorization"].Replace("Bearer ", "")) as JwtSecurityToken;
            var identity = token.Payload.Sub;


            var bodyMessage = String.Empty;
            HttpStatusCode statusCode=  HttpStatusCode.OK;
            if (!request.PathParameters.ContainsKey("CompanyID"))
            {
                bodyMessage = "{ Status: Failed , Message: Missing CompanyID value}";
                statusCode = HttpStatusCode.BadRequest;
            } 
            else if (!request.PathParameters.ContainsKey("EntryDate"))
            {
                bodyMessage = "{ Status: Failed , Message: Missing EntryDate value}";
                statusCode = HttpStatusCode.BadRequest;
            }
            else
            {
                try
                {
                    //Get Company 
                    var jsonResponse = request.Body;
                    Guid companyID;
                    Guid employeeID;
                    DateTime entryDate;
                    CompanyEntry ce;
                    Dictionary<string, string> errorList;
                    if (CompanyEntryConvert.TryParse(jsonResponse, out ce, out errorList) 
                        && Guid.TryParse(request.PathParameters["CompanyID"],out companyID) 
                        && Guid.TryParse(request.PathParameters["EmployeeID"], out employeeID)
                        && DateTime.TryParse(request.PathParameters["EntryDate"], out entryDate))
                    {
                        var ied = new InsertEntryDate(DBUtilityFactory.DyanmoDBContext());
                        ied.Execute(companyID, employeeID, entryDate, ce);
                        bodyMessage = "{ Status: Success }";                        
                    } else
                    {
                        bodyMessage = "{ Status: Failed , Message: Invalid variables sent}";
                        statusCode = HttpStatusCode.BadRequest;
                    }

                }
                catch
                {
                    bodyMessage = "{ Status: Failed , Message: Server Issues}";
                    statusCode = HttpStatusCode.InternalServerError;
                }
            }
            //TODO: Fix Field Validation         
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = bodyMessage,
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };
            return response;
        }
        //Check
        public async Task<APIGatewayProxyResponse> GetCompanies(APIGatewayProxyRequest request)
        {
            var bodyMessage = String.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            var input = request.Body;
            var getCompanies = new GetCompanies(DBUtilityFactory.DyanmoDBContext());
            try
            {
                bodyMessage = JsonConvert.SerializeObject(getCompanies.Execute(input).Select(x => x.ToJson()).ToArray());
            }
            catch
            {
                bodyMessage = "{ Status: Failed , Message: Server Issues}";
                statusCode = HttpStatusCode.InternalServerError;
            }
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = bodyMessage,
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };

            return response;
        }

        public async Task<APIGatewayProxyResponse> GetCompaniesByEmployee(APIGatewayProxyRequest request)
        {
            var bodyMessage = String.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            var jwtHandler = new JwtSecurityTokenHandler();
            //grab employeeid
            var token = jwtHandler.ReadToken(request.Headers["Authorization"].Replace("Bearer ", "")) as JwtSecurityToken;
            var identity = token.Payload.Sub;
            var getCompanies = new GetEmployeeCompanyMapping(DBUtilityFactory.DyanmoDBContext());
            Guid employee;
            if (Guid.TryParse(identity, out employee))
                try
                {
                    bodyMessage = JsonConvert.SerializeObject(getCompanies.Execute(employee).Select(x => x.ToJson()).ToArray());
                }
                catch (Exception ex)
                {
                    bodyMessage = "{ Status: Failed , Message: Server Issues}";
                    statusCode = HttpStatusCode.InternalServerError;
                }
            else
            {
                bodyMessage = "{ Status: Failed , Message: EmployeeID is not registered}";
                statusCode = HttpStatusCode.InternalServerError;
            }
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = bodyMessage,
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };
            return response;
        }

        public async Task<APIGatewayProxyResponse> GrabEntriesByYear(APIGatewayProxyRequest request)
        {
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{ \"name\":\"stop\", \"age\":30, \"car\":null }",
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };

            return response;
        }

    }

}
