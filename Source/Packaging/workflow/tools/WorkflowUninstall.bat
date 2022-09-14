@set serviceName="DHI Workflow Host"
sc.exe stop %serviceName%
sc.exe delete %serviceName%