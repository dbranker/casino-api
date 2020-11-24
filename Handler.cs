using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace AwsDotnetCsharp
{
    public class Handler
    {
        public async Task<APIGatewayProxyResponse> Hello(APIGatewayProxyRequest request, ILambdaContext context)
        {


            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{ \"name\":\"John\", \"age\":30, \"car\":null }",
                Headers = new Dictionary<string, string> {
                      {  "Access-Control-Allow-Origin", "*"},
                      {  "Access-Control-Allow-Credentials", "true"}
// 
         }
            };

            return response;
        }

        // public async Task<APIGatewayProxyResponse> Run(APIGatewayProxyRequest request)
        // {
        //     var requestModel = new GetItemRequest { Id = new Guid(request.PathParameters["id"]) };
        //     var mediator = _serviceProvider.GetService<IMediator>();

        //     var result = await mediator.Send(requestModel);

        //     return result == null ?
        //       new APIGatewayProxyResponse { StatusCode = 404 } :
        //       new APIGatewayProxyResponse { StatusCode = 200, Body = JsonConvert.SerializeObject(result) };
        // }
    }

}
