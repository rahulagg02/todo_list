# ToDo Application

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

# Approach

I made a to‑do application that is responsive and intuitive. On the backend, I used a simple factory pattern so that it’s easy to switch between in‑memory storage and SQLite via EF Core by changing just an HTTP header. On the frontend, I set up a debounced search to avoid flooding the server and added inline editing where you can click any task, change its title and then press Enter or click away to save.

I also added a completed‑tasks feature where as soon as you check a box, the task smoothly moves down into a "Completed" section. If you change your mind, simply uncheck it and it returns to your pending list. To make sure completed tasks persist across reloads they’re stored in localStorage.

# Challenges

1. Making the provider switch feel instant without UI flicker involved juggling cached data and temporarily pausing animations.

2. Getting inline edit inputs to autofocus reliably meant coordinating Vue’s reactive updates with nextTick() so the input element is actually in the DOM before calling .focus().

3. Keeping completed tasks in sync required careful handling of local and server state, moving items between lists, deleting and restoring them also persisting the completed list separately from the API.

# Resources

https://www.geeksforgeeks.org/vue-js/#

https://learn.microsoft.com/en-us/ef/core/

https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-9.0

Axios GitHub

Lodash Debounce

<img width="1451" alt="Screenshot 2025-04-28 at 7 22 44 PM" src="https://github.com/user-attachments/assets/9b299f94-f3cb-4e14-9bc4-69014a5d4939" />



