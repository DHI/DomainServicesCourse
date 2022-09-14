cd %1
@set serviceName="DHI Workflow Host"
sc.exe create %serviceName% start=delayed-auto binpath="%1\WorkflowHostWinService.exe" 
sc.exe description %serviceName% "Workflow host for DHI Domain Services" 
sc.exe start %serviceName%
