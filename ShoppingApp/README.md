# Shopping Application (console)

Open `ShoppingApplication.sln` in Visual Studio and press F5, or from this folder run:

    dotnet run --project ShoppingApplication

Needs the .NET 8 SDK or newer (internet needed once, to restore the NuGet packages:
Newtonsoft.Json, QuestPDF, Spectre.Console). The packages use floating versions ("*") so the first
restore always works; run `dotnet list package` afterwards if you want to pin the versions.

## Project layout
- Model/        User, Address, Product, CartItem, Invoice
- Repository/   UserRepository, ProductRepository, InvoiceRepository (data access)
- Service/      AuthService, CartService, InvoiceService (PDF), Logger (file log), Notifier (customer messages)
- View/         UserView (all console screens)
- Validator.cs  input checks, Program.cs entry point + crash logging

## Created at run time (next to the .exe, in bin/Debug/net8.0/)
- ShoppingAppData/users.json, products.json
- Logs/app.log
- Output/INV-<date-time>.pdf  (invoice, opened automatically after checkout)

## Keys
- Home: type a Product ID (adds 1-5), C = cart, X = logout
- Cart: A add, E edit quantity, R remove, K checkout, B back
- Login: B back, S sign up, F forgot password

## Window size
The home and cart screens redraw by themselves when the window is resized.
For the whole home screen at once use a window of about 70 columns x 30 rows or more;
a hint appears above the prompt when the window is smaller.
