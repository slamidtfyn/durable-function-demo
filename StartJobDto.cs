using System;
using Newtonsoft.Json;

namespace slamidtfyn.durablefunctiondemo
{

    public class StartJobDto {
       
        [JsonProperty("id")]
        public Guid JobId { get; set; }
        
        [JsonProperty("instance")]
        public string InstanceId { get; set; }
        
        [JsonProperty("async")]
        public bool Async { get; set; }
        
        [JsonProperty("delay")]
        public int Delay { get; set; } = 1000; //ms
    }
}
