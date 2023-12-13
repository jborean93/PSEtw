# PSEtw

[![Test workflow](https://github.com/jborean93/PSEtw/workflows/Test%20PSEtw/badge.svg)](https://github.com/jborean93/PSEtw/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/jborean93/PSEtw/branch/main/graph/badge.svg?token=b51IOhpLfQ)](https://codecov.io/gh/jborean93/PSEtw)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/PSEtw.svg)](https://www.powershellgallery.com/packages/PSEtw)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/jborean93/PSEtw/blob/main/LICENSE)

PowerShell module for capturing ETW events in realtime.
Currently this supports Manifest and Trace Logging ETW providers, MOF and WPP providers will most likely not work.

See [PSEtw index](docs/en-US/PSEtw.md) for more details.

## Requirements

These cmdlets have the following requirements

* PowerShell v5.1 or newer

## Examples
The two important cmdlets in this module are [Trace-PSEtwEvent](./docs/en-US/Trace-PSEtwEvent.md) and [Register-PSEtwEvent](./docs/en-US/Register-PSEtwEvent.md).
The `Trace-PSEtwEvent` cmdlet is designed for interactive usages where a user starts a trace and cancels it as needed.
It can also be used in non-interactive scenarios but it will need to be stopped with [Stop-PSEtwTrace](./docs/en-US/Stop-PSEtwTrace.md).
The following will start a trace for the `PowerShellCore` provider and capture any events with the keyword `Pipeline`.

```powershell
Trace-PSEtwEvent -Provider PowerShellCore -KeywordsAll Pipeline
```

The default format displays the provider name, the name of the task, and the event message if one is present.
There are more properties in the output object which can be retrieved like any normal object.
This can be combined with the pipeline to log traces to a file or do any other custom task with the trace objects themselves.
For example the following will output each trace for the Windows PowerShell provider with the `Cmdlets` keyword to a file.
The Yaml example would require a separate module like [Yayaml](https://github.com/jborean93/PowerShell-Yayaml)

```powershell
Trace-PSEtwEvent -Provider Microsoft-Windows-PowerShell -KeywordsAny Cmdlets |
    ForEach-Object { $_ | ConvertTo-Json -Depth 3 } | Set-Content C:\temp\ps.log -Encoding UTF8

Trace-PSEtwEvent -Provider Microsoft-Windows-PowerShell -KeywordsAny Cmdlets |
    ForEach-Object { $_ | ConvertTo-Yaml -Depth 3; "`n"` } | Set-Content C:\temp\ps.log -Encoding UTF8
```

When you wish to stop the trace press `ctrl+c` and the trace should stop.

The `Register-PSEtwEvent` cmdlet is similar to the [Register-ObjectEvent](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/register-objectevent?view=powershell-7.4) in that is creates a PowerShell event subscriber specifically for ETW events.
ETW events received will go through the PowerShell eventing system and used just like any other event.
It is important to use [Unregister-Event](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/unregister-event?view=powershell-7.4) to unregister a subscriber created by `Register-PSEtwEvent` as the ETW trace session can persist beyond the current process.

When a trace is started the module will attempt to open the ETW trace session called `PSEtw` and if that is not present it will create a new one under that name.
Only administrators can create an ETW trace session and members of the `Performance Log Users` can open an existing trace.
A custom trace session can also be used by using the [New-PSEtwTraceSession](./docs/en-US/New-PSEtwSession.md) cmdlet.

Each cmdlet supports tab completion when using `-Provider ...` with a manifest based ETW provider.
This makes it easy to figure out values that can be set for the `-KeywordsAll`, `-KeywordsAny`, and `-Level` parameters.
TraceLogging ETW providers have no manifest registered on the system so cannot be completed and values typically specified through the integer values.
Some common tab completion scenarios are

```powershell
# Display all manifest-based providers starting with 'Po'
Trace-PSEtwEvent -Provider Po<ctrl+space>

# Display all keywords for the PowerShellCore provider
Trace-PSEtwEvent -Provider PowerShellCore -KeywordsAny <ctrl+space>
```

## Installing
The easiest way to install this module is through [PowerShellGet](https://docs.microsoft.com/en-us/powershell/gallery/overview).

You can install this module by running either of the following `Install-PSResource` or `Install-Module` command.

```powershell
# Install for only the current user
Install-PSResource -Name PSEtw -Scope CurrentUser
Install-Module -Name PSEtw -Scope CurrentUser

# Install for all users
Install-PSResource -Name PSEtw -Scope AllUsers
Install-Module -Name PSEtw -Scope AllUsers
```

The `Install-PSResource` cmdlet is part of the new `PSResourceGet` module from Microsoft available in newer versions while `Install-Module` is present on older systems.

## Contributing

Contributing is quite easy, fork this repo and submit a pull request with the changes.
To build this module run `.\build.ps1 -Task Build` in PowerShell.
To test a build run `.\build.ps1 -Task Test` in PowerShell.
This script will ensure all dependencies are installed before running the test suite.
Most tests require Administrative access to run.
