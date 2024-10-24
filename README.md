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

2.  Configured your local database in appsetting.json  "Server=beanscene.database.windows.net;Database=BeanScene;User ID=BeanScene;Password=Programming1!;MultipleActiveResultSets=true;TrustServerCertificate=True",
3.  Push all migration  : dotnet ef database update 
