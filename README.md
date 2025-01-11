# Bean-Scene Reservation System

This is an MVC-based reservation system for the Bean Scene restaurant, developed using C# and .NET technologies.

## Technologies Used

- **Programming Language**: C#
- **Frameworks and Libraries**:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.8
  - `Microsoft.EntityFrameworkCore.Sqlite` 8.0.8
  - `Microsoft.EntityFrameworkCore.Tools` 8.0.8
  - `Microsoft.AspNetCore.Identity.UI` 8.0.8
  - `Microsoft.EntityFrameworkCore.SqlServer` 8.0.8

## Features

- User authentication and role-based access.
- Admin, Manager, and Staff accounts for role-specific operations.
- Integration with an SQL database for persistent storage.

## Setup Instructions

### Prerequisites

- Ensure you have the following installed:
  - .NET SDK (version 8.0 or later)
  - SQLite or SQL Server for database management

### Steps

1. **Clone the repository**  
   Clone the project to your local machine using the following command:  
   ```bash
   git clone https://github.com/z-evm/bean-scene-mvc.git
2. **Configure the Database**
   Update the appsettings.json file with your database connection details:
   `"ConnectionStrings": {
      "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User ID=YOUR_USER;Password=YOUR_PASSWORDTrustServerCertificate=True"
     }`
3. **Apply Migrations**
   `dotnet ef database update`
4. **Run the application**
   `dotnet run`

### Default Accounts

The system seeds the following default user accounts during the initial setup:
  - **Admin Account**
      - Email: admin@beanscene.com
      - Password: password
  - **Staff Account**
      - Email: staff@beanscene.com
      - Password: password
  - **Manager Account**
      - Email: manager@beanscene.com
      - Password: password

### License

This project is licensed under the MIT License. See the LICENSE file for details.
