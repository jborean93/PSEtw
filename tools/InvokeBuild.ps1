using namespace System.Collections
using namespace System.IO

#Requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [Manifest]
    $Manifest,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug'
)

#region Build

task Clean {

}

task BuildManaged {

}

task BuildModule {

}

task BuildDocs {

}

task Sign {

}

task Package {

}

#endregion Build

#region Test

task Sanity {

}

task UnitTests {

}

task PesterTests {

}

#endregion Test

task Build -Jobs Clean, BuildManaged, BuildModule, BuildDocs, Sign, Package

task Test -Jobs Sanity, UnitTests, PesterTests
