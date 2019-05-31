# Panacea v2.1

## Lessons from v.2.0
The design standard of Panacea v2.0 was to be a modular application where plugins could be loaded/unloaded on demand or depending on configuration/assignments.

There are important lessons learnt from Panacea v2.0 currently implemented design:
* It doesn't provide clean development guidelines and APIs to make plugin integration easy and straight-forward.
* Not everything is a plugin. Since there are no clean ways for plugins to work with each other, what is called `Core` ended up having more functionality than all the plugins combined (Theming, Media, RFID, etc.). This created a bloated Panacea Core package which requires a large amount of disk space even if some features are not in use by the installation where Panacea runs. Moreover, changing a single line of code (which happens a lot when so much functionality exists in one package) requires the entire package to be updated, transferred and stored which causes pollution to our servers.
* Core/Plugins compatibility ended up too complex as Core/Plugins are analyzed on IL level to list public Types and Method signatures provided/called and then compared on the server side to match compatible modules. This is done only for 1 assembly (`PanaceaLib`) causing dependency issues with other common assemblies (Newtonsoft, ServiceStack.Json etc).
* There wasn't a unified theming library to keep Panacea's UI implementation 100% consistent.

## Changes in v2.1
The changes in Panacea v2.1 are:
* Architecture Restructure
  * Use MVVM pattern. [details](#mvvm-pattern)
  * Use SOLID. [details](#solid)
  * The `Core` will be responsible for managing plugins **only** (load/unload, manage errors etc). The `Core` package will be updated only when a new feature related to plugins needs to be added (never happened so far) or if a bug related to this small functionality has been solved. 
  * Everything that existed in the Core package has become a plugin (ModernUi, MediaPlayers, etc.). [details](#core-package)
* Code Reorganization
  * Panacea can be built as `x86`, `x64` and for `both`. The first 2 will require the least space, the third can be used for hospitals with terminals that have both architectures.
  * Use of private nuget server in order to avoid putting binaries in repositories. (Create packages for binaries that do not exist in public). Such repositories exist in out Nuget organization.
  * Everything plugin exists in its own repository. This adds maintainance value as well as the ability to integrate with CI (Jenkins).
  * All plugins that export functionality declare their APIs under `Panacea.Modularity` namespace in their own library and repository. Other plugins can use libraries under that namespace to integrate with others. This makes plugin interopability clearer. 
  * Integrate all libraries with Jenkins and our private Nuget server.
* Resources Optimization
  * Remove dependencies to external libraries that have proven problematic (DevExpress) and add alternatives.
  * Reduce panacea size: Some resources can be reduced. For example, since VLC is a plugin and there is a cleaner way for plugins to work with each other, there is the ability to make a plugin also modular which in this case VLC plugin can have subplugins which contain the binaries of various VLC versions and they can be picked on the server. So, instead of have a core package with 4 versions of VLC, we have the ability to pick only one plugin with specific vlc binaries version.
* UX Improvements
  * Create `Panacea.Controls` library for a more consistent UI
    * Base UX on material design.
    * Apply advanced extensions like asynchronous commands for buttons (automatically display a progress ring, disable the button until the command has finshed) that make UI better and development easier.
  * Move navigation bar at the bottom of the screen so users won't have to raise their hands more than is needed.
  * Reduce the usage of need for scrolling and switch to paged views.
  * Based on the above modify existing plugins UI to new look. Changes apply to;
    * All Entertainment Plugins
    * Hospital Information
    * Education
    * Surveys
    * Room Controls
    * Food Services
    * Television
 * New Feature Functionality
  * Users Management
   * Auto-management for Panacea Users (based on ADT messages)
  * Billing	
    * Remote buy services webapp (for relatives)
    * Buy many packages in one transaction
    * Payment Gateway Global Service with Payflow silent posts to avoid double payments
   * Accessibility	
     * SIP n Puff 
   * Hospital Staff	
     * Nurse Dashboard with video call and Patient Surveillance
   * Support/Maintenance
    * PSS  Redesign -Identify currently applied system patches per terminal & reapply them when they are not present based on local pss file info (ie. after reimaging)
  * Panacea Logs-Elevate Panacea Logs to event viewer
  * Panacea Automated Troubleshooter (PAT)
  * Panacea Launcher & System-CMD launch according to 64 and 32 bit and powershell option addition
  * Distribute and assign multiple Panacea versions in one installation

# MVVM Pattern

In one sentence, the MVVM Pattern is to use a separate class called a View Model which should contain all the logic that can be performed in a View. The View class will contain code only related to view logic (ex. animations). The greatest benefit is that coded tests can be created for the View Models and can be executed without the need of a user interface (need for a UI thread, an application and more). Other benefits include code quality, shorter maintainance. The change related to MVVM Pattern in Panacea is to change all APIs to use the pattern. A small library with all essentials for MVVM is called Panacea.Mvvm.
## v2
```csharp
public void NavigateToPage(FrameworkElement page);
```
In the example above, plugins could call the `NavigateToPage` method in order to show a specific view that contained all the logic. But a view can only be created in a dispatcher thread and moreover, since the pattern wasn't completely followed, testing it was impossible.

## v2.1
```csharp
public void NavigateToPage(ViewModelBase page);
```
The `ViewModelBase` class is defined in `Panacea.Mvvm`. It's a simple class and a View can be constructed and attached to it only on demand.

# SOLID
* Single responsibility - A class should only have a single responsibility, that is, only changes to one part of the software's specification should be able to affect the specification of the class. 
* Open–closed principle - Software entities should be open for extension,  but closed for modification. 
* Liskov substitution principle - Objects in a program should be replaceable with instances of their subtypes without altering the correctness of that program.
* Dependency inversion principle - One should depend upon abstractions, [not] concretions.

see [the wiki page for SOLID](https://en.wikipedia.org/wiki/SOLID).

# Core package
Extracted plugins from the old package are:
* **Clock** - simple plugin that adds a clock in the navigation bar
* **ModernUi** - a plugin responsible for the appearance of the application and exposes APIs that other plugins can use in order to show UI. Mvvm style.
* **Billing** - contains all the billing UI/logic that other plugins can use.
* **MediaPlayerContainer** - the user interface for media players. It cannot play any media but can embed and use other Media Player plugins in order to play various types of media.
  * **VLC Media Player** - can play anything VLC can play. It also contains the executable where media can play in their own process. Doesn't contain any VLC binaries, this is why at least one VlcBinaries plugin is required for it to work.
    * **VlcBinaries-2-1-2**
    * **VlcBinaries-2-1-5**
    * **VlcBinaries-2-2-5-1**
    * **VlcBinaries-2-2-6**
  * **AnalogTvMediaPlayer** - can play analog feeds from USB TV Tuners.
  * **AtscMediaPlayer** - can play ATSC Digital OTA TV.
  * **WebMediaPlayer** - can play HLS live streams.
* **WinSCard** - intergrates RFID/Smart card readers that support Windows WinSCard API.
* **SerialRfid** - intergrates RFID readers that can be accessed through a serial port.
* **Rfideas** - integrates Rfideas RFID readers using Rfideas SDK.
* **CastlesRfid** - integrates Castles RFID readers using Castles SDK.
* **Remedi** - intagrates Remedi terminals using the Remedi SDK.
* **Barco** - integrates Barco terminals.
* **RemoteActions** - adds the ability to execute remote actions on the terminal from the server (reboot, restart etc).
* **DebugInfo** - adds the ability to see advanced information directly on the terminal.


Example: `Panacea.Modularity.RfidReader`
 This package contains definitions about what a RFID Reader Plugin can do (eg expose an event `CardConnected`). Any plugin that adds support for RFID readers can implement the IPlugin type defined in this assembly. Any plugin that want to query Panacea and get all the plugins that expose RFID functionality must also use this package. (The compatibility check between producer and consumer plugins that use the same package is done on the version of the package).


If a plugin required to open a web page (Hospital Information, Games), developers had to call vague, dynamic functions:
```csharp
Host.RaiseEvent("web", new Dictionary<string, dynamic>() {
    { "url", "http://google.com" },
    { "ShowTitle", false" } 
});
```
There was no proper interface to understand what the options are for opening a web page. The same example can be applied for all plugin integration (Imprivata and more).

Comparison with new APIs: *_core variable holds a `PanaceaServices` instance which gets injected by Panacea into the plugins via constructor injection. It contains some basic functionality such as plugin loading, getting information from the server and logging. APIs under `Panacea.Modularity` extend this class using `Extension methods` to offer easier APIs to other plugins.*

```csharp
if (_core.TryGetWebBrowser(out IWebBrowserPlugin browser))
{
    browser.OpenUnmanagedTab("https://google.com", false);
}
else
{
    // there is no web browser plugin available
}
```
The same applies for functionality that was previously in the `Core` package:
```csharp
Host.ThemeManager.ConsumeItem("Television", new Media("HBO"), () => Host.MediaPlayer.Play());
```
which becomes more powerful in 2.1
```csharp

// TryGetMediaPlayer is an extension method declared in Panacea.Modularity.Media. Plugins that want to consume
// media functionality may call such functions which simply perform some queries on all loaded plugins to find
// plugins that expose functionality declared in Panacea.Modularity.Media. By nature, such plugins may or may
// not be currently loaded and this is expressed nicely in the code.
if (_core.TryGetMediaPlayer(out IMediaPlayer player))
{
    if (_core.TryGetBilling(out IBillingPlugin billing))
    {
        // API from Panacea.Modularity.Billing. It is implemented in Panacea.Modules.Billing
        // this line will:
        //       ask users to sign in if they haven't already
        //       check their active services and ask them to buy service in order to continue  
        if (await billing.TryConsumeItem(new Media("HBO"), out Service service))
        {
            player.Play();
        }
    }
    else
    {
        // there is no billing available. Just play immediately
        player.Play();
    }
}
else
{
    // no media player available
}
```

Although the way that plugins interact is based on `Service Location` pattern, it's the nature of the problem that demands it. Plugins are not guaranteed to be loaded and the checks must be done at runtime.

