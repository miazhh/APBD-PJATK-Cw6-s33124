Projekt to aplikacja backendowa napisana w ASP.NET Core Web API, służąca do zarządzania wizytami pacjentów w przychodni medycznej. 
Głównym celem zadania jest praktyczne przećwiczenie komunikacji z bazą danych SQL Server wyłącznie przy użyciu technologii ADO.NET, 
z całkowitym pominięciem narzędzi ORM takich jak Entity Framework. Aplikacja udostępnia endpointy do wykonywania operacji CRUD na wizytach, 
dbając o mapowanie danych na obiekty DTO oraz sprawdzanie reguł biznesowych, takich jak weryfikacja konfliktów terminów u danego lekarza 
czy blokowanie usuwania zakończonych spotkań. Całość kładzie duży nacisk na dobre praktyki programistyczne: wykorzystanie metod asynchronicznych 
oraz pełną parametryzację zapytań w celu ochrony przed atakami SQL Injection.
