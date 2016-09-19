using Amazon;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TempCheckPiUI.Models;

namespace TempCheckPiUI.Services
{
    public class AWSMobileServices
    {
        private static AWSMobileServices instance;

        public static AWSMobileServices Instance
        {
            get
            {
                return instance ?? (instance = new AWSMobileServices());
            }
        }

        public CognitoAWSCredentials Credentials
        {
            get;
            private set;
        }

        public AmazonDynamoDBClient Client
        {
            get;
            private set;
        }

        public DynamoDBContext Context
        {
            get;
            private set;
        }

        private AWSMobileServices()
        {
            AWSConfigs.LoggingConfig.LogMetrics = true;
            AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;
            AWSConfigs.LoggingConfig.LogMetricsFormat = LogMetricsFormatOption.JSON;
            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.SystemDiagnostics;

            Credentials = new CognitoAWSCredentials(AWSConfiguration.AmazonCognitoIdentityPoolId, AWSConfiguration.AmazonCognitoRegion);
            Client = new AmazonDynamoDBClient(Credentials, AWSConfiguration.AmazonDynamoDBRegion);
            Context = new DynamoDBContext(Client);
        }

        public async Task SaveAsync<T>(T item)
        {
            await Context.SaveAsync(item);
        }

        public async Task DeleteAsync<T>(T item)
        {
            await Context.DeleteAsync(item);
        }
    }
}
