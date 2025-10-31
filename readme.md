This sample demonstrates connecting forms to Workflow Engine. See the detailed documentation [here](https://workflowengine.io/documentation/forms).

# How to run the sample

1. Create an empty Microsoft SQL Server (MSSQL) database. This sample works only with Microsoft SQL Server.
2. In [/backend/WorkflowApi/appsettings.json](https://github.com/optimajet/workflow-forms-sample/blob/master/backend/WorkflowApi/appsettings.json), update the connection string to point to your database.
3. From the root of the cloned repository, run:
    ```bash
    cd backend
    dotnet build
    cd WorkflowApi
    dotnet run
    ```
4. The backend will start and automatically run the required scripts against your database. To verify a successful start, open `https://localhost:5141/swagger/` in your browser—you should see the project's API description.
5. From the repository root, run:
    ```bash
    cd frontend
    npm install
    npm run dev
    ```
6. Open `http://localhost:5142/` to view the sample UI. Usage instructions are available [here](https://workflowengine.io/documentation/forms-sample).
