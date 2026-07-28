# JobMailer API

ASP.NET Core API for sending job-application emails and logging submissions to an Excel workbook.

## Run locally

```powershell
dotnet run
```

The API is available at `http://localhost:5067` and Swagger is at:

`http://localhost:5067/swagger`

## Endpoint

`POST /api/JobApplications`

The React frontend should send requests to:

`http://localhost:5067/api/JobApplications`

When the Vite frontend runs at `http://localhost:5173`, allow that origin in the CORS policy in `Program.cs`.

## Configuration

Configure the `Smtp` and `JobMailer:ExcelPath` settings with .NET user secrets for local development. Do not commit real SMTP passwords to source control.
