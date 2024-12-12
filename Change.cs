using System.Collections.Generic;
using CyberArk.Extensions.Plugins.Models;
using CyberArk.Extensions.Utilties.Logger;
using CyberArk.Extensions.Utilties.Reader;
using System;
using System.IO;
using System.Security;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections;


// Change the Template name space
namespace CyberArk.Extensions.Plugin.RealPowerShell
{
    public class Change : BaseAction
    {
        #region Consts

        public static readonly string USERNAME = "username";
        public static readonly string PORT = "port";
        public static readonly string debug = "debug";

        #endregion

        #region constructor
        /// <summary>
        /// Logon Ctor. Do not change anything unless you would like to initialize local class members
        /// The Ctor passes the logger module and the plug-in account's parameters to base.
        /// Do not change Ctor's definition not create another.
        /// <param name="accountList"></param>
        /// <param name="logger"></param>
        public Change(List<IAccount> accountList, ILogger logger)
            : base(accountList, logger)
        {
        }
        #endregion

        #region Setter
        /// <summary>
        /// Defines the Action name that the class is implementing - Change
        /// </summary>
        override public CPMAction ActionName
        {
            get { return CPMAction.changepass; }
        }
        #endregion

        /// <summary>
        /// Plug-in Starting point function.
        /// </summary>
        /// <param name="platformOutput"></param>
        override public int run(ref PlatformOutput platformOutput)
        {


            #region Init

            // CyberArk code to start custom logging and set default Return Code to a value that will show a bad plugin configuration if unchanged
            Logger.MethodStart();
            int RC = 9999;

            Boolean isUsage = false;
            Boolean hasTarget = false;
            Boolean hasLogon = false;
            Boolean hasReconcile = false;
            Boolean isDebugEnabledInPlatform = false;

            // create pointer variables we will fill in later to ensure the scope of them reaches all areas of code
            PSCredential MasterPSCredObject; //= new PSCredential("MasterPSCredObject", StringExtension.convertToSecureString("PLACEHOLDER"));
            PSCredential TargetPSCredObjectCurrent;
            PSCredential TargetPSCredObjectNew;
            PSCredential LogonPSCredObject;
            PSCredential ReconPSCredObject;

            Hashtable CARKMasterHashtable = new Hashtable(); ;
            Hashtable CARKTargetHashtable = new Hashtable(); ;
            Hashtable CARKLogonHashtable = new Hashtable(); ;
            Hashtable CARKReconHashtable = new Hashtable();


            // create a runspace to populate later with PowerShell
            Runspace runspaceObject = RunspaceFactory.CreateRunspace();

            // create a "bucket" to collect the output from PowerShell script which we will fill in later
            Collection<PSObject> PSresultsObject = new Collection<PSObject>();

            // create the PowerShell container object
            PowerShell powershellObject = PowerShell.Create();

            // we have our runspace object so now we call the method to open it
            runspaceObject.Open();

            // attach the PowerShell object to the runspace we created
            powershellObject.Runspace = runspaceObject;


            System.IO.StreamReader StreamReaderObject = new System.IO.StreamReader("tester.ps1");

            try
            {
                string debugText = ParametersAPI.GetOptionalParameter(debug, TargetAccount.AccountProp, TargetAccount.ExtraInfoProp);

                if (debugText.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    isDebugEnabledInPlatform = true;
                    CARKTargetHashtable.Add("debug", true);
                }

            }
            catch
            {
                log.WriteLine("change", "customCode", "Unable to detect DEBUG setting in the platform settings - assume none", LogLevel.WARNING);
            }




            try
            {
                log.WriteLine("change", "customCode", "Attempting to read the PowerShell script", LogLevel.INFO);


                powershellObject.AddScript(StreamReaderObject.ReadToEnd());
            }
            catch
            {
                log.WriteLine("change", "customCode", "Unable to read script file", LogLevel.ERROR);
            }


            #endregion


            #region ValidatePopulatedAccounts

            try
            {

                log.WriteLine("change", "customCode", "Checking if the Master account is populated to determine if this plugin is a Usage", LogLevel.INFO);

                //Hashtable psMasterAccountHashtable = new Hashtable(); 
                foreach (var entry in MasterAccount.AccountProp)
                {
                    CARKMasterHashtable.Add(entry.Key, entry.Value);
                }

                MasterPSCredObject = new PSCredential("MasterPSCredObject", MasterAccount.CurrentPassword);

                powershellObject.Runspace.SessionStateProxy.SetVariable("CARKMasterAccountHashtable", CARKTargetHashtable);

                isUsage = true;

                log.WriteLine("change", "customCode", "Confirmed plugin type = Usage", LogLevel.INFO);


            }
            catch
            {
                log.WriteLine("change", "customCode", "Master is empty so this plugin is NOT a Usage", LogLevel.WARNING);
            }


            if (!isUsage)
            {

                log.WriteLine("change", "customCode", "Confirmed plugin type = Non-usage", LogLevel.INFO);

                try
                {
                    string KeyPairString = "";
                    foreach (KeyValuePair<string, string> keyValuePair in TargetAccount.AccountProp)
                    {
                        // Console.WriteLine("Key: {0}, Value: {1}", keyValuePair.Key, keyValuePair.Value);
                        if (!keyValuePair.Key.Contains("password"))
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = " + keyValuePair.Value;
                            CARKTargetHashtable.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                        else
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = ******* (ssshhhh it's a secret after all)";
                        }

                    }

                    TargetPSCredObjectCurrent = new PSCredential("TargetPSCredObject", TargetAccount.CurrentPassword);
                    TargetPSCredObjectNew = new PSCredential("TargetPSCredObjectNew", TargetAccount.NewPassword);

                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetPSCredObjectCurrent", TargetPSCredObjectCurrent);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetPSCredObjectNew", TargetPSCredObjectNew);

                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetHashtable", CARKTargetHashtable);

                    hasTarget = true;

                    log.WriteLine("change", "customCode", "Here are the properties on Target Account:\n" + KeyPairString + "\n", LogLevel.INFO);

                }
                catch
                {
                    //Console.WriteLine("Cought target");
                    log.WriteLine("change", "customCode", "Account is not a Usage which means it must have a Target account yet does not.", LogLevel.ERROR);
                }


                try
                {
                    string KeyPairString = "";
                    foreach (KeyValuePair<string, string> keyValuePair in ReconcileAccount.AccountProp)
                    {
                        // Console.WriteLine("Key: {0}, Value: {1}", keyValuePair.Key, keyValuePair.Value);
                        if (!keyValuePair.Key.Contains("password"))
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = " + keyValuePair.Value;
                            CARKReconHashtable.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                        else
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = ******* (ssshhhh it's a secret after all)";
                        }

                    }

                    ReconPSCredObject = new PSCredential("ReconPSCredObject", ReconcileAccount.CurrentPassword);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKReconPSCredObject", ReconPSCredObject);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKReconHashtable", CARKReconHashtable);

                    hasReconcile = true;

                    log.WriteLine("change", "customCode", "Here are the properties on Reconcile Account:\n" + KeyPairString + "\n", LogLevel.INFO);
                }
                catch
                {
                    Console.WriteLine("Cought recon");
                }

