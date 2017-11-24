using PowerTranzSDK.Requests;
using PowerTranzSDK.Responses;
using PtzSdk.Win.Samples.Library;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PtzSdk.Win.Samples.Console
{
    class Program
    {
        static PtzSdkLibrary sdk;
        static ManualResetEventSlim quit = new ManualResetEventSlim(false);

        static void Main(string[] args)
        {


            try
            {

                sdk = new PtzSdkLibrary //This is basically a proxy object and hides the SDK integration details.  It is not necessary and is used only to provide the same processing functionality to other sample applications.
                {
                    ApplicationId = "9eb64949-2fbc-4ef0-947f-c148469d37bb", //dedicated ApplicationId for PtzSdk.Win.Samples.Console
                    GatewayKey = Properties.Settings.Default.GatewayKey,
                    PowerTranzId = Properties.Settings.Default.PowerTranzId,
                    PowerTranzPassword = Properties.Settings.Default.PowerTranzPassword,
                    TerminalIdleMessage = "   Gateway Systems",
                    TerminalAddress = "Miura 676"
                };


                var request = new PtzPaymentAuth
                {
                    OrderId = "123456789",
                    TotalAmount = 10.00m,
                    TipAmount = 1.56m,
                    TaxAmount = 2.13m,
                    CurrencyCode = "840",   
                    TerminalId = "01249102",
                    //etc...
                };

                var sw = new Stopwatch(); sw.Start();
                sdk.ConnectBluetooth();  //Connect the terminal
             

                //Event handling. Note that  the PtzSdkLibrary handles all the events and for demonstration purposes just logs the event data.  
                //Here we add a second handler to two of the events to illustrate application specific requirements

                sdk.Terminal.DidConnectTerminal += () =>
                {
                    //We are connected
                    sdk.ClearScreen();
                    
                    System.Console.WriteLine($"\r\nTerminal Connected ({sw.Elapsed.TotalSeconds}s).  Press any key to start a transaction...");
                    System.Console.ReadKey();
                    sdk.StartTransaction(request);  //Send the transaction
                };

                sdk.Terminal.DidFinishTransactionWithResponse += (PtzPaymentResponse r) =>
                {
                    //Transaction is complete. Note that the other error events should also be checked as this event may not fire. 


                    //This is not necessary - just demonstrates clearing the screen after a transaction
                    //Need another thread here so we don't block the thread on which the terminal screen is updated while waiting for console input
                    var tsk = Task.Run(async () =>
                    {
                        await Task.Delay(1000); //just to allow logging to finish in the other handler before prompting
                        System.Console.Write($"\r\n\r\n{r.ResponseMessage}.  Press any key...\r\n");
                        System.Console.ReadKey();
                        sdk.ClearScreen();
                        System.Console.ReadKey();
                        quit.Set();
                    });
                };


                quit.Wait();
            }
            catch (Exception x)
            {
                System.Console.Write(x.ToString());
            }

        }

    }
}


