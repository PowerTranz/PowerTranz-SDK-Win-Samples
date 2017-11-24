using PowerTranzSDK.Requests;
using PowerTranzSDK.Responses;
using PtzSdk.Win.Samples.Library;
using System;
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

                sdk = new PtzSdkLibrary();
                sdk.ApplicationId = "9eb64949-2fbc-4ef0-947f-c148469d37bb"; //dedicated ApplicationId for PtzSdk.Win.Samples.Console
                sdk.GatewayKey = Properties.Settings.Default.GatewayKey;
                sdk.PowerTranzId = Properties.Settings.Default.PowerTranzId;
                sdk.PowerTranzPassword = Properties.Settings.Default.PowerTranzPassword;
                sdk.TerminalIdleMessage = "   Gateway Systems";
                sdk.TerminalAddress = "Miura 676";


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

                sdk.ConnectBluetooth();  //Connect the terminal

                sdk.Terminal.DidConnectTerminal += () =>
                {
                    //We are connected
                    sdk.ClearScreen();
                    System.Console.Write("\r\nTerminal Connected.  Press any key to start a transaction...");
                    System.Console.ReadKey();
                    sdk.StartTransaction(request);  //Send the transaction
                };

                sdk.Terminal.DidFinishTransactionWithResponse += (PtzPaymentResponse r) =>
                {
                    //Transaction is complete. Note that the other error events should also be checked as this event may not fire. 

                    //Need another thread here so we don't block the thread on which the terminal screen is updated
                    //This is not necessary - just demonstrates clearing the screen after a transaction
                    var tsk = Task.Run(async () =>
                    {
                        await Task.Delay(1000); //just to allow logging to finish before prompting
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