                try
                {
                    string KeyPairString = "";
                    foreach (KeyValuePair<string, string> keyValuePair in LogOnAccount.AccountProp)
                    {
                        // Console.WriteLine("Key: {0}, Value: {1}", keyValuePair.Key, keyValuePair.Value);
                        if (!keyValuePair.Key.Contains("password"))
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = " + keyValuePair.Value;
                            CARKLogonHashtable.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                        else
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = ******* (ssshhhh it's a secret after all)";
                        }


                    }

                    LogonPSCredObject = new PSCredential("ReconPSCredObject", LogOnAccount.CurrentPassword);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKLogonPSCredObject", LogonPSCredObject);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKReconHashtable", CARKReconHashtable);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKLogonHashtable", CARKLogonHashtable);

                    hasLogon = true;

                    log.WriteLine("change", "customCode", "Here are the properties on Logon Account:\n" + KeyPairString + "\n", LogLevel.INFO);
                }
                catch
                {
                    log.WriteLine("change", "customCode", "Logon account is empty", LogLevel.WARNING);
                }
            } // if NOT a usage END

            #endregion




            try
            {

                #region Logic
                /////////////// Put your code here ////////////////////////////



                powershellObject.Runspace.SessionStateProxy.SetVariable("CAOperation", "change");


                PSresultsObject = powershellObject.Invoke();

                if (powershellObject.Streams.Error.Count > 0)
                {
                    log.WriteLine("change", "PowerShellError", "PowerShell threw errors below--------------------------\n", LogLevel.ERROR);
                    StringBuilder sb = new StringBuilder();
                    foreach (ErrorRecord er in powershellObject.Streams.Error)
                    {
                        sb.Append(er.ToString() + "       ");
                    }

                    log.WriteLine("change", "PowerShellError", "PowerShell raw message:\n\n" + sb.ToString() + "\n\n End of error \n", LogLevel.ERROR);
                    sb.Clear();

                }
                else
                {
                    log.WriteLine("change", "PowerShellOutput", "PowerShell Output below --------------------------\n", LogLevel.INFO);

                    foreach (PSObject rtnItem in PSresultsObject)
                    {

                        log.WriteLine("change", "PowerShellOutput", "Raw PowerShell Output:\n\n" + rtnItem.ToString() + "\n\n-----------------End of Output", LogLevel.INFO);

                        if (rtnItem.ToString().Contains("PowerShell Success"))
                        {
                            log.WriteLine("change", "PowerShellOutputSuccess", "We do indeed see 'PowerShell Success' in the PowerShell output so assume success!!!", LogLevel.INFO);
                            RC = 0;
                        }



                    }

                }


                if (RC != 0)
                {
                    log.WriteLine("change", "PowerShellOutput", "We did NOT see 'PowerShell Success' in the PowerShell output so assume FAILURE. Debug the PowerShell sript.", LogLevel.ERROR);
                }



                /////////////// END of putting your code above ////////////////////////////
                #endregion Logic

            }
            catch (Exception ex)
            {
                RC = HandleGeneralError(ex, ref platformOutput);
            }
            finally
            {
                // no matter what, always clean up the objects we used to ensure the OS get its resources back
                StreamReaderObject.Close();
                StreamReaderObject.Dispose();
                powershellObject.Dispose();
                runspaceObject.Close();
                runspaceObject.Dispose();
                Logger.MethodEnd();
            }

            // Important:
            // 1.RC must be set to 0 in case of success, or 8000-9000 in case of an error.
            // 2.In case of an error, platformOutput.Message must be set with an informative error message, as it will be displayed to end user in PVWA.
            //   In case of success (RC = 0), platformOutput.Message can be left empty as it will be ignored.
            return RC;

        }

    }
}

