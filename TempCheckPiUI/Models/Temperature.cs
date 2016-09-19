using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TempCheckPiUI.Models
{
    [DynamoDBTable("tempcheck-mobilehub-415559713-temperature")]
    public class Temperature
    {
        [DynamoDBHashKey("deviceId")]
        public string DeviceId { get; set; }

        [DynamoDBProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [DynamoDBProperty("value")]
        public double Value { get; set; }
    }
}
