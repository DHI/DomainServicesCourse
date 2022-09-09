$ErrorActionPreference = 'Stop'; # stop on all errors
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$validExitCodes = @(0)

#
# run installation utilities
#

$statementsToRun = "& ""C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe"" ""$toolsDir\WorkflowHostWinService.exe"""
Start-ChocolateyProcessAsAdmin $statementsToRun -validExitCodes $validExitCodes

$statementsToRun = "NET START ""DHI Workflow Host"""
Start-ChocolateyProcessAsAdmin $statementsToRun -validExitCodes $validExitCodes