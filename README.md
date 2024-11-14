# Bean-Scene-ReservationSystem

This is an MVC system for the Bean Scene reservation system.

## Technologies Used

- **Programming Language**: C#
- **Frameworks and Libraries**:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.8
  - `Microsoft.EntityFrameworkCore.Sqlite` 8.0.8
  - `Microsoft.EntityFrameworkCore.Tools` 8.0.8
  - `"Microsoft.AspNetCore.Identity.UI"`  8.0.8
  - `Microsoft.EntityFrameworkCore.SqlServer` 8.0.8
## Setup Instructions

1. Clone the repository to your local machine:
   git clone https://github.com/Bean-Scene-Zachary-and-Alex/Bean-Scene-ReservationSystem.git

2.  Configured your local database in appsetting.json  "Server=bean-scene-system.database.windows.net;Database=Bean-Scene;User ID=bean-scene-admin;Password=q@1#kEDWPV$kv3O%1beU@EZ;MultipleActiveResultSets=true;TrustServerCertificate=True"
3.  Push all migration  : dotnet ef database update 



User Manager:
Admin : admin@BeanScene
password:111111

Staff:staff@BeanScene
password:111111


When you create user account for both of them  after that shut down programm and run again  and login.
