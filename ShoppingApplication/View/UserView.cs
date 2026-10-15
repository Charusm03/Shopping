using ShoppingApplication.Model;
using ShoppingApplication.Repository;
using ShoppingApplication.Service;
using Spectre.Console;
using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace ShoppingApplication.View
{
    internal class UserView
    {
        private readonly AuthService _auth;
        private readonly CartService _cart;
        private readonly ProductRepository _products;
        private readonly InvoiceService _invoices;
        private const int MaxAttempts = 3;

        public UserView(AuthService auth, CartService cart, ProductRepository products, InvoiceService invoices)
        {
            _auth = auth;
            _cart = cart;
            _products = products;
            _invoices = invoices;
        }

        public void RunApplicationLoop()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(@"Welcome to the Shopping App!
==================================
SHOPPING APPLICATION
==================================
1. Sign Up
2. Login
3. Exit
----------------------------------
Enter choice: ");

                switch ((Console.ReadLine() ?? "").Trim())
                {
                    case "1":
                        if (SignUpPage())
                        {
                            DashboardPage();
                        }
                        break;
                    case "2":
                        if (LoginPage())
                        {
                            DashboardPage();
                        }
                        break;
                    case "3":
                        Logger.Info("Application exited");
                        Console.WriteLine(" Happy shopping!");
                        return;
                    default:
                        Logger.Warn("Invalid menu choice");
                        Console.WriteLine(" Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                }
            }
        }

        private bool LoginPage()
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Console.Clear();
                Console.WriteLine("============ LOGIN ============");
                Console.Write("Username : ");
                var username = (Console.ReadLine() ?? "").Trim();
                Console.Write("Password : ");
                var password = ReadPassword();

                if (_auth.Login(username, password))
                {
                    Console.WriteLine("\n[SUCCESS] Login successful!");
                    Thread.Sleep(1000);
                    return true;
                }
                Console.WriteLine($"\n[ERROR] ({attempt}/3). Invalid username or password");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            Logger.Warn("Too many failed login attempts");
            return false;
        }

        private bool SignUpPage()
        {
            Console.Clear();
            Console.WriteLine("========== SIGN UP ==========");
            var first = ReadValid("First Name       : ", Validator.FirstName);
            var last = ReadValid("Last Name (opt)  : ", Validator.LastName);
            var dobText = ReadValid("DOB (DD/MM/YYYY): ", Validator.Dob);
            var phone = ReadValid("Phone Number     : ", Validator.Phone);
            var username = ReadValid("Username         : ", name =>
            {
                var formatValidation = Validator.Username(name);
                if (formatValidation is not null)
                {
                    return formatValidation;
                }

                if (_auth.UserNameExists(name))
                {
                    return "Username already taken.";
                }
                return null;
            });

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Password: 8+ chars, 1 upper, 1 lower, 1 number, 1 special");
            Console.ResetColor();
            string password;
            while (true)
            {
                Console.Write("Password : ");
                password = ReadPassword();
                var formatValidation = Validator.Password(password);
                if (formatValidation != null)
                {
                    Console.WriteLine($"[ERROR] {formatValidation}");
                    continue;
                }

                Console.Write("Confirm Password : ");
                if (ReadPassword() == password)
                {
                    break;
                }
                Console.WriteLine("[ERROR] Passwords do not match. Try again.");
            }

            var user = new User
            {
                FirstName = first,
                LastName = string.IsNullOrWhiteSpace(last) ? null : last,
                Dob = DateTime.ParseExact(dobText, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                Phone = phone,
                Username = username
            };
            _auth.Register(user, password);
            Console.WriteLine("\n[SUCCESS] Account created! Redirecting to Dashboard...");
            Thread.Sleep(1000);
            return true;
        }


        private void DashboardPage()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(@$"Welcome, {_auth.CurrentUser!.FirstName}    
[C] Cart   [X] LogOut
==================================================");

                foreach (var group in _products.GetAll().GroupBy(p => p.Category))
                {
                    var table = new Table()
                        .Title($"[bold yellow]---------- {group.Key.ToUpper()} ----------[/]")
                        .Border(TableBorder.Rounded)
                        .AddColumn(new TableColumn("[blue]ID[/]").Centered())
                        .AddColumn(new TableColumn("[green]Product Name[/]"))
                        .AddColumn(new TableColumn("[magenta]Price[/]").RightAligned());
                    foreach (var p in group)
                    {
                        table.AddRow(p.Id.ToString(), p.Name, $"Rs. {p.Price}/-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine();
                }

                Console.WriteLine("--------------------------------------------------");
                DrawStatusView();

                Console.Write("\n Choose an option or type Product ID: ");
                var firstKey = Console.ReadKey(intercept: false);

                switch (firstKey.Key)
                {
                    case ConsoleKey.X:
                        _auth.Logout();
                        _cart.Clear();
                        return;

                    case ConsoleKey.C:
                        Logger.Info("Cart opened");
                        CartPage();
                        break;

                    default:
                        var restOfInput = Console.ReadLine() ?? "";
                        string input = (firstKey.KeyChar + restOfInput).Trim();
                        ProcessProductSelection(input);
                        break;
                }
            }
        }

        private void DrawStatusView()
        {
            Console.WriteLine("\n=== LIVE HUB ===");
            var logs = Logger.RecentLog();
            for (var i = 0; i < Math.Min(3, logs.Length); i++)
            {
                Console.WriteLine($"  > {Trim(logs[i], 45)}");
            }

            Console.WriteLine($@"
CART: 
Items: {_cart.Count} Total: Rs. {_cart.Total}");
            if (!_cart.IsEmpty)
            {
                foreach (var item in _cart.Items)
                {
                    Console.WriteLine($"-> {item.Product.Id} ({item.Product.Name}) * {item.Quantity}");
                }
            }
        }

        private void CartPage()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("================== YOUR CART ==================");

                if (_cart.IsEmpty)
                {
                    Console.WriteLine("\n[[ Your cart is empty ]]\n");
                }
                else
                {
                    Console.WriteLine($"{"ID"} {"Product"} {"Qty"} {"Total"}");
                    foreach (var i in _cart.Items)
                    {
                        Console.WriteLine($"{i.Product.Id} {i.Product.Name} {i.Quantity} {i.Total}");
                    }
                    Console.WriteLine($@"
---------------------------------------------
Grand Total: Rs {_cart.Total}");
                }

                Console.WriteLine("==============================================");
                Console.Write(@"[E] Edit Qty   [R] Remove   
[K] Checkout   [B] Back

Choice: ");
                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.E:
                        EditOrRemove(remove: false);
                        break;

                    case ConsoleKey.R:
                        EditOrRemove(remove: true);
                        break;

                    case ConsoleKey.K:
                        if (_cart.IsEmpty)
                        {
                            Logger.Warn("Cart is empty. Add products first.");
                            break;
                        }
                        if (CheckoutPage())
                        {
                            return;
                        }
                        break;

                    case ConsoleKey.B:
                        return;

                    default:
                        Logger.Warn("Invalid option choice.");
                        break;
                }
            }
        }

        private bool CheckoutPage()
        {
            var user = _auth.CurrentUser!;
            Console.Clear();
            Console.WriteLine($@"=========== CHECKOUT ===========
Customer : {user.FullName} (UserName - {user.Username})
Items    : {_cart.Count}
Total    : Rs. {_cart.Total}/-
--------------------------------");
            Console.Write(" Confirm payment? (Y/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "Y")
            {
                Logger.Info("Checkout cancelled by user");
                return false;
            }


            Logger.Info($"Payment of Rs. {_cart.Total}/- successful (PAID)");
            Console.WriteLine("\n[SUCCESS] Payment successful!!!");

            var invoice = _invoices.TopLayer(user, _cart.Items.ToList());
            PrintInvoice(invoice);
            try
            {
                var path = _invoices.CreatePdf(invoice);
                Logger.Info($"Invoice PDF saved: {path}");
                Console.WriteLine($"\n[SUCCESS] Invoice saved to: {path}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Invoice PDF wasn't generated due to : {ex.Message}");
                Logger.Error(ex.ToString());
                Console.WriteLine("\n[ERROR] Payment done, but the PDF could not be created.");
            }

            _cart.Clear();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            return true;
        }

        private void PrintInvoice(Invoice invoice)
        {
            Console.WriteLine(@"==================================
INVOICE {invoice.Number}
Date: {invoice.Date:dd/MM/yyyy}  Time: {invoice.Date:HH:mm:ss}
User: {invoice.Customer.Username}
Name: {invoice.Customer.FullName}
==================================");
            foreach (var i in invoice.Items)
            {
                Console.WriteLine($"Id: {i.Product.Id} Name: {i.Product.Name} Quantity: {i.Quantity} * Product price:{i.Product.Price} = {i.Total}");
            }
            Console.WriteLine($@"
-----------------------------------
TOTAL: Rs {invoice.Total} PAID
-----------------------------------");
        }

        private void EditOrRemove(bool remove)
        {
            if (_cart.IsEmpty)
            {
                Logger.Warn("Cart is empty.");
                return;
            }

            Console.Write(" Enter Product ID: ");
            var id = (Console.ReadLine() ?? "").Trim();
            var product = _products.GetById(id);
            if (product == null || !_cart.Contains(product.Id))
            {
                Logger.Warn($"Product Id- '{id}' is not in your cart");
                return;
            }

            var qty = remove ? 0 : ReadInt(" New quantity (0 = remove): ", 0, CartService.MaxQty);
            _cart.SetQuantity(product, qty);
        }

        private void ProcessProductSelection(string productId)
        {
            var product = _products.GetById(productId);
            if (product == null)
            {
                Logger.Warn($"Invalid product ID '{productId}'");
                return;
            }

            if (_cart.Contains(product.Id))
            {
                var current = _cart.GetQty(product.Id);
                var qty = ReadInt($"\n Already in cart ({current}). New quantity (0 = remove): ", 0, CartService.MaxQty);
                _cart.SetQuantity(product, qty);
            }
            else
            {
                var qty = ReadInt($"\n Quantity for {product.Name} (1-{CartService.MaxQty}): ", 1, CartService.MaxQty);
                _cart.SetQuantity(product, qty);
            }
        }


        private static string ReadValid(string prompt, Func<string, string?> validate)
        {
            while (true)
            {
                Console.Write(prompt);
                var value = (Console.ReadLine() ?? "").Trim();
                var error = validate(value);
                if (error == null) return value;
                Console.WriteLine($"[ERROR] {error}");
            }
        }
        private static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out var n) && n >= min && n <= max) return n;
                Console.WriteLine($"[ERROR] Enter a number between {min} and {max}.");
            }
        }

        private static string ReadPassword()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }

        private static string Trim(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }

            if (maxLength <= 3)
            {
                return "...";
            }

            string truncatedText = input[..(maxLength - 3)];
            return truncatedText + "...";
        }
    }
}
