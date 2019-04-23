# Panacea

## Intro
The purpose of Panacea v2.0 was to have a modular application where plugins could be loaded/unloaded on demand or depending on configuration/assignmenets.

Panacea v2.0 failed miserably in few points:
* It doesn't provide clean development guidelines and APIs to make plugin integration easy and straight-forward.
* Not everything is a plugin. Since there are no clean ways for plugins to work with each other, what is called `Core` ended up having more functionality than all the plugins combined (Theming, Media etc). This created a bloated Panacea Core package which requires a large amount of disk space even if some features are not in use by the installation where Panacea runs. Moreover, changing a single line of code (which happens a lot when so much functionality exists in one package) requires the entire package to be updated, transferring and storing big files and polluting our servers.
* Core/Plugins compatibility is a mess as Core/Plugins are analyzed on IL level to list public Types and Method signatures provided/called and then compared on the server side to match compatible modules. This is done only for 1 assembly (`PanaceaLib`) causing dependency issues with other common assemblies (Newtonsoft, ServiceStack.Json etc).

The changes in Panacea v2.1 are:
* The `Core` will be responsible for managing plugins **only** and add  (load/unload, manage errors etc). The `Core` package will be updated only when a new feature related to plugins is added (never happened so far) or if a bug related to this small functionality has been solved.
* All plugins that export functionality or plugins that expect functionality from other plugins will declare their APIs under `Panacea.Modularity` namespace in their own Nuget package.

Example: `Panacea.Modularity.RfidReader`
 This package contains definitions about what a RFID Reader Plugin can do (eg expose an event `CardConnected`). Any plugin that adds support for RFID readers can implement the IPlugin type defined in this assembly. Any plugins that want to query Panacea and get all the plugins that expose RFID functionality must also use this package. (The compatibility between plugins that use the same package is done on the version of the package).
