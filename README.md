# Panacea

## Intro
The purpose of Panacea v2.0 was to be a modular application where plugins could be loaded/unloaded on demand or depending on configuration/assignmenets.

Panacea v2.0 failed miserably in few points:
* It doesn't provide clean development guidelines and APIs to make plugin integration easy and straight-forward.
* Not everything is a plugin. Since there are no clean ways for plugins to work with each other, what is called `Core` ended up having more functionality than all the plugins combined (Theming, Media, RFID etc). This created a bloated Panacea Core package which requires a large amount of disk space even if some features are not in use by the installation where Panacea runs. Moreover, changing a single line of code (which happens a lot when so much functionality exists in one package) requires the entire package to be updated, transferred and stored which causes pollution to our servers.
* Core/Plugins compatibility is a mess as Core/Plugins are analyzed on IL level to list public Types and Method signatures provided/called and then compared on the server side to match compatible modules. This is done only for 1 assembly (`PanaceaLib`) causing dependency issues with other common assemblies (Newtonsoft, ServiceStack.Json etc).
* There wasn't any theming library to keep Panacea's UI consistent.

The changes in Panacea v2.1 are:
* Code Architecture
  * Use MVVM pattern (Panacea.Mvvm).
  * Use Dependency Injection in order to make Panacea Testable with code.
  * The `Core` will be responsible for managing plugins **only** (load/unload, manage errors etc). The `Core` package will be updated only when a new feature related to plugins needs to be added (never happened so far) or if a bug related to this small functionality has been solved.
  * Everything that existed in the Core package has become a plugin (ModernUi, MediaPlayers etc).
  * Use of private nuget server in order to avoid putting binaries in repositories. (Create packages for binaries that do not exist in public).
* Organizational
  * Everything is in its own repository. This will add maintainance value as well the ability to integrate with CI (Jenkins).
  * All plugins that export functionality declare their APIs under `Panacea.Modularity` namespace in their own library. Other plugins can use libraries under that namespace to integrate with others. This makes plugin interopability clearer.
  * Integrate all libraries with Jenkins and our private Nuget server.
* General improvements
  * Remove dependencies to external libraries that have proven problematic (DevExpress) and investigate in alternatives.
  * Reduce panacea size: Some resources can be reduced. For example, since VLC is a plugin and there is a cleaner way for plugins to work with each other, there is the ability to make a plugin also modular which in this case VLC plugin can have subplugins which contain the binaries of various VLC versions and they can be picked on the server. So, instead of have a core package with 4 versions of VLC, we have the ability to pick only one plugin with specific vlc binaries version.
* UX Improvements
  * Create `Panacea.Controls` library with theming capabilities.

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

