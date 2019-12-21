using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Miniblog.Core.Models
{
    public class CaptchaResponse
    {
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "error-codes")]
        public IEnumerable<string> ErrorCodes { get; set; }
    
        [JsonProperty(PropertyName = "challenge_ts")]
        public DateTime ChallengeTime { get; set; }

        public string HostName { get; set; }

        public double Score { get; set; }

        public string Action { get; set; }
    }
}
