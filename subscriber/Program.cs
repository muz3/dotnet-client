using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.ManagedClient;
using MQTTnet.Protocol;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MQTTSubscriberTest
{
    class Subscriber
    {
        public static ManualResetEvent Shutdown = new ManualResetEvent(false);
        static void Main()
        {

            DotNetEnv.Env.Load();
                    
    		// configs, please filling your information accordingly
    		var customClientId = DotNetEnv.Env.GetString("CLIENT_ID"); // format suggested 
    		var loggerName = DotNetEnv.Env.GetString("LOGGER_NAME");
    		var mqttServAddress = DotNetEnv.Env.GetString("MQTT_ADDR"); //subject to change
    		var username = DotNetEnv.Env.GetString("USERNAME"); //subject to change
    		var password = DotNetEnv.Env.GetString("PASSWORD");  //subject to change
    		var topic = DotNetEnv.Env.GetString("TOPIC");

            Thread subscriber = new Thread(async () =>
            {
                // Setup and start a managed MQTT client.
                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithClientId(customClientId)
                        .WithWebSocketServer(mqttServAddress)                        
					    .WithCredentials(username, password)
                        .Build())
                    .Build();

                var factory = new MqttFactory();

                var mqttSubscriberClient = factory.CreateManagedMqttClient(new MqttNetLogger(loggerName));
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                    if (e.TraceMessage.Exception != null)
                    {
                        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                    }

                    Debug.WriteLine('\x2' + trace);
                };

                await mqttSubscriberClient.StartAsync(options);
                Console.WriteLine("mqtt client started\n");

                mqttSubscriberClient.Disconnected += (s, e) =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                };

                mqttSubscriberClient.Connected += (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");
                };

                mqttSubscriberClient.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
             
                };
                            
                await mqttSubscriberClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());
                Console.WriteLine($"Subscribed to {topic}");
                

                Shutdown.WaitOne();
            });

            subscriber.Start();
        }
    }
}