# Vue + .NET Core To-Do App with Dynamic Factory Selection

Backend: ASP.NET Core Web API (C#), Entity Framework Core with SQLite

Frontend: Vue 3 + Vite, Axios for HTTP requests, Lodash.debounce for search optimization


# Running the Backend

1. Navigate to the API project:

```bash
cd Todo.Api
dotnet restore
```
2. Apply EF Core migrations and create SQLite database:

```bash
dotnet tool install --global dotnet-ef  
dotnet ef database update
```
3. Run the API:
```bash
dotnet run
```
By default, the API listens on [http://localhost:5141](http://localhost:5141/api/todos)

# Running the Frontend

1. Open a new terminal and navigate to the UI folder:
```bash
cd todo-ui
```

2. Install dependencies:

```bash
npm install
``` 

3. Start the dev server:
```bash
npm run dev
```

The UI will be available at http://localhost:5173

Make sure the backend is running before using the UI.


<img width="1451" alt="Screenshot 2025-04-30 at 5 41 36 PM" src="https://github.com/user-attachments/assets/44ba0d48-7f90-40b7-9ae2-8cc7bdc1efe9" />




