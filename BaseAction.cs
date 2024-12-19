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
using System.IO;

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

        // function that is shared across all other classes in this plugin. Since the code doesn't change
        // we put the bulk of the custom code in the base action class so it's automatically shared
        // through the other classes as abstract
        public int UniversalPowershellPlugin(string CPMAction, PlatformOutput platformOutput) {

            // set default Return Code to a value that will show errors about the plugin if it's not changed
            int RC = 9999;
            #region Init

            // debug string and powershell script name that we'll use later to get values out of platform/acccount
            string debug = "debug";
            string PowerShellScriptName = "PowerShellScriptName";

            // assume the plugin by default is not a plugin
            Boolean isUsage = false;

            // create pointer variables we will fill in later to ensure the scope of them reaches all areas of code
            PSCredential MasterPSCredObject; 
            PSCredential TargetPSCredObjectCurrent;
            PSCredential TargetPSCredObjectNew;
            PSCredential LogonPSCredObject;
            PSCredential ReconPSCredObject;

            // create empty hashtables to be populated later in the code but put here for scoping purposes
            Hashtable CARKMasterHashtable = new Hashtable(); 
            Hashtable CARKTargetHashtable = new Hashtable();
            Hashtable CARKTargetExtraHashtable = new Hashtable();
            Hashtable CARKLogonHashtable = new Hashtable(); 
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

            // get the PowerShell script name from either the account or platform properties
            string ScriptName = ParametersAPI.GetOptionalParameter(PowerShellScriptName, TargetAccount.AccountProp, TargetAccount.ExtraInfoProp);

            
            // block of code to get the debug status 
            try
            {
                string debugText = ParametersAPI.GetOptionalParameter(debug, TargetAccount.AccountProp, TargetAccount.ExtraInfoProp);

                if (debugText.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
             
                    CARKTargetHashtable.Add("debug", true);
                }

            }
            catch
            {
                log.WriteLine(CPMAction, "customCode", "Unable to detect DEBUG setting in the platform settings - assume none", LogLevel.WARNING);
            }



            //block to validate script name in the BIN folder and then read in the script
            // if it fails we failed the entire plugin since it doesn't make any sense to continue
            try
            {
                log.WriteLine(CPMAction, "customCode", "Attempting to read the PowerShell script = " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScriptName), LogLevel.INFO);
                
                System.IO.StreamReader StreamReaderObject = new System.IO.StreamReader(ScriptName);
                powershellObject.AddScript("Set-ExecutionPolicy Bypass -Scope Process -Force");
                powershellObject.AddScript(StreamReaderObject.ReadToEnd());
           
                StreamReaderObject.Close();
                StreamReaderObject.Dispose();

            }
            catch (Exception ex)
            {
                log.WriteLine(CPMAction, "customCode", "Catch on powershell script read " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScriptName), LogLevel.ERROR);

                RC = HandleGeneralError(ex, ref platformOutput);
                powershellObject.Dispose();
                runspaceObject.Close();
                runspaceObject.Dispose();
                Logger.MethodEnd();
                return RC;
            }


            #endregion


            #region ValidatePopulatedAccounts
            // block to check if the "usage" (service) is being used and put in a try/catch because we still want the 
            // plugin to continue if it fails because that means it's not a usage but a different kind of plugin
            try
            {

                log.WriteLine(CPMAction, "customCode", "Checking if the Master account is populated to determine if this plugin is a Usage", LogLevel.INFO);
                string KeyPairString = "";
                
                //loop through each master account property
                foreach (var entry in MasterAccount.AccountProp)
                {

                    // build a hashtable to pass in to PowerShell
                    if (!entry.Key.Contains("password"))
                    {
                        KeyPairString += "\n" + entry.Key + " = " + entry.Value;
                        CARKMasterHashtable.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        KeyPairString += "\n" + entry.Key + " = ******* (ssshhhh it's a secret after all)";
                    }
                }

                // show all the properties in the logs for debugging
                log.WriteLine(CPMAction, "customCode", "Here are the properties on Master Account:\n" + KeyPairString + "\n", LogLevel.INFO);

                // create the PowerShell credential object 
                // NOTE: in the case of the "master" account only, there is no concept of "old/new" password. Both properties will always
                //          contain the same password. Be definition, a usage only needs the "current" password so your PowerShell
                //          logic needs to be able to handle this
                MasterPSCredObject = new PSCredential("MasterPSCredObject", MasterAccount.CurrentPassword);

                // set the populated hashtable object to the PowerShell object
                powershellObject.Runspace.SessionStateProxy.SetVariable("CARKMasterAccountHashtable", CARKTargetHashtable);

                isUsage = true;

                log.WriteLine(CPMAction, "customCode", "Confirmed plugin type = Usage", LogLevel.INFO);

            }
            catch
            {
                log.WriteLine(CPMAction, "customCode", "Master is empty so this plugin is NOT a Usage", LogLevel.WARNING);
            }

            // if the usage variable is NOT true (double negative) aka it's not a usage but a target platform
            if (!isUsage)
            {

                log.WriteLine(CPMAction, "customCode", "Confirmed plugin type = Non-usage", LogLevel.INFO);


                // try getting a TARGET account properties which in theory will always be 100% populated if it's not a usage
                try
                {

                    // build a hashtable for the normal account properties
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

                    // "extrainfo" is another set of properties that are found in the platform so we collet those to whatever they are
                    string KeyPairString2 = "";
                    foreach (KeyValuePair<string, string> keyValuePair in TargetAccount.ExtraInfoProp)
                    {
                        // Console.WriteLine("Key: {0}, Value: {1}", keyValuePair.Key, keyValuePair.Value);
                        if (!keyValuePair.Key.Contains("password"))
                        {
                            KeyPairString2 += "\n" + keyValuePair.Key + " = " + keyValuePair.Value;
                            CARKTargetExtraHashtable.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                        else
                        {
                            KeyPairString += "\n" + keyValuePair.Key + " = ******* (ssshhhh it's a secret after all)";
                        }

                    }



                    // TARGET type accounts have both a current and a new password which means we need 2 seperate credential objects
                    TargetPSCredObjectCurrent = new PSCredential("TargetPSCredObject", TargetAccount.CurrentPassword);
                    TargetPSCredObjectNew = new PSCredential("TargetPSCredObjectNew", TargetAccount.NewPassword);

                    // attach the credential objects to PowerShell variables
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetPSCredObjectCurrent", TargetPSCredObjectCurrent);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetPSCredObjectNew", TargetPSCredObjectNew);

                    // attach the target and platform properties to the powershell variables
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetHashtable", CARKTargetHashtable);
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKTargetExtraHashtable", CARKTargetExtraHashtable);


                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Target Account:\n" + KeyPairString + "\n", LogLevel.INFO);

                }
                catch
                {
                    log.WriteLine(CPMAction, "customCode", "Account is not a Usage which means it must have a Target account yet does not.", LogLevel.ERROR);
                }

                // attempt to read in the reconcile account which may or may not be populated in PVWA.
                // YOUR PowerShell logic needs to determine if this is used or not
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

           

                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Reconcile Account:\n" + KeyPairString + "\n", LogLevel.INFO);
                }
                catch
                {
                    log.WriteLine(CPMAction, "customCode", "Caught Recon issue", LogLevel.INFO);
                }


                // attempt to read in the properties of the logon account which may or may not be populated in PVWA
                // YOU are responsible for your PowerShell logic checking if this is needed or not
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
                    powershellObject.Runspace.SessionStateProxy.SetVariable("CARKLogonHashtable", CARKLogonHashtable);


                    log.WriteLine(CPMAction, "customCode", "Here are the properties on Logon Account:\n" + KeyPairString + "\n", LogLevel.INFO);
                }
                catch
                {
                    log.WriteLine(CPMAction, "customCode", "Logon account is empty", LogLevel.WARNING);
                }
            } // if NOT a usage END

            #endregion



            // now that we have all our variables in order such as account properties, script to run, and debug we can actually
            // execute the PowerShell script and pass in everything we've collected
            try
            {

                #region Logic

                // set another PowerSHell variable so YOUR logic knows which CPM operation is happening (change, verify, reconcile)
                powershellObject.Runspace.SessionStateProxy.SetVariable("CAOperation", CPMAction);

                // here is where the "magic" really happens. We actually execute the PowerShell script that YOU supplied and in that script YOU
                // need to handle ALL the logic, error checking, etc. All the output from the script is collected by this variable
                PSresultsObject = powershellObject.Invoke();

                // check if the PowerShell execution had an errors in the error stream. 0 is ideal but 1 or more will trigger this "if" section
                if (powershellObject.Streams.Error.Count > 0)
                {

                    // loop through ecah error and build a string containing all the messages from the error for debugging YOUR script
                    log.WriteLine(CPMAction, "PowerShellError", "PowerShell threw errors below--------------------------\n", LogLevel.ERROR);
                    StringBuilder sb = new StringBuilder();
                    foreach (ErrorRecord er in powershellObject.Streams.Error)
                    {
                        sb.Append(er.ToString() + "       ");
                    }

                    log.WriteLine(CPMAction, "PowerShellError", "PowerShell raw message:\n\n" + sb.ToString() + "\n\n End of error \n", LogLevel.ERROR);
                    sb.Clear();

                }
                // else in this case means zero errors which is good :-)
                else
                {

                    // loop through ALL lines of output that YOUR script put out
                    string noErrorOutPut = "";
                    foreach (PSObject rtnItem in PSresultsObject)
                    {

                        // build the string of all the lines that PowerShell put out. This concat string will be written to the Debug log
                        noErrorOutPut += rtnItem.ToString() + "\n";

                        // for each output line that YOUR script puts out, check if any of those lines contain the "magic phrase" of "PowerShell Success"
                        // this is the key phrase this line looks for to know if YOUR script worked. This means YOUR script has to have all the logic,
                        // all the error handling, and everything else needed before it output that phrase for this code to check.
                        // Remember this code is "dumb" in the sense is has no logic specific to YOUR plugin at all. You have to add the smarts.
                        if (rtnItem.ToString().Contains("PowerShell Success"))
                        {

                            // now that the plugin sees the success message we output it in the logs and change the RC variable to 0 which in CyberArk
                            // terms means success. 
                            log.WriteLine(CPMAction, "PowerShellOutputSuccess", "We do indeed see 'PowerShell Success' in the PowerShell output so assume success!!!", LogLevel.INFO);
                            RC = 0;
                        }

                    }

                    // write the raw PowerShell output to the CyberArk logs for troubleshooting later
                    log.WriteLine(CPMAction, "PowerShellOutput", "\n-------------- Raw PowerShell Output Below --------------------------------------------------------------------\n\n" + noErrorOutPut + "\n\n--------------------------------------------------- End of Raw PowerShell Output Aboove  -------------------------------------------------------------", LogLevel.INFO);
                }

                // if RC is not zero then we can assume YOUR script did not output the magic phrase which means it failed so the plugin should fail
                if (RC != 0)
                {
                    log.WriteLine(CPMAction, "PowerShellOutput", "We did NOT see 'PowerShell Success' in the PowerShell output so assume FAILURE. Debug the PowerShell sript.", LogLevel.ERROR);
                }

                #endregion Logic

            }
            catch (Exception ex)
            {

                // if anything triggers a failure catch it and mark the error to be returned to CyberArk
                RC = HandleGeneralError(ex, ref platformOutput);
            }
            finally
            {
                // no matter what, always clean up the objects we used to ensure the OS get its resources back
                
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
