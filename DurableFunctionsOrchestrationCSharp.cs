using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace slamidtfyn.durablefunctiondemo
{
    public static class DurableFunctionsOrchestrationCSharp
    {

        /* trigger durable function based on a http request */
        /*
         * given following input
         * {
         *   "id": <guid>,
         *   "async": false|true,
         *   "delay": xxx (milliseconds optional default 1000)
         * }
         *
         * will either wait for completion or return status urls - if completion times out set of status urls will be returned instead making the request async
         * the given delay will simulate a long running process
        */

        [FunctionName(nameof(HttpStart))]
        public static async Task<HttpResponseMessage> HttpStart(
                   [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
                   [OrchestrationClient]DurableOrchestrationClient starter,
                   ILogger log)
        {
            string value = await req.Content.ReadAsStringAsync();

            StartJobDto data = Newtonsoft.Json.JsonConvert.DeserializeObject<StartJobDto>(value);
            string instanceId = await starter.StartNewAsync(nameof(DurableFunctionsOrchestration), data);



            log.LogInformation($"Started orchestration with ID = '{instanceId}' for {data.JobId}.");
            if (!data.Async)
            {
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            }
            else
                return starter.CreateCheckStatusResponse(req, instanceId);


        }

        /* trigger durable function based on a queue entry
         * is always async
         * uses same input as HttpStart function
         */
        [FunctionName(nameof(QueueStart))]
        public static async Task QueueStart([QueueTrigger(nameof(QueueStart), Connection = "csb15feceb925a5x4498xa55_STORAGE")]StartJobDto data,
         [OrchestrationClient]DurableOrchestrationClient starter,
         [Queue(nameof(HandleApiReplyQueue)), StorageAccount("csb15feceb925a5x4498xa55_STORAGE")] ICollector<StartJobDto> msg,
         ILogger log)
        {

            string instanceId = await starter.StartNewAsync(nameof(DurableFunctionsOrchestration), data);

            msg.Add(data);

        }

        /* clean up in reply queue after a queue start */
        [FunctionName(nameof(HandleApiReplyQueue))]
        public static async Task HandleApiReplyQueue([QueueTrigger(nameof(HandleApiReplyQueue), Connection = "csb15feceb925a5x4498xa55_STORAGE")]StartJobDto data,
                                                   ILogger log)
        {
            await Task.Run(() =>
            {
                log.LogInformation($"Handled Job: {data.JobId}");
            });
        }



        #region durable function
        [FunctionName(nameof(DurableFunctionsOrchestration))]
        public static async Task<StartJobDto> DurableFunctionsOrchestration(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            StartJobDto input = context.GetInput<StartJobDto>();

            input.InstanceId = context.InstanceId;

            await context.CallActivityAsync(nameof(CallChildApi), input);


            var result = await context.WaitForExternalEvent<StartJobDto>(input.JobId.ToString());
            
            return result;
        }





        [FunctionName(nameof(CallChildApi))]
        public static async Task CallChildApi([ActivityTrigger]StartJobDto dto, [Queue(nameof(CallChildApi)), StorageAccount("csb15feceb925a5x4498xa55_STORAGE")] ICollector<StartJobDto> msg)
        {
            await Task.Delay(dto.Delay); // wait xx milliseconds
            await Task.Run(() =>
            {
                msg.Add(dto);
            });
        }

        [FunctionName(nameof(HandleChildApiQueue))]
        public static async Task HandleChildApiQueue([QueueTrigger(nameof(CallChildApi), Connection = "csb15feceb925a5x4498xa55_STORAGE")]StartJobDto data,
                                                    [OrchestrationClient]DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(data.InstanceId, data.JobId.ToString(), data);
        }

        #endregion

    }
}