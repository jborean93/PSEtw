# PSEtw

[![Test workflow](https://github.com/jborean93/PSEtw/workflows/Test%20PSEtw/badge.svg)](https://github.com/jborean93/PSEtw/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/jborean93/PSEtw/branch/main/graph/badge.svg?token=b51IOhpLfQ)](https://codecov.io/gh/jborean93/PSEtw)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/PSEtw.svg)](https://www.powershellgallery.com/packages/PSEtw)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/jborean93/PSEtw/blob/main/LICENSE)

PowerShell module for capturing ETW events in realtime.
Currently this supports Manifest and Trace Logging ETW providers, MOF and WPP providers will not work.

See [PSEtw index](docs/en-US/PSEtw.md) for more details.

## Requirements

These cmdlets have the following requirements

* PowerShell v5.1 or newer

## Examples
TODO: Add examples here

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
