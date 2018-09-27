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

namespace MQTTPublisherTest
{
    internal class Publisher
    {
        public static ManualResetEvent Shutdown = new ManualResetEvent(false);
        public static IManagedMqttClient mqttPublisherClient;

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
    		var yourXML = "<?xml version=\"1.0\"?>\r\n";  // a sample test xml in the format your are supposed to send

            Thread publisher = new Thread(async () =>
            {
                Console.WriteLine(mqttServAddress);
                // Setup and start a managed MQTT client.
                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithClientId(customClientId)
                        .WithWebSocketServer(mqttServAddress)                                                
                        //.WithTcpServer(mqttServAddress) // use this for local tcp connection 
					    .WithCredentials(username, password)
                        .Build())
                    .Build();

                var factory = new MqttFactory();

                mqttPublisherClient = factory.CreateManagedMqttClient(new MqttNetLogger(loggerName));
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                    if (e.TraceMessage.Exception != null)
                    {
                        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                    }

                    Debug.WriteLine('\x2' + trace);
                };


                Action send = async () =>
                {
                    var msg = new MqttApplicationMessage
                    {
                        Topic = topic,
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = true,
                        Payload = System.Text.Encoding.UTF8.GetBytes( 
                            Newtonsoft.Json.JsonConvert.SerializeObject(new {
                                                        data = yourXML,  //xml string needs to encoded and escaped utf-8
                                                        auth =  new {
                                                            access_token = "",  // subject to change later
                                                            secret_token = "" // subject to change later
                                                        }
                                                    })
                        )		
                    };
                    await mqttPublisherClient.PublishAsync(msg);
                    Console.WriteLine($"Published topic: {msg.Topic}");
                };

                mqttPublisherClient.Disconnected += (s, e) =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                };

                mqttPublisherClient.Connected += (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###"); 
                    for (var i = 1; i <= 1; i++)
                    {
                        Console.WriteLine($"the number of message sent: {i}");
                        send(); // sending 1000 messages async
                    }
                   
                };

                mqttPublisherClient.ApplicationMessageProcessed += (s, e) =>
                {                       
                    if ( e.HasSucceeded ) {
                        Console.WriteLine("message successfully published!");
                    }
                    else {
                        Console.WriteLine("message failed to publish!");
                    }                  
                }; 
              
                try {
                    await mqttPublisherClient.StartAsync(options);
                    Console.WriteLine("mqtt client started\n");
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
               
                Shutdown.WaitOne();
                // await mqttPublisherClient.StopAsync();
            });

            publisher.Start();
            Console.WriteLine("testing dotnet mqtt client...");
        }
        
    }
}

