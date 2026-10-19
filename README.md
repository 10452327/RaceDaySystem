# RaceDay RESTful API System – Part 2 Submission

## 1. Overview

The RaceDay API is an ASP.NET Core RESTful backend designed to manage sporting events, event categories, participant enrolments, and race results. The system provides authentication and role-based access for organisers and participants. It also uses automated build and testing through GitHub Actions to help maintain code quality and reliability.

## 2. User Roles

### Organiser
- Create, update, and manage race events.
- Configure event pricing and categories, including distance and age categories.
- View participant enrolments for events.
- Record race finishing times and positions.

### Participant
- Register an account and log in.
- Browse available sporting events and categories.
- Enrol in specific event categories.
- View personal race results.

## 3. Setup and Run

Follow these steps to run the application locally.

### Step 1: Clone the Repository

Open PowerShell or a terminal and execute:

```bash
git clone https://github.com/10452327/RaceDaySystem.git
cd RaceDaySystem
```

### Step 2: Configure the Database

Open `RaceDay/appsettings.json` and verify the database connection string. Update it if necessary to match your local database configuration.

### Step 3: Install Entity Framework Core Tools

If the Entity Framework Core command-line tools are not installed, run:

```bash
dotnet tool install --global dotnet-ef
```

If they are already installed, this step can be skipped.

### Step 4: Create or Update the Database

Run the following command from the repository root:

```bash
dotnet ef database update --project RaceDay
```

### Step 5: Run the API

Start the application using:

```bash
dotnet run --project RaceDay
```

### Step 6: Open Swagger

Once the API is running, open the following URL in your browser:

http://localhost:5066/swagger/index.html

Swagger provides an interactive interface for viewing and testing the API endpoints.

**Swagger API Screenshot**

The screenshot below shows the RaceDay API running locally in Swagger, with endpoints for authentication, enrolments, and events.

![RaceDay API running in Swagger]<img width="1915" height="978" alt="Screenshot 2026-10-19 184126" src="https://github.com/user-attachments/assets/46c89858-1d3e-4809-8142-e82acbca97b8" />


*Figure 1: RaceDay RESTful API endpoints displayed in Swagger.*

> Ensure the screenshot file is saved in the repository at `docs/images/raceday-swagger.png`. If you use a different filename or folder, update the image path above accordingly.

## 4. SQL Script and Database Migrations

The project uses the Entity Framework Core Code-First approach to manage the database schema. Database tables and relationships are defined through application models and migrations.

The migration files and model snapshots can be reviewed in the `RaceDay/Migrations` directory, if present in the project.

To initialise or update the database, run:

```bash
dotnet ef database update --project RaceDay
```

## 5. CI/CD Pipeline

The project uses GitHub Actions to automate the build and testing process.

The workflow configuration is located at:

`.github/workflows/ci.yml`

The pipeline is configured to build the project and run automated tests when the relevant GitHub events occur.

**GitHub Actions Build Screenshot**

![Successful RaceDay GitHub Actions workflow]<img width="1919" height="982" alt="Screenshot 2026-10-19 184204" src="https://github.com/user-attachments/assets/f3e23497-b0ae-4ba8-9f46-bf5dab165571" />


*Figure 2: GitHub Actions CI/CD workflow completing successfully.*

The GitHub Actions screenshot shows a green success indicator for the `build-and-test` job. The workflow summary reports a successful run, confirming that the configured pipeline completed successfully for the displayed commit.

View the workflow run here:

https://github.com/10452327/RaceDaySystem/actions/runs/39282805406

## 6. Unit Testing

The RaceDay project includes a `RaceDay.Tests` project for automated testing using xUnit. Unit tests are used to verify the behaviour of the API, including participant registration, authentication, and input validation.

The tests help identify errors and confirm that the application responds correctly to valid and invalid requests.

### Unit Testing Results

The following screenshot shows the test results in Visual Studio's Test Explorer.

![RaceDay unit testing results]<img width="1919" height="1079" alt="Screenshot 2026-10-19 174331" src="https://github.com/user-attachments/assets/4d80d98e-47db-47ad-a4b2-a6ce2564620b" />


**Test results:**
- **Total tests:** 3
- **Passed:** 3
- **Failed:** 0
- **Skipped:** 0
- **Status:** Successful

*Figure 3: RaceDay unit tests executed successfully in Visual Studio Test Explorer.*

These results demonstrate that all three tests passed during this test run, with no failures or skipped tests.

To run the tests again, execute the following command from the repository root:

```bash
dotnet test
```

## 7. Video Demonstration

**YouTube Video Link:**

The video demonstration should show the application running, the Swagger interface, the main API endpoints, relevant registration and authentication functionality, and the execution of automated tests.

## 8. References and AI Disclosure

### References

GitHub (n.d.) *GitHub Actions documentation*. Available at: https://docs.github.com/en/actions (Accessed: 19 October 2026).

Microsoft (n.d.) *Create web APIs with ASP.NET Core*. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/ (Accessed: 19 October 2026).

Microsoft (n.d.) *Entity Framework Core documentation*. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 19 October 2026).

xUnit (n.d.) *xUnit.net documentation*. Available at: https://xunit.net/ (Accessed: 19 October 2026).

### AI Disclosure

GitHub Copilot, were used as collaborative assistants for tasks such as code structuring, debugging API routing, developing test ideas. The project authors are responsible for reviewing the code and ensuring that the submitted documentation accurately reflects the implemented system.
