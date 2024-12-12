# CyberArk Real PowerShell
## _A plugin to make creating plugins easier_

CARP (CyberArk Real PowerShell) is a prepopulated template of the   [CyberArk .NET SDK](https://docs.cyberark.com/pam-self-hosted/latest/en/content/pasimp/plug-in-netinvoker.htm?tocpath=Developer%7CCreate%20extensions%7CCreate%20CPM%20plugins%7CCredentials%20Management%20.NET%20SDK%7C_____0) that abstracts "hard" code like C# and Prompts/Process files down to simple PowerShell code! In other words, **you can write 99% (or more) of your code in PowerShell without ever needing to touch or know those other painful languages.** Here are the highlighted features:

- Logic and error handling to intelligently decide the context the plugin itself is being used in (eg [Target](https://docs.cyberark.com/pam-self-hosted/latest/en/content/landing%20pages/lpbuiltinplugins.htm) vs. [Usage "Service"](https://docs.cyberark.com/pam-self-hosted/latest/en/content/landing%20pages/lpserviceplugins.htm)). This means you don't have to decide what kind of plugin you need, you can use the **same** plugin on both! (If you know anything about plugin development which I assume you do being here then you know the value of this)
- Limited to no C# coding needed (unless you have advanced needs to expand on this code). Unless you know c# well, most people want to pay a vendor to write this level of code for them when in reality, a simple PowerShell script would do just fine
- Sample PowerShell script to show just how easy it is to use this plugin for nearly all your needs
- The best part - **no** Prompts/Process ini files to deal with (sorry if you love those but I find them painful to explain and edit)
- ✨Magic ✨


## Disclaimers (who doesn't like a list of those)

These days you can't be too careful so "for the record" note the following:

- This code has ++**not**++ been endorsed, supported, reviewed, or encouraged by CyberArk or their affiliates
- While I have tested this code myself to the nth degree, YOU are solely responsible for it being used for your purposes. I take no repsonsibility for any and all issues caused by its usage.
- I cannot and will not provide "support" for this plugin. Anyone in the CyberArk business knows that consultants at minimum make ~$250+ per hour so as much as I'd love to support it, it's unfair, let alone unrealistic for me to do so
- I built this code during my own personal time (not work hours), using my own personal PC (not work), and from my own brain 
- Regarding the code:
  - My code does not follow some best practices to which I admit directly and openly. My objective was not to win first prize on aeasthetics.
  - My primary object was to make code that "works" and is "easier to unerstand" rather than "efficiency" or "minimal resources". (eg there is plenty of redundency and room for fancy things like abstract classes, sharing methods, etc. but I chose not to use those to try and keep the code linear and easier to understand for the masses)
  - If you don't like it, think it's trash, hate it, or etc. by all means I encourage YOU to change it on your end. I can't and never will think of everything under the sun
