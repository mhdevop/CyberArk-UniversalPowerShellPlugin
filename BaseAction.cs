using System.Collections.Generic;
using CyberArk.Extensions.Plugins.Models;
using CyberArk.Extensions.Utilties.CPMPluginErrorCodeStandarts;
using CyberArk.Extensions.Utilties.Logger;
using CyberArk.Extensions.Utilties.CPMParametersValidation;
using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Text;

// Change the Template name space
namespace CyberArk.Extensions.Plugin.RealPowerShell
{
    /*
     * Base Action class should contain common plug-in functionality and parameters.
     * For specific action functionality and parameters use the action classes.
     */
    abstract public class BaseAction : AbsAction
    {
        #region Properties

        internal ParametersManager ParametersAPI { get; private set; }

        #endregion


        public int UniversalPowershellPlugin(string CPMAction, PlatformOutput platformOutput) {

            int RC = 9999;
            #region Init

           string USERNAME = "username";
           string PORT = "port";
           string debug = "debug";



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
                log.WriteLine(CPMAction, "customCode", "Unable to detect DEBUG setting in the platform settings - assume none", LogLevel.WARNING);
            }




            try
            {
                log.WriteLine(CPMAction, "customCode", "Attempting to read the PowerShell script", LogLevel.INFO);


                powershellObject.AddScript(StreamReaderObject.ReadToEnd());
            }
            catch
            {
                log.WriteLine(CPMAction, "customCode", "Unable to read script file", LogLevel.ERROR);
            }


            #endregion


            #region ValidatePopulatedAccounts

            try
            {

                log.WriteLine(CPMAction, "customCode", "Checking if the Master account is populated to determine if this plugin is a Usage", LogLevel.INFO);

                //Hashtable psMasterAccountHashtable = new Hashtable(); 
                foreach (var entry in MasterAccount.AccountProp)
                {
                    CARKMasterHashtable.Add(entry.Key, entry.Value);
                }

                MasterPSCredObject = new PSCredential("MasterPSCredObject", MasterAccount.CurrentPassword);

                powershellObject.Runspace.SessionStateProxy.SetVariable("CARKMasterAccountHashtable", CARKTargetHashtable);

                isUsage = true;

                log.WriteLine(CPMAction, "customCode", "Confirmed plugin type = Usage", LogLevel.INFO);


            }
            catch
            {
                log.WriteLine(CPMAction, "customCode", "Master is empty so this plugin is NOT a Usage", LogLevel.WARNING);
            }


            if (!isUsage)
            {

                log.WriteLine(CPMAction, "customCode", "Confirmed plugin type = Non-usage", LogLevel.INFO);

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

                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Target Account:\n" + KeyPairString + "\n", LogLevel.INFO);

                }
                catch
                {
                    //Console.WriteLine("Cought target");
                    log.WriteLine(CPMAction, "customCode", "Account is not a Usage which means it must have a Target account yet does not.", LogLevel.ERROR);
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

                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Reconcile Account:\n" + KeyPairString + "\n", LogLevel.INFO);
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

                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Logon Account:\n" + KeyPairString + "\n", LogLevel.INFO);
                }
                catch
                {
                    log.WriteLine(CPMAction, "customCode", "Logon account is empty", LogLevel.WARNING);
                }
            } // if NOT a usage END

            #endregion




            try
            {

                #region Logic
                /////////////// Put your code here ////////////////////////////



                powershellObject.Runspace.SessionStateProxy.SetVariable("CAOperation", CPMAction);


                PSresultsObject = powershellObject.Invoke();

                if (powershellObject.Streams.Error.Count > 0)
                {
                    log.WriteLine(CPMAction, "PowerShellError", "PowerShell threw errors below--------------------------\n", LogLevel.ERROR);
                    StringBuilder sb = new StringBuilder();
                    foreach (ErrorRecord er in powershellObject.Streams.Error)
                    {
                        sb.Append(er.ToString() + "       ");
                    }

                    log.WriteLine(CPMAction, "PowerShellError", "PowerShell raw message:\n\n" + sb.ToString() + "\n\n End of error \n", LogLevel.ERROR);
                    sb.Clear();

                }
                else
                {
                    string noErrorOutPut = "";
                    foreach (PSObject rtnItem in PSresultsObject)
                    {

                        noErrorOutPut += rtnItem.ToString() + "\n";
                        if (rtnItem.ToString().Contains("PowerShell Success"))
                        {
                            log.WriteLine(CPMAction, "PowerShellOutputSuccess", "We do indeed see 'PowerShell Success' in the PowerShell output so assume success!!!", LogLevel.INFO);
                            RC = 0;
                        }

                    }

                    log.WriteLine(CPMAction, "PowerShellOutput", "\n-------------- Raw PowerShell Output Below --------------------------------------------------------------------\n\n" + noErrorOutPut + "\n\n--------------------------------------------------- End of Raw PowerShell Output Aboove  -------------------------------------------------------------", LogLevel.INFO);


                }


                if (RC != 0)
                {
                    log.WriteLine(CPMAction, "PowerShellOutput", "We did NOT see 'PowerShell Success' in the PowerShell output so assume FAILURE. Debug the PowerShell sript.", LogLevel.ERROR);
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




        #region constructor
        /// <summary>
        /// BaseAction Ctor. Do not change anything unless you would like to initialize local class members
        /// The Ctor passes the logger module and the plug-in account's parameters to base.
        /// Do not change Ctor's definition not create another.
        /// <param name="accountList"></param>
        /// <param name="logger"></param>
        public BaseAction(List<IAccount> accountList, ILogger logger)
            : base(accountList, logger)
        {
            // Init ParametersManager
            ParametersAPI = new ParametersManager();
        }
        #endregion

        /// <summary>
        /// Handle the general RC and error message.
        /// <summary>
        /// <param name="ex"></param>
        /// <param name="platformOutput"></param>
        internal int HandleGeneralError(Exception ex, ref PlatformOutput platformOutput)
        {
            ErrorCodeStandards errCodeStandards = new ErrorCodeStandards();
            Logger.WriteLine(string.Format("Received exception: {0}.", ex), LogLevel.ERROR);
            platformOutput.Message = errCodeStandards.ErrorStandardsDict[PluginErrors.STANDARD_DEFUALT_ERROR_CODE_IDX].ErrorMsg;
            return errCodeStandards.ErrorStandardsDict[PluginErrors.STANDARD_DEFUALT_ERROR_CODE_IDX].ErrorRC; 
        }

    }
}
