# DHI Domain Services Course Materials
Course materials for a [DHI Domain Services](https://github.com/DHI/DomainServices) course.

[![ci-webapi](https://github.com/DHI/DomainServicesCourse/workflows/ci-webapi/badge.svg)](https://github.com/DHI/DomainServicesCourse/actions/workflows/ci-webapi.yml)
[![ci-authorization](https://github.com/DHI/DomainServicesCourse/workflows/ci-authorization/badge.svg)](https://github.com/DHI/DomainServicesCourse/actions/workflows/ci-authorization.yml)
[![ci-job-orchestrator](https://github.com/DHI/DomainServicesCourse/workflows/ci-job-orchestrator/badge.svg)](https://github.com/DHI/DomainServicesCourse/actions/workflows/ci-job-orchestrator.yml)

![](Images/services-communication.png)

## How to Get Started

To set up the sample application to run on you own machine you have to configure and run the following four services:

### Authorization Server

The Authorization Server (in `AuthorizationServer.sln`) uses a pair of RSA signing keys for generation and validation of JWT access tokens. You need to generate and store these RSA keys as environment variables as described in the [documentation](https://dhi-developer-documentation.azurewebsites.net/domain_services/faq/#how-to-create-a-pair-of-rsa-signing-keys-for-generation-and-validation-of-jwt-access-tokens).

> NOTE: It requires a restart to enable new environment variables.

To build and run the Authorization Server, use the `BuildAndRun.bat` file. 

## Web Server

The web API is configured with a PostgreSQL database. You have to create an environment variable called "PostgreSqlConnectionString" with the connection string.

> NOTE: It requires a restart to enable new environment variables.

To build and run the Web Server (in `WebApi.sln`), use the `BuildAndRun.bat` file.

## Job Orchestrator

To install the Job Orchestrator (in `JobOrchestratorWinService.sln`) as a Windows Service run `install.bat`

> NOTE: Remember to force administrator privileges.

Once installed, the Job Orchestrator is managed (start/stop) through the standard Services application:

![](Images/windows-services.png)

The Job Orchestrator is configured to log to the Event Viewer:

![](Images/event-viewer.png)

## Job Host

Your own machine will be acting as the job host. 

Build and run the DeployWorkflowService console application (in `workflow.sln`). This will create a "Deployment" folder (in the debug or release folder).

From here you must run `DHI.Workflow.Service.WinSvcHost.Install.bat`.

> NOTE: Remember to force administrator privileges.

Once installed, the workflow service is managed (start/stop) through the standard Services application (see above).


