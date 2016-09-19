using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TempCheckPiUI
{
    public class AWSConfiguration
    {
        //AWS MobileHub user agent string
        //public static readonly string AWS_MOBILEHUB_USER_AGENT = "MobileHub fa541242-bcd0-4950-9b9f-dcc48de09147 aws-my-sample-app-android-v0.8";
        //public static readonly Regions AMAZON_DEFAULT_REGION = Regions.US_EAST_1;

        //public static readonly RegionEndpoint AmazonCognitoRegion = RegionEndpoint.EUWest1;
        //public static readonly string AmazonCognitoIdentityPoolId = "eu-west-1:4f1944be-8354-48c7-a684-55a8fb002470";
        //public static readonly RegionEndpoint AmazonDynamoDBRegion = RegionEndpoint.EUCentral1;

        public static readonly RegionEndpoint AmazonCognitoRegion = RegionEndpoint.USEast1;
        public static readonly string AmazonCognitoIdentityPoolId = "us-east-1:44ed8f84-e002-4579-b2f7-e90504a85295";
        public static readonly RegionEndpoint AmazonDynamoDBRegion = RegionEndpoint.USEast1;
    }
}
