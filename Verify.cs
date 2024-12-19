using System.Collections.Generic;
using CyberArk.Extensions.Plugins.Models;
using CyberArk.Extensions.Utilties.Logger;
using CyberArk.Extensions.Utilties.Reader;
using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Text;

// Change the Template name space
namespace CyberArk.Extensions.Plugin.RealPowerShell
{
    public class Verify : BaseAction
    {
       

        #region constructor
        /// <summary>
        /// Logon Ctor. Do not change anything unless you would like to initialize local class members
        /// The Ctor passes the logger module and the plug-in account's parameters to base.
        /// Do not change Ctor's definition not create another.
        /// <param name="accountList"></param>
        /// <param name="logger"></param>
        public Verify(List<IAccount> accountList, ILogger logger)
            : base(accountList, logger)
        {
        }
        #endregion

        #region Setter
        /// <summary>
        /// Defines the Action name that the class is implementing - Verify
        /// </summary>
        override public CPMAction ActionName
        {
            get { return CPMAction.verifypass; }
        }
        #endregion

        /// <summary>
        /// Plug-in Starting point function.
        /// </summary>
        /// <param name="platformOutput"></param>
        override public int run(ref PlatformOutput platformOutput)
        {


            // CyberArk code to start custom logging and set default Return Code to a value that will show a bad plugin configuration if unchanged
            Logger.MethodStart();
            int RC = 9999;


            try
            {
                // Run our shared base action code. It's the same code no matter the CPM action being taken as we abstract
                // all the "logic" to PowerShell and not here in C#
                log.WriteLine("verify", "customCode", "Attempting new function", LogLevel.INFO);
                RC = UniversalPowershellPlugin("verify", platformOutput);

            }
            catch (Exception ex)
            {
                RC = HandleGeneralError(ex, ref platformOutput);
            }
            finally
            {
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
