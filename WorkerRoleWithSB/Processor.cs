using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerRoleWithSB
{
    class SimpleEventProcessor : IEventProcessor
    {
        public static CloudTable Table;
        EventProcessorHost eventProcessorHost;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        Stopwatch checkpointStopWatch;

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor Shuting Down.  Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("SimpleEventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                try
                {
                    string data = Encoding.UTF8.GetString(eventData.GetBytes());

                    SensorTelemetry telemetry = Newtonsoft.Json.JsonConvert.DeserializeObject<SensorTelemetry>(data);
                    // Create the TableOperation that inserts the customer entity.
                    TableOperation insertOperation = TableOperation.Insert(telemetry);

                    // Execute the insert operation.
                    var result = WorkerRole.table.Execute(insertOperation);
                }
                catch (Exception)
                {
                    //TODO: handle exception
                }
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }
    }
    public class SensorTelemetry : TableEntity
    {
        public SensorTelemetry()
        {
            this.PartitionKey = DateTime.UtcNow.ToString("HHmmss");
            this.RowKey = Guid.NewGuid().ToString();

            MeasurementTime = DateTime.Now;
            ThermometerAmbTemp = 0.0;
            ThermometerObjTemp = 0.0;
            AccelerometerX = 0.0;
            AccelerometerY = 0.0;
            AccelerometerZ = 0.0;
            HumidityInPercent = 0.0;
            MagnetometerX = 0.0;
            MagnetometerY = 0.0;
            MagnetometerZ = 0.0;
            BarometerAmbPres = 0.0;
            GyroscopeX = 0.0;
            GyroscopeY = 0.0;
            GyroscopeZ = 0.0;
        }

        public DateTime MeasurementTime { get; set; }
        public double ThermometerAmbTemp { get; set; }
        public double ThermometerObjTemp { get; set; }
        public double AccelerometerX { get; set; }
        public double AccelerometerY { get; set; }
        public double AccelerometerZ { get; set; }
        public double HumidityInPercent { get; set; }
        public double MagnetometerX { get; set; }
        public double MagnetometerY { get; set; }
        public double MagnetometerZ { get; set; }
        public double BarometerAmbPres { get; set; }
        public double GyroscopeX { get; set; }
        public double GyroscopeY { get; set; }
        public double GyroscopeZ { get; set; }

    }

}
