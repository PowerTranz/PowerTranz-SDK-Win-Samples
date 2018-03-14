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

                Terminal = new PTZMiuraTerminal(PtzApi, 20);

                RegisterListeners(Terminal);

                if (TerminalAddress.StartsWith("COM"))
                {
                    CommonUtility.LogInfo("Connecting via USB");
                    var tsk = Terminal.ConnectTerminalWithInputTypeAsync(Enumerations.CardTerminalInputType.CardTerminalTypeUsb, TerminalAddress);
                }
                else
                {
                    CommonUtility.LogInfo("Connecting via BlueTooth");
                    var tsk = Terminal.ConnectTerminalWithInputTypeAsync(Enumerations.CardTerminalInputType.CardTerminalTypeBluetooth, TerminalAddress);
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
            terminal.WillConnectTerminal += WillConnectTerminalHandler;
            terminal.DidFailWithError += Terminal_DidFailWithError;
            terminal.DidFinishTransactionWithResponse += DidFinishTransactionWithResponseHandler;
            terminal.TerminalPaymentFailed += TerminalPaymentFailedHandler;
            terminal.DidRequestAccountTypeSelection += DidRequestAccountTypeSelectionHandler;
            terminal.DidReturnAmountConfirmation += DidReturnAmountConfirmationHandler;
            terminal.OnBatteryLow += OnBatteryLowHandler;
            terminal.TerminalDidRequestTransactionAmount += TerminalDidRequestTransactionAmountHandler;
            terminal.DidRequestSelectEMVApplication += DidRequestSelectEMVApplicationHandler;
            terminal.TerminalDidRequestFinalConfirmation += TerminalDidRequestFinalConfirmationHandler;
            terminal.DidRequestDevicePromptText += DidRequestDevicePromptTextHandler;
            terminal.DidReceiveCardEvent += DidReceiveCardEventHandler;
            terminal.SelectDevice += SelectDeviceHandler;
            terminal.TerminalDidReceiveKeypadInput += TerminalDidReceiveKeypadInputHandler;

            terminal.PromptCanceled += TerminalOnPromptCanceled;
            terminal.DidFailWithReversal += Terminal_DidFailWithReversal;
        }

        private void Terminal_DidFailWithReversal(PtzPaymentResponse response, Enumerations.ReversalReason reason)
        {
            var responseString = "TRANSACTION FAILED\r\n\r\n" + JsonConvert.SerializeObject(response, Formatting.Indented);
            responseString = responseString + $"\r\n Reversal reason {reason}";

            CommonUtility.LogInfo(responseString);
            CurrentTransactionId = response.TransactionIdentifier;

        }

        private void Terminal_DidFailToConnectTerminal(Exception error)
        {
            CommonUtility.LogInfo($"Terminal_DidFailToConnectTerminal- {error.ToString()}\r\n");
            terminalConnected = false;
        }

        private void Terminal_DidFailWithError(Exception error)
        {
            try
            {
                CommonUtility.LogInfo($"Terminal_DidFailWithError - {error.ToString()}\r\n");
                Terminal.DisplayText(TerminalIdleMessage);
                IsActiveTransaction = false;
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }

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


        private void DidFinishTransactionWithResponseHandler(PtzPaymentResponse response)
        {
            try
            {
                IsActiveTransaction = false;

                var responseString = JsonConvert.SerializeObject(response, Formatting.Indented);

                CommonUtility.LogInfo($"Transaction complete\r\n{responseString}");

                CurrentTransactionId = response.TransactionIdentifier;

            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }

        }

        private void TerminalPaymentFailedHandler(PtzPaymentRequest request, string error)
        {
            try
            {
                CommonUtility.LogInfo($"Transaction Failed: reason - {error}\r\n");
                Terminal.DisplayText("Declined");
                IsActiveTransaction = false;
            }
            catch (Exception x)
            {
                CommonUtility.LogInfo($"{x.ToString()}\r\n");
            }
        }

        private void DidRequestAccountTypeSelectionHandler(int[] accountType)
        {
            CommonUtility.LogInfo($"DidRequestAccountTypeSelectionHandler {accountType.ToString()}");
        }

        private void DidReturnAmountConfirmationHandler(bool confirmed)
        {
            CommonUtility.LogInfo($"DidRequestAccountTypeSelectionHandler {confirmed.ToString()}");
        }

        private void OnBatteryLowHandler(Enumerations.CardTerminalBatteryStatus batteryStatus)
        {

            switch (batteryStatus)
            {
                case Enumerations.CardTerminalBatteryStatus.CardTerminalBatteryStatusLow:
                    CommonUtility.LogInfo($"BATTERY LOW");
                    break;
                case Enumerations.CardTerminalBatteryStatus.CardTerminalBatteryStatusCriticallyLow:
                    CommonUtility.LogInfo($"BATTERY CRITICALLY LOW");
                    break;
            }

        }

        private void TerminalDidRequestTransactionAmountHandler()
        { }

        private void DidRequestSelectEMVApplicationHandler(List<string> applications)
        { }

        private void TerminalDidRequestFinalConfirmationHandler()
        { }

        private void DidRequestDevicePromptTextHandler(CommandEnums.DevicePrompt prompt)
        { }

        private void DidReceiveCardEventHandler(Enumerations.CardEvent eventType)
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
