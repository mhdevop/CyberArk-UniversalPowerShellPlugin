<#
.SYNOPSIS
    Abstracts and lowers the coding threshold to interact with CyberArk integrations to the PowerShell language
.DESCRIPTION
    Abstracts the deep C# coding experience and voids the need for Prompts/Process files and boils the entire experience down to this PowerShell script.
    The "magic" of this solution is that it transparently passes variables within the runspace that the C# plugin creates and NOT over the command line. Maybe 
    you've noticed in your Windows Event viewer but PowerShell logs arguments passed so it's crazy to feed in a password as it shows up in plaintext. Here,
    my plugin transparently attaches all the normal CyberArk variables you could need. In other words, work 99% in PowerShell, not in C# or Prompt/Process files.
.EXAMPLE
    To "test" this plugin enter the "CPMBin" folder and run the following line in COMMAND (not PowerShell): (reference: https://docs.cyberark.com/pam-self-hosted/latest/en/content/pasimp/plug-in-netinvoker_test.htm?tocpath=Developer%7CCreate%20extensions%7CCreate%20CPM%20plugins%7CCredentials%20Management%20.NET%20SDK%7C_____5)
    CANetPluginInvoker.exe ..\user.ini changepass CyberArk.Extensions.Plugin.RealPowerShell.dll true
.NOTES
    Author: https://github.com/mhdevop/CyberArk-UniversalPowerShellPlugin
#>

#intro
Write-Output "Welcome to RealPowerShell Skeleton code! This plugin will show you very simple 'logic' examples and explain the variables you have access to"

# prepare the "success" variable that will be read by our C# plugin code and reported back to CPM. By default we set it to failure and only set it to the magic string
# if we are certain that it succeeded
$global:RealPowerShellPassOrFail = "false" # in YOUR logic you need to set to =  "PowerShell Success"

# the CyberArk "properties" (safe, username, etc.) are all stored in a single PowerShell Hashtable for a consolidated experience.
# in this example, we get the value of "debug" and if it's true we print all the variables that RealPowerShell has passed to this script
if ($CARKTargetExtraHashtable["debug"]){
    Write-Output (Get-Variable * | where {$_.name -like "CARK*"} | Out-String )
    $object = [PSCustomObject]$CARKTargetHashtable
    $object | Export-Csv -Path "CARKTargetHashtable.csv" -NoTypeInformation -Force
    $object = [PSCustomObject]$CARKTargetExtraHashtable
    $object | Export-Csv -Path "CARKTargetExtraHashtable.csv" -NoTypeInformation -Force
}

<#  SAMPLE values that are available during runtime. "hashtable" contain account properties and "cred" objects contain the actual passwords

Name                           Value                                                                                    
----                           ----- 
CARKLogonHashtable             {username, PolicyID, foldername, objectname...}                                          
CARKLogonPSCredObject          System.Management.Automation.PSCredential                                                
CARKReconHashtable             {username, PolicyID, foldername, objectname...}                                          
CARKReconPSCredObject          System.Management.Automation.PSCredential                                                
CARKTargetExtraHashtable       {PlatformParameter, port, debug}                                                         
CARKTargetHashtable            {objectname, username, debug, foldername...}                                             
CARKTargetPSCredObjectCurrent  System.Management.Automation.PSCredential                                                
CARKTargetPSCredObjectNew      System.Management.Automation.PSCredential  

#>

# EXAMPLE 1 - the "username" is the value we want to get out of each hashtable based on type of account: target/logon/reconcile
Write-Output "Target Username = $($CARKTargetHashtable["username"])"
Write-Output "Logon Username = $($CARKLogonHashtable["username"])"
Write-Output "Reconcile Username = $($CARKReconHashtable["username"])"

# EXAMPLE 2 - the "username" is the value we want to get out of each hashtable based on type of account: target/logon/reconcile
Write-Output "Target Username DEBUG = $($CARKTargetHashtable["debug"])"
Write-Output "Logon Username DEBUG = $($CARKLogonHashtable["debug"])"
Write-Output "Reconcile Username DEBUG = $($CARKReconHashtable["debug"])"

# EXAMPLE 3 - pass the encrypted and packaged user/password directly to a command as a "Credential" (commented out so you don't hit AD with fake/testing accounts)
# Get-ADUser AcountNameHere -Credential $CARKTargetPSCredObjectCurrent

# EXAMPLE 4 - Don't use these example unless it's a last resort! If you need the "plaintext" password you can get it but you should strive for example #3 above as it plays
#               nicely with so many PowerShell commandlets by default
Write-Output "Target Username PW = First character of password $($CARKTargetPSCredObjectCurrent.getnetworkcredential().password[0])******"
Write-Output "Logon Username  PW = First character of password $($CARKLogonPSCredObject.getnetworkcredential().password[0])******"
Write-Output "Reconcile Username  PW = First character of password $($CARKReconPSCredObject.getnetworkcredential().password[0])******"


function Invoke-Verify{

    Write-Output "Running Verify Now (really we're just testing if we can login)"

    # CHANGEME - add all your logic here. Otherwise just some fake testing logic here
    Write-Output "Logging in to XYZ application using the current Target Credentials with account $($CARKTargetPSCredObjectCurrent.username)"
    Write-Output "...Here's some REST API code that YOU supply to take in the credentials..."
    Write-Output "...More of YOUR code that checks if logging in worked our not..."
    Write-Output "...Let's assume your logic says the credentials worked..."

    return $true
}


function Invoke-ChangePass{

    Write-Output "Running Changepass Now"

    # CHANGEME - add all your logic here. Otherwise just some fake testing logic here
    Write-Output "Logging in to XYZ application using the current Target Credentials with account $($CARKTargetPSCredObjectCurrent.username)"
    Write-Output "...Here's some REST API code that YOU supply to take in the credentials..."

    Write-Output "Passing in the Current Credentials to the application:"
    $CARKTargetPSCredObjectCurrent

    Write-Output "Now the app is asking for the new password to set so we better use the one CPM is telling us to use or else we'll be mismatched if we try and generate our own"
    $CARKTargetPSCredObjectNew

    Write-Output "...More of YOUR code that does the password change..."
    Write-Output "...Let's assume your logic says the credentials worked..."

    return $true
}


function Invoke-ReconcilePass{

    Write-Output "Running Reconcile Now"

    # CHANGEME - add all your logic here. Otherwise just some fake testing logic here
    Write-Output "Logging in to XYZ application using the current Reconcile Credentials with account $($CARKReconPSCredObject.username)"
    Write-Output "...Here's some REST API code that YOU supply to take in the credentials..."

    Write-Output "Passing in the Current RECONCILE Credentials to the application:"
    $CARKReconPSCredObject

    Write-Output "Now the app is asking for the new password to the TARGET account to set"
    $CARKTargetPSCredObjectNew

    Write-Output "...More of YOUR code that does the password RECONCILE change..."
    Write-Output "...Let's assume your logic says the credentials worked..."

    return $true
}





##### MAIN Logic ##############################

# CPM will only do one "operation" at time: changepass, verifypass, preconcile, reconcile. If you remember in the C# code, we "ignore" logic for login/prereconile/verify
# so that all logic is handled in this script. In other words, this is the part CyberArk won't be thrilled at in that we're abstracting out their SDK and boiling it down
# which causes an intential "mismatch" between the phase the logic actually occurs in. If this is confusing then just remember this plugin comes with no support.
Write-Output "CPM Operation Attempt = $CAOperation"
if ($CAOperation -eq "change")
{

    Write-Output "Starting CUSTOM verify function you coded"
    # to change a password we should verify our current passwords works to login first
    if (Invoke-Verify)
    {

        Write-Output "Starting CUSTOM changepass function you coded since we passed the verify (logon) portion so we know the password works to change"
        # now do the password change
        if (Invoke-ChangePass)
        {
            Write-Output "Congrats, you logged in + changed the password so we know this dummy plugin worked!"
            # if we changed the password then we have success! Set the variable for it to be written out at the end of this script
            $global:RealPowerShellPassOrFail = "PowerShell Success"
        }
        else{
            Write-Error "Failed to change the password"
        }
    }
}
elseif ($CAOperation -eq "verify")
{
    Write-Output "Starting CUSTOM verify function you coded"
    # to verify a password all we do is logon essentially
    if (Invoke-Verify)
    {
        Write-Output "Congrats, all you wanted to do this was time was verify (logon) and your code confirmed it"
        # if we changed the password then we have success! Set the variable for it to be written out at the end of this script
        $global:RealPowerShellPassOrFail = "PowerShell Success"
    }
    else{
        Write-Error "Failed to verify the passwords"
    }
}
elseif ($CAOperation -eq "reconcile")
{

    Write-Output "Starting CUSTOM reconcile function you coded"
    # now do the password change
    if (Invoke-ReconcilePass)
    {
        Write-Output "Congrats your custom code that uses reconcile credentials to set the target credentials worked somehow"
        # if we reconciled the password then we have success! Set the variable for it to be written out at the end of this script
        $global:RealPowerShellPassOrFail = "PowerShell Success"
    }
    else{
        Write-Error "Failed to reconcile the password"
    }
}

# for the grand finale, write the pass/fail results of the entire plugin so that CPM can read it. As a reminder, anything but the magic phrase "PowerShell Success"
# means that the plugin failed
write-output $global:RealPowerShellPassOrFail