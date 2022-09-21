dotnet publish -p:PublishProfile=FolderProfile
@set buildFolder=bin\release\net6.0-windows\win-x64
xcopy %buildFolder%\DHI.Workflow.CodeWorkflowEngine.* %buildFolder%\publish\ /y
if not exist %buildFolder%\publish\log\ mkdir %buildFolder%\publish\log