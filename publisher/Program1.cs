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
using System.ServiceProcess;

public partial class BuildXMLService : ServiceBase
{

  private IManagedMqttClient mqttPublisherClient;
  public BuildXMLService()
  {
    InitializeComponent();
  }
  string path = Application.StartupPath + @"\LOG\";
  string path1 = Application.StartupPath + @"\XML\";
  string customClientId = "test-pub-01";//Guid.NewGuid().ToString(); //"test-health-check-internal"; // format suggested 
  string mqttServAddress = "wss://xtrace200.verachain.io"; //"wss://xtrace200.demo1.csig.vera.bcgdv.ventures"; //subject to change
  string username = "root"; //subject to change
  string password = "secret";  //subject to change
  string xtraceTopic = "labels/commit";
  string loggerName = "test-pub-log";
  MqttFactory factory = new MqttFactory();

  protected override void OnStart(string[] args)
  {
    Thread MyThread = new Thread(new ThreadStart(PublisherThreadStar));
    MyThread.Start();
    base.OnStart(args);

    // System.Timers.Timer timer = new System.Timers.Timer(30000);
    // timer.Elapsed += new System.Timers.ElapsedEventHandler(MTimedEvent);
    // timer.Enabled = true;
  }

  private void PublisherThreadStar()
  {
    //Thread publisher = new Thread(async () =>
    //{
    var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithClientId(customClientId)
                .WithWebSocketServer(mqttServAddress)
                .WithCredentials(username, password)
                .Build())
            .Build();

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


    mqttPublisherClient.Disconnected += (s, e) =>
    {
      Console.WriteLine("### DISCONNECTED FROM SERVER ###");
    };

    mqttPublisherClient.Connected += (s, e) =>
    {
      Console.WriteLine("### CONNECTED WITH SERVER ###");      
    };

    mqttPublisherClient.ApplicationMessageProcessed += (s, e) =>
    {
          // Console.WriteLine(JsonConvert.SerializeObject(e));
          // Console.WriteLine(JsonConvert.SerializeObject(s));
          // Console.WriteLine($"Processed Message: + Topic = {e.ApplicationMessage.Topic}");
          if (e.HasSucceeded)
      {
        Console.WriteLine("message successfully published!");
      }
      else
      {
        Console.WriteLine("message failed to publish!");
      }
    };

    // await mqttPublisherClient.StartAsync(options);
    await mqttPublisherClient.StartAsync(options);

    // TODO: bug in library, https://github.com/chkr1011/MQTTnet/issues/191
    while (!mqttPublisherClient.IsConnected)
    {
      Thread.Sleep(5000);
    }


    Console.WriteLine("mqtt client started\n");


  }

}