using Newtonsoft.Json;
using PowerTranzSDK;
using PowerTranzSDK.CardTerminals.Miura;
using PowerTranzSDK.Miura.Commands.Miura;
using PowerTranzSDK.Miura.Utilities;
using PowerTranzSDK.Requests;
using PowerTranzSDK.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PtzSdk.Win.Samples.Library
{
    public class PtzSdkLibrary
    {

        public PTZMiuraTerminal Terminal;
        private PtzApi PtzApi;

        public string ApplicationId { get; set; }
        public string GatewayKey { get; set; }
        public string PowerTranzId { get; set; }
        public string PowerTranzPassword { get; set; }

        private bool terminalConnected { get; set; } = false;

        public bool IsContactless { get; set; } = false;

        public string CurrentTransactionId { get; set; }    //Keeps track of the current transactionId for TransMod.
        public string TerminalIdleMessage { get; set; } = "";
        public bool IsActiveTransaction { get; set; } = false;
        public string TerminalAddress { get; set; }

        public PtzSdkLibrary()
        {
        }

        #region "Connection"


        public void Connect()
        {
            try
            {
                //Note that the url is specified in the PowerTranzSDK.dll.config file in this example
                PtzApi = new PtzApi(ApplicationId, GatewayKey, PowerTranzId, PowerTranzPassword, null, 20);

                Terminal = new PTZMiuraTerminal(PtzApi);

                RegisterListeners(Terminal);

                if (TerminalAddress.StartsWith("COM"))
                {
                    CommonUtility.LogInfo("Connecting via USB");
                    var tsk = Terminal.ConnectTerminalWithInputTypeAsync(CardTerminalInputType.CardTerminalTypeUsb, TerminalAddress);
                }
                else
                {
                    CommonUtility.LogInfo("Connecting via BlueTooth");
                    var tsk = Terminal.ConnectTerminalWithInputTypeAsync(CardTerminalInputType.CardTerminalTypeBluetooth, TerminalAddress);
                }



            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"Connection exception \n\n{x.ToString()}");
            }

        }


        public void Disconnect()
        {
            Terminal.DisconnectTerminal();
        }

        #endregion

        public void StartTransaction(PtzPaymentRequest request)
        {
            try
            {
                if (terminalConnected)
                {
                    if (IsContactless)
                    {
                        Task.Run(() => Terminal.BeginNFCTransactionWithRequest(request)); //Launch the transaction on another thread
                    }
                    else
                    {
                        Task.Run(() => Terminal.BeginEMVTransactionWithRequest(request));
                    }

                    CommonUtility.LogInfo("Transaction in progress");
                }
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"StartTransaction exception \r\n{x.ToString()}");
            }
        }

        public void CancelTransaction()
        {
            if (terminalConnected) Terminal.CancelTransaction();
        }



        public void SoftReset()
        {
            if (terminalConnected) Terminal.ResetDevice();
        }


        public void ClearScreen()
        {
            if (terminalConnected) Terminal.DisplayText(TerminalIdleMessage);
        }

        public async Task<List<PtzTransactionResponse>> SearchTransactions(DateTime startDate, DateTime endDate, bool approved)
        {

            //Note that the url is specified in the PowerTranzSDK.dll.config file in this example
            var ptzapi = new PtzApi(ApplicationId, GatewayKey, PowerTranzId, PowerTranzPassword, null, 20);

            var req = new PtzTransactionRequest();
            req.StartDateTime = startDate;
            if (endDate != null) req.EndDateTime = endDate;

            req.Approved = approved;

            var trxns = await ptzapi.TransactionSearchAsync(req);
            return trxns.Transactions.ToList();
        }


        #region EventHandlers

        private void RegisterListeners(PTZMiuraTerminal terminal)
        {
            terminal.DidConnectTerminal += DidConnectTerminalHandler;
            terminal.DidDisconnectTerminal += DidDisconnectTerminalHandler;
            terminal.DidFailToConnectTerminal += Terminal_DidFailToConnectTerminal; ;

            terminal.DidFail += DidFail;
            terminal.DidFinish += DidFinish;
            terminal.DidFailWithReversal += DidFailWithReversal;
            terminal.DidFinishWithReversal += DidFinishWithReversal;

            terminal.OnBatteryLow += OnBatteryLowHandler;
            terminal.OnBatteryPercentage += OnBatteryPercentage;


            terminal.DidRequestDevicePromptText += DidRequestDevicePromptTextHandler;
            terminal.DidReceiveCardEvent += DidReceiveCardEventHandler;
            terminal.TerminalDidReceiveKeypadInput += TerminalDidReceiveKeypadInputHandler;
            terminal.PromptCanceled += TerminalOnPromptCanceled;

            terminal.SdkStateChanged += SdkStateChanged;

        }

        private void SdkStateChanged(string state)
        {
            CommonUtility.LogInfo($"New Internal SDK state: {state}\r\n");
        }

        private void OnBatteryPercentage(int batteryPercentage)
        {
            CommonUtility.LogInfo($"Battery level: {batteryPercentage.ToString()}%\r\n");
        }

        private void DidFinish(PtzPaymentResponse response)
        {
            try
            {
                IsActiveTransaction = false;

                var responseString = "TRANSACTION COMPLETE\r\n\r\n" + JsonConvert.SerializeObject(response, Formatting.Indented);
                responseString += $"\r\n TerminalResult: {response?.TerminalResult.ToString()}";

                CommonUtility.LogInfo(responseString);
                CurrentTransactionId = response.TransactionIdentifier;

            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }

        }


        private void DidFinishWithReversal(PtzPaymentResponse response, ReversalReason reason, bool reversalApproved)
        {
            try
            {
                var responseString = "TRANSACTION COMPLETE\r\n\r\n" + JsonConvert.SerializeObject(response, Formatting.Indented);
                responseString += $"\r\n Reversal reason {reason}";
                responseString += $"\r\n Reversal approved: {reversalApproved}";
                responseString += $"\r\n TerminalResult: {response?.TerminalResult.ToString()}";

                CommonUtility.LogInfo(responseString);
                CurrentTransactionId = response.TransactionIdentifier;
                IsActiveTransaction = false;
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }

        }

        private void DidFailWithReversal(PtzPaymentResponse response, ReversalReason reason, bool reversalApproved, Exception exception)
        {
            try
            {
                var responseString = "TRANSACTION FAILED\r\n\r\n" + JsonConvert.SerializeObject(response, Formatting.Indented);
                responseString += $"\r\n Reversal reason {reason}";
                responseString += $"\r\n Reversal approved: {reversalApproved}";
                responseString += $"\r\n TerminalResult: {response?.TerminalResult.ToString()}";
                responseString += $"\r\n Exception: {exception?.Message}";

                CommonUtility.LogInfo(responseString);
                CurrentTransactionId = response.TransactionIdentifier;

                IsActiveTransaction = false;
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }
        }


        private void DidFail(PtzPaymentResponse response, Exception exception)
        {
            try
            {
                var responseString = "TRANSACTION FAILED\r\n\r\n" + JsonConvert.SerializeObject(response, Formatting.Indented);
                responseString += $"\r\n TerminalResult: {response?.TerminalResult.ToString()}";
                responseString += $"\r\n Exception: {exception?.Message}";

                CommonUtility.LogInfo(responseString);

                Terminal.DisplayText(TerminalIdleMessage);
                IsActiveTransaction = false;
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }

        }

        private void Terminal_DidFailToConnectTerminal(Exception error)
        {
            CommonUtility.LogInfo($"Terminal_DidFailToConnectTerminal- {error.ToString()}\r\n");
            terminalConnected = false;
        }


        private void TerminalOnPromptCanceled()
        {
            CommonUtility.LogInfo($"User canceled transaction\r\n");
            IsActiveTransaction = false;
            Terminal.DisplayText(TerminalIdleMessage);
        }


        private void OnReturnEncryptionStatus(CommandEnums.EncryptionStatus encryptionStatus)
        {
            CommonUtility.LogInfo($"OnReturnEncryptionStatus- {encryptionStatus.ToString()}\r\n");
        }

        private void DidConnectTerminalHandler()
        {
            CommonUtility.LogInfo($"CONNECTED");
            terminalConnected = true;
            Terminal.DisplayText(TerminalIdleMessage);

        }

        private void DidDisconnectTerminalHandler()
        {
            CommonUtility.LogInfo($"DISCONNECTED");
            terminalConnected = false;
            IsActiveTransaction = false;
        }


        private void WillConnectTerminalHandler()
        {
            CommonUtility.LogInfo($"Connecting...");
        }


        
        
        
        private void OnBatteryLowHandler(CardTerminalBatteryStatus batteryStatus)
        {

            switch (batteryStatus)
            {
                case CardTerminalBatteryStatus.CardTerminalBatteryStatusLow:
                    CommonUtility.LogInfo($"BATTERY LOW");
                    break;
                case CardTerminalBatteryStatus.CardTerminalBatteryStatusCriticallyLow:
                    CommonUtility.LogInfo($"BATTERY CRITICALLY LOW");
                    break;
            }

        }

        
        private void DidRequestDevicePromptTextHandler(CommandEnums.DevicePrompt prompt)
        { }

        private void DidReceiveCardEventHandler(CardEvent eventType)
        {
            CommonUtility.LogInfo($"DidReceiveCardEventHandler {eventType.ToString()}");
        }

        private void SelectDeviceHandler(List<string> devices)
        { }

        private void TerminalDidReceiveKeypadInputHandler()
        { }

        #endregion

    }
}
