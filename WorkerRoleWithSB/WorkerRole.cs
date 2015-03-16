using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRoleWithSB
{
    public class WorkerRole : RoleEntryPoint
    {
        const string EventHubName = "[HUB NAME]";

        public static CloudTable table;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.WriteLine("Starting processing of messages");



            CompletedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("SensorTag");

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            EventProcessorHost eventProcessorHost = new EventProcessorHost(Guid.NewGuid().ToString(), EventHubName, EventHubConsumerGroup.DefaultGroupName, CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHub.ConnectionString"), CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString"));
            eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>().Wait();


            return base.OnStart();
        }

        public override void OnStop()
        {

            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
