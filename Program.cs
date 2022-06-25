using Forge.Logging;
using Forge.Logging.Utils;
using Forge.Logging.Log4net;
using Sesame.Communication.Data.Indentification;
using Sesame.Communication.External.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETFwConsoleAppWithServerCalls
{

    class Program
    {

        private static ILog LOGGER = null;

        private readonly AutoResetEvent mFaultHandlerEvent = new AutoResetEvent(false);
        private Thread mFaultHandlerThread = null;
        private AutoResetEvent mFaultHandlerStopEvent = null;
        private bool mFaultHandlerRunning = true;

        static void Main(string[] args)
        {
            // initializing logger
            Log4NetManager.InitializeFromAppConfig();
            LOGGER = LogManager.GetLogger(typeof(Program));
            LogUtils.LogAll();

            Console.WriteLine("Running tests...");

            Program p = new Program();
            p.Initialize();
            p.RunTestCalls();
            p.Shutdown();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public void Initialize()
        {
            // initialize communication system
            ClientProxyBase.SourceId = ClientIdGenerator.GenerateId(ClientTypeEnum.External);
            ClientProxyBase.ConfigureClientProxyForCallback(new ConfigurationForCallback("TcpEndpointForCallbackMode"));
            ClientProxyBase.Faulted += ClientProxyBase_Faulted;
            try
            {
                ClientProxyBase.Open();
            }
            catch (Exception ex)
            {
                LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                mFaultHandlerEvent.Set();
            }
            mFaultHandlerThread = new Thread(new ThreadStart(FaultHandlerThreadMain));
            mFaultHandlerThread.Name = "FaultHandlerThread";
            mFaultHandlerThread.Start();
        }

        public void RunTestCalls()
        {
            using (ComProxy proxy = new ComProxy())
            {
                // query database(s)
                DatabaseResponse dbResponse = proxy.GetDatabases();
                foreach (KeyAndValueItem item in dbResponse.Items)
                {
                    LOGGER.Info($"DbId: {item.Id}, Name: {item.Name}");
                    // query each db details
                    SPDatabaseDetailsResponse dbDetails = proxy.GetDatabaseDetails(new SPDatabaseDetailsRequest() { DatabaseId = item.Id });
                    LOGGER.Info($"Currency: {dbDetails.Currency}");
                    LOGGER.Info($"Population: {dbDetails.Population.ToString()}");
                    LOGGER.Info($"Sample: {dbDetails.Sample.ToString()}");
                    LOGGER.Info("Languages:");
                    foreach (KeyAndValueItem langItem in dbDetails.Languages)
                    {
                        LOGGER.Info($"{langItem.Id}, name: {langItem.Name}");
                    }
                    LOGGER.Info("-----------");
                }
            }
            Console.WriteLine("Success! Check your log file.");
        }

        public void Shutdown()
        {
            ClientProxyBase.Faulted -= ClientProxyBase_Faulted;

            mFaultHandlerStopEvent = new AutoResetEvent(false);
            mFaultHandlerRunning = false;
            mFaultHandlerEvent.Set();
            mFaultHandlerStopEvent.WaitOne();
            mFaultHandlerStopEvent.Dispose();
            mFaultHandlerEvent.Dispose();

            ClientProxyBase.Close();
        }

        private void ClientProxyBase_Faulted(object sender, EventArgs e)
        {
            mFaultHandlerEvent.Set();
        }

        private void FaultHandlerThreadMain()
        {
            while (mFaultHandlerRunning)
            {
                mFaultHandlerEvent.WaitOne();
                if (mFaultHandlerRunning)
                {
                    try
                    {
                        ClientProxyBase.Open();
                    }
                    catch (Exception ex)
                    {
                        LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                        Thread.Sleep(1000);
                    }
                }
            }
            mFaultHandlerStopEvent.Set();
        }

    }

}
