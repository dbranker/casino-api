using Amazon.Lambda.Core;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using CasinoLibrary.Models;
using CasinoLibrary.Cleaning;
using DynamoDBLibrary;
using DynamoDBLibrary.Methods;
using Amazon.DynamoDBv2.DocumentModel;

using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using DynamoDBLibrary.DBContext;

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
                      {  "Access-Control-Allow-Headers", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
                }
            };

            return response;
        }
        public async Task<APIGatewayProxyResponse> InsertEntryDate(APIGatewayProxyRequest request, ILambdaContext context)
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
                        var ied = DBUtilityFactory.PutEntryDate(DBUtilityFactory.CreateDyanmoDBContext());
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
        //TODO: May not need this method anymore
        public async Task<APIGatewayProxyResponse> GetCompanies(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var bodyMessage = String.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            var input = request.Body;
            var getCompanies = DBUtilityFactory.GetCompanies(DBUtilityFactory.CreateDyanmoDBContext());
            try
            {
                bodyMessage = JsonConvert.SerializeObject(getCompanies.Execute(input).Select(x => x.ToJson())).ToString();
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

        public async Task<APIGatewayProxyResponse> GetCompaniesByEmployee(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var bodyMessage = String.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            var jwtHandler = new JwtSecurityTokenHandler();
            //grab employeeid
            var token = jwtHandler.ReadToken(request.Headers["Authorization"].Replace("Bearer ", "")) as JwtSecurityToken;
            var identity = token.Payload.Sub;
            var getCompanies = DBUtilityFactory.GetCompanies(DBUtilityFactory.CreateDyanmoDBContext());            
                try
                {
                    bodyMessage = JsonConvert.SerializeObject(getCompanies.Execute(identity).Select(x => x.ToJson()).ToArray());
                }
                catch (Exception ex)
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

        public async Task<APIGatewayProxyResponse> GetEntries(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var jRequestID = context.AwsRequestId;
            //Mock Lmabda function doesn't LowerCase key, Prod does
            //TODO: Ask Jon about this            
            LambdaLogger.Log("RUN: " + jRequestID + ", CONTEXT: " + JsonConvert.SerializeObject(context));
            LambdaLogger.Log("RUN: " + jRequestID + ", EVENT: GetEntries");
            LambdaLogger.Log("RUN: " + jRequestID + ", ARGS: " + JsonConvert.SerializeObject(request));            
            //Set Template Response
            APIGatewayProxyResponse response = new APIGatewayProxyResponse
            {
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"},
                      {  "Content-Type","application/json" }                      
                }
            };
            var bodyMessage = String.Empty;
            var compVar = String.Empty;
            try
            {
                compVar = request.QueryStringParameters.ContainsKey("companyid") ? "companyid" : "CompanyID";
            }
            catch (Exception ex)
            {
                LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: " + ex.Message);
                response.Body = "{ Status: Failed , Message: EntryDate wasn't a proper MM/dd/yyyy date}";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            using (var db = DBUtilityFactory.CreateDyanmoDBContext() as IDisposable)
            {
                var getEntries = DBUtilityFactory.GetEntries(db as IDynamoDBContext);
                if (!request.Headers.ContainsKey("Authorization"))
                {
                    LambdaLogger.Log("ERROR: Authorization wasn't found. Check that Authorizer has been added or Cognito is alive");
                    response.Body = "{ Status: Failed , Message: Authorization issues, contact administrator}";
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return response;
                }
                else if (!request.QueryStringParameters.ContainsKey(compVar))
                {
                    LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: CompanyID wasn't found.");
                    response.Body = "{ Status: Failed , Message: CompanyID parameter was not found}";
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return response;
                }
                else
                {
                    var jwtHandler = new JwtSecurityTokenHandler();
                    var token = jwtHandler.ReadToken(request.Headers["Authorization"].Replace("Bearer ", "")) as JwtSecurityToken;
                    string identity = token.Payload.Sub;
                    Guid companyID;
                    Guid employeeID;
                    DateTime entryDate;
                    string[] formats = { "MM/dd/yyyy" };
                    try
                    {
                        if (!Guid.TryParse(identity, out employeeID))
                        {
                            LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: Couldn't parse UserID into Guid. Check [token.Payload.Sub] jwt protocol");
                            response.Body = "{ Status: Failed , Message: Authorization issues, contact administrator}";
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return response;
                        }
                        if (!Guid.TryParse(request.QueryStringParameters[compVar], out companyID))
                        {
                            LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: Couldn't parse CompanyID into Guid.");
                            response.Body = "{ Status: Failed , Message: CompanyID could not be parsed}";
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return response;
                        }
                        if (request.QueryStringParameters.ContainsKey("EntryDate"))
                            entryDate = DateTime.Parse(request.QueryStringParameters["EntryDate"]);
                        else
                            entryDate = DateTime.UtcNow;
                        //Go back 30 days and forward 30 Days
                        DateTime startDate = entryDate.AddDays(-30);
                        DateTime endDate = entryDate.AddDays(30);
                        var getEntriesResults = getEntries.Execute(companyID, employeeID, startDate, endDate);
                        var entryResponses = getEntriesResults.Select(x => new EntryResponse()
                        {
                            Entry = (CompanyEntry)JsonConvert.DeserializeObject<CompanyEntry>(x["EntryValues"]),
                            EntryDate = DateTime.Parse(x["EntryDate"]),
                            EntryID = Guid.Parse(x["EntryID"])
                        }).ToList();
                        var finalResponse = new CompanyEntryResponse()
                        {
                            Entries = entryResponses
                        };
                        response.Body = JsonConvert.SerializeObject(finalResponse);
                        response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    catch (FormatException ex)
                    {
                        LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: " + ex.Message);
                        response.Body = "{ Status: Failed , Message: EntryDate wasn't a proper MM/dd/yyyy date}";
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        LambdaLogger.Log("RUN: " + jRequestID + ", ERROR: " + ex.Message);
                        response.Body = "{ Status: Failed , Server issues. Contact Admin}";
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return response;
                    }
                }
            }
            return response;
        }

    }

}
