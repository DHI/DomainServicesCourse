@set serviceName="DHI Domain Services Job Orchestrator"
sc.exe create %serviceName% start=delayed-auto binpath="%~dp0JobOrchestratorWinService.exe" 
sc.exe description %serviceName% "Job orchestrator for DHI Domain Services" 
sc.exe start %serviceName%