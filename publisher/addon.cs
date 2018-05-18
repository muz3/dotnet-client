
    private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private Thread _thread;

    protected override void OnStart(string[] args)
    {
      _thread = new Thread(PublisherThreadStart);
      _thread.Name = "My Mqtt Worker Thread";
      _thread.IsBackground = true;
      _thread.Start();

      System.Timers.Timer timer = new System.Timers.Timer(30000);
      timer.Elapsed += new System.Timers.ElapsedEventHandler(MTimedEvent);
      timer.Enabled = true;

    }

    private async void PublisherThreadStart()
    {
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

      // await mqttPublisherClient.StartAsync(options);
      await mqttPublisherClient.StartAsync(options);
      Console.WriteLine("mqtt client started\n");

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
        if (e.HasSucceeded)
        {
          Console.WriteLine("message successfully published!");
        }
        else
        {
          Console.WriteLine("message failed to publish!");
        }
      };

      // infinite loop
      while (!_shutdownEvent.WaitOne(0))
      {
        // Replace the Sleep() call with the work you need to do
        Thread.Sleep(1000);
      }
          
    }

    protected override void OnStop()
    {
      _shutdownEvent.Set();
      if (!_thread.Join(3000))
      { // give the thread 3 seconds to stop
        _thread.Abort();
      }
    }

