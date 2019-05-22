# Panacea

## Intro
The purpose of Panacea v2.0 was to have a modular application where plugins could be loaded/unloaded on demand or depending on configuration/assignmenets.

Panacea v2.0 failed miserably in few points:
* It doesn't provide clean development guidelines and APIs to make plugin integration easy and straight-forward.
* Not everything is a plugin. Since there are no clean ways for plugins to work with each other, what is called `Core` ended up having more functionality than all the plugins combined (Theming, Media etc). This created a bloated Panacea Core package which requires a large amount of disk space even if some features are not in use by the installation where Panacea runs. Moreover, changing a single line of code (which happens a lot when so much functionality exists in one package) requires the entire package to be updated, transferred and stored which causes pollution to our servers.
* Core/Plugins compatibility is a mess as Core/Plugins are analyzed on IL level to list public Types and Method signatures provided/called and then compared on the server side to match compatible modules. This is done only for 1 assembly (`PanaceaLib`) causing dependency issues with other common assemblies (Newtonsoft, ServiceStack.Json etc).

The changes in Panacea v2.1 are:
* Use MVVM pattern (Panacea.Mvvm).
* Use Dependency Injection in order to make Panacea Testable with code.
* The `Core` will be responsible for managing plugins **only** (load/unload, manage errors etc). The `Core` package will be updated only when a new feature related to plugins needs to be added (never happened so far) or if a bug related to this small functionality has been solved.
* All plugins that export functionality or plugins that expect functionality from other plugins will declare their APIs under `Panacea.Modularity` namespace in their own Nuget package.

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

Comparison with new APIs:

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
if (_core.TryGetMediaPlayer(out IMediaPlayer player))
{
    if (_core.TryGetBilling(out IBillingPlugin billing))
    {
        if (await billing.ConsumeItem(new Media("HBO")))
        {
            player.Play();
        }
    }
    else
    {
        // there is billing available. Just play immediately
        player.Play();
    }
}
else
{
    // no media player available
}
```

Although the way that plugins interact is based on `Service Location` pattern, it's the nature of the problem that demands it. Plugins are not guaranteed to be loaded and the checks must be done at runtime.

## Progress

### Modules 
Name | Repository | Status
--- | --- | ---
Advertisements | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Advertisements) | Complete
AlarmClock |  | 
AnalogMediaPlayer |  | 
AtscMediaPlayer |  |
Barco |  |
Billing | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Billing) | Draft
Books | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Books) | In Progress
CardActions | |
Castles | |
Centrak | |
Chromium | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Chromium) | Complete
Citrix | |
ClinicalButtons | |
Clock | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Clock) | Complete
Computrition | |
ContactStaff | |
DeveloperPanel | |
DisplayMessages | |
Education | |
Exit | |
Favorites | |
FlytechK948 | |
FoodServices | |
Games | |
HealthcareTeam | |
HID | |
HippocratesHospital | |
HospitalForms | |
Hospital Information | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.HospitalInformation) | Complete
Hotspot | |
Imprivata | |
InstantMessage | |
KurentoVideoCall | |
MediaPlayerContainer | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.MediaPlayerContainer) | Complete
MedicalApplications | |
Meditech | |
Mirror | |
ModernUI | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.ModernUi) | In Progress (50%)
Movies | |
News | |
OnyxBE181 | |
PanaceaInformation | |
Pdfjs | |
PhotoGallery | |
Podcasts | |
Postcard | |
Radio | |
RdpLauncher | |
Reboot | |
Relay Module Plugins | |
Remedi | |
RemoteDesktop | |
Reports | |
Rfideas | |
RogersWebMediaPlayer | |
RoomControl | |
ScreenShare | |
Serial | |
SipNPuff | |
Skype | |
Speech | |
Surveys | |
TeamViewer | |
Telephone | |
Television | |
VlcBinaries-2-1-2 | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcBinaries-2-2-6) | Complete
VlcBinaries-2-1-5 | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcBinaries-2-2-6) | Complete
VlcBinaries-2-2-6 | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcBinaries-2-2-6) | Complete
VlcBinaries-2-2-5-1 | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcBinaries-2-2-6) | Complete
VlcBinaries-2-2-6 | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcBinaries-2-2-6) | Complete
VLCMediaPlayer | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.VlcMediaPlayer) | Complete
Weather | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.Weather) | Complete
WebBrowser | [Here](https://git.i3inc.ca/Panacea2-1/Panacea.Modules.WebBrowser) | In Progress (80%)


