# ToDo-List

git clone <YOUR_REPO_URL>
cd allocore_project

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
3.Run the API:
```bash
dotnet run
```
By default, the API listens on http://localhost:5141.

# Running the Frontend

1. Open a new terminal and navigate to the UI folder:
```bash
cd ../todo-ui
```

2. Install dependencies:

```bash
npm install
``` 

3. Start the dev server:
```bash
npm run dev
```

The UI will be available at http://localhost:5173.

Make sure the backend is running before using the UI.

