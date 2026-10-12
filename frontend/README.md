# Frontend

## Installation and SonarQube (Windows PowerShell)

Run these commands from the `frontend` directory. Use `npm.cmd` to avoid
PowerShell blocking `npm.ps1`; no execution policy change is needed.

```powershell
npm.cmd ci
npm.cmd run build
npm.cmd test -- --watch=false
```

SonarQube is configured for `http://localhost:9000` and project key
`tasinmazFront` in `sonar-project.properties`. Start your SonarQube server,
then provide a token for that project only in the current terminal:

```powershell
$env:SONAR_TOKEN = Read-Host 'SonarQube token' -MaskInput
npm.cmd run sonar
Remove-Item Env:SONAR_TOKEN
```

`-MaskInput` requires PowerShell 7. In Windows PowerShell 5.1, use:

```powershell
$sonarCredential = Get-Credential -UserName 'sonar' -Message 'Enter the SonarQube token as the password'
$env:SONAR_TOKEN = $sonarCredential.GetNetworkCredential().Password
npm.cmd run sonar
Remove-Item Env:SONAR_TOKEN
Remove-Variable sonarCredential
```

Do not put the token in tracked files. The scan separates `*.spec.ts` tests
from application sources. Coverage is not configured yet.

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.7.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
