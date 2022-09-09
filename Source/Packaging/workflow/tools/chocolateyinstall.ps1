$ErrorActionPreference = 'Stop'; # stop on all errors
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$validExitCodes = @(0)

#
# run installation utilities
#

$statementsToRun = "& ""$toolsDir\WorkflowInstall.bat"" ""$toolsDir"""
Start-ChocolateyProcessAsAdmin $statementsToRun -validExitCodes $validExitCodes