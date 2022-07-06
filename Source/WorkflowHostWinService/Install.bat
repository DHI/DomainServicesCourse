cd %~dp0
@set serviceName="DHI Workflow Host"
sc.exe create %serviceName% start=delayed-auto binpath="%~dp0WorkflowHostWinService.exe" 
sc.exe description %serviceName% "Workflow host for DHI Domain Services" 
sc.exe start %serviceName%
