using PowerTranzSDK.DataModel;
using PowerTranzSDK.Requests;
using PtzSdk.Win.Samples.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PtzSdk.Win.Samples.Console
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {


                var sdk = new PtzSdkLibrary();
                sdk.ApplicationId = "9eb64949-2fbc-4ef0-947f-c148469d37bb"; //dedicated ApplicationId for PtzSdk.Win.Samples.Console
                sdk.GatewayKey = Properties.Settings.Default.GatewayKey;
                sdk.PowerTranzId = Properties.Settings.Default.PowerTranzId;
                sdk.PowerTranzPassword = Properties.Settings.Default.PowerTranzPassword;
                sdk.TerminalIdleMessage = "Gateway Systems";
                sdk.TerminalAddress = "Miura 676";

                sdk.ConnectBluetooth();

                var request = new PtzPaymentAuth
                {
                    OrderId = "11111",
                    TotalAmount = 14.51m,
                    TipAmount = 1.56m,
                    TaxAmount = 2.13m,
                    OtherAmount = 1.99m,
                    CurrencyCode = "840",
                    Source = new PtzPaymentSource
                    {
                        CardPresent = true,
                        CardEmvFallback = true,
                        Contactless = false,
                        CardPan = string.Empty,
                        MaskedPan = "476173******0119",
                        CardCvv = string.Empty,
                        CardExpiration = "2212",
                        EncryptedCardTrackData = "cd2920ada07667e69dbda8a23773ec06774e7ce1bc34bed7cf7eee9f1ce412804d754bbae555feb196e38e45d44e93836e230b3ff5161f3f1f7b51503f885f6df35a493faac1d5346653e6b0b80584c3a1765ed94245ac83f975e8419890de6a4c010659056a14799ec54efffa876a088e9fe8386a2bda19cb5760328ebbe041",
                        Ksn = "000002027c6124e00019",
                    },
                    TerminalId = "01249102",
                    TerminalCode = "MiuraM010",
                };

                sdk.Terminal.DidConnectTerminal += () => sdk.StartTransaction(request);

                System.Console.WriteLine("Press any key to exit");
                System.Console.ReadKey();

            }
            catch(Exception x)
            {
                System.Console.Write(x.ToString());
            }

        }

    }
}
