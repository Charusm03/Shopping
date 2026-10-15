using ShoppingApplication.Model;
using ShoppingApplication.Repository;
using ShoppingApplication.service;
using ShoppingApplication.Service;
using Spectre.Console;
using System.Globalization;
using System.Text;

namespace ShoppingApplication.View
{
    internal class UserView
    {
        private readonly AuthService _auth;
        private readonly CartService _cart;
        private readonly ProductRepository _products;
        private readonly InvoiceService _invoices;
        private const int MaxAttempts = 3;
        private const int NoticeLines = 3;   // notifications shown on the dashboard
        private const int MaxCartLines = 4;  // cart items listed in the live cart before "...and N more"

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
                ClearScreen();
                Console.Write(@"Welcome to the Shopping App!
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
                        Console.WriteLine(" Invalid choice. Please enter 1, 2 or 3.");
                        Console.WriteLine(" Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                }
            }
        }

        // Clears the screen AND the scroll-back, so only one copy of the current page exists
        private static void ClearScreen()
        {
            Console.Clear();
            if (AnsiConsole.Profile.Capabilities.Ansi)
            {
                Console.Write("\u001b[3J");
            }
        }

        private bool LoginPage()
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                ClearScreen();
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
            ClearScreen();
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
                ClearScreen();
                Console.WriteLine($"Welcome, {_auth.CurrentUser!.FirstName}");
                AnsiConsole.MarkupLine("[bold white on blue] [[C]] CART [/]   [bold white on red] [[X]] LOGOUT [/]");
                Console.WriteLine("==================================================");

                DrawProducts();
                DrawNotificationsAndCart();

                AnsiConsole.Write(new Rule("[bold yellow]YOUR CHOICE[/]").LeftJustified());
                Console.Write("Enter Product ID to add / edit quantity (or C = Cart, X = Logout): ");
                var input = (Console.ReadLine() ?? "").Trim();

                switch (input.ToUpper())
                {
                    case "":
                        break;

                    case "X":
                        _auth.Logout();
                        _cart.Clear();
                        Notifier.Clear();
                        return;

                    case "C":
                        Logger.Info("Cart opened");
                        CartPage();
                        break;

                    default:
                        ProcessProductSelection(input);
                        break;
                }
            }
        }

        private void DrawProducts()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[blue]Category[/]")
                .AddColumn(new TableColumn("[green]ID[/]").Centered())
                .AddColumn("[green]Product Name[/]")
                .AddColumn(new TableColumn("[green]Price[/]").RightAligned());

            foreach (var group in _products.GetAll().GroupBy(p => p.Category))
            {
                var firstRow = true;
                foreach (var p in group)
                {
                    table.AddRow(
                        firstRow ? $"[bold red]{Markup.Escape(group.Key.ToUpper())}[/]" : "",
                        Markup.Escape(p.Id.ToString()),
                        Markup.Escape(p.Name),
                        $"Rs. {p.Price:N0}/-");
                    firstRow = false;
                }
            }
            AnsiConsole.Write(table);
        }

        private void DrawNotificationsAndCart()
        {
            var notices = Notifier.Latest(NoticeLines)
                .Select(n => $"[{ColorOf(n.Level)}]> {Markup.Escape(Trim(n.Message, 48))}[/]")
                .ToList();
            if (notices.Count == 0)
            {
                notices.Add("[grey]> No notifications yet[/]");
            }

            var items = _cart.Items.ToList();
            var cartLines = items
                .Take(MaxCartLines)
                .Select(i => $"{i.Product.Id} {Trim(i.Product.Name, 18)} x{i.Quantity}  Rs. {i.Total:N0}")
                .ToList();
            if (items.Count == 0)
            {
                cartLines.Add("Cart is empty");
            }
            else if (items.Count > MaxCartLines)
            {
                cartLines.Add($"...and {items.Count - MaxCartLines} more item(s)");
            }
            cartLines.Add($"Items: {_cart.Count}   Total: Rs. {_cart.Total:N0}/-");

            var panel = new Table()
                .Border(TableBorder.Rounded)
                .Expand()
                .AddColumn("[bold cyan]NOTIFICATIONS[/]")
                .AddColumn("[bold green]LIVE CART[/]");

            // Markup for the notices (escaped above); Text for the cart so names are never read as styling
            panel.AddRow(new Markup(string.Join("\n", notices)), new Text(string.Join("\n", cartLines)));
            AnsiConsole.Write(panel);
        }

        private static string ColorOf(NoticeLevel level) => level switch
        {
            NoticeLevel.Success => "green",
            NoticeLevel.Warning => "yellow",
            _ => "red"
        };

        private void CartPage()
        {
            var mark = Notifier.Mark; // on this page only messages raised here are shown

            while (true)
            {
                ClearScreen();
                Console.WriteLine("================== YOUR CART ==================");

                if (_cart.IsEmpty)
                {
                    Console.WriteLine("\n[ Your cart is empty ]\n");
                }
                else
                {
                    Console.WriteLine($"{"ID"} {"Product"} {"Qty"} {"Total"}");
                    foreach (var i in _cart.Items)
                    {
                        Console.WriteLine($"{i.Product.Id} {i.Product.Name} {i.Quantity} {i.Total:N0}");
                    }
                    Console.WriteLine($@"
---------------------------------------------
Grand Total: Rs {_cart.Total:N0}");
                }

                Console.WriteLine("==============================================");

                var latest = Notifier.LastAfter(mark);
                if (latest != null)
                {
                    AnsiConsole.MarkupLine($"[{ColorOf(latest.Level)}]> {Markup.Escape(latest.Message)}[/]");
                }

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
                            Logger.Warn("Checkout attempted with an empty cart");
                            Notifier.Warning("Your cart is empty. Add products first.");
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
                        Notifier.Warning("Please choose E, R, K or B.");
                        break;
                }
            }
        }

        private bool CheckoutPage()
        {
            var user = _auth.CurrentUser!;
            ClearScreen();
            Console.WriteLine($@"=========== CHECKOUT ===========
Customer : {user.FullName} (UserName - {user.Username})
Items    : {_cart.Count}
Total    : Rs. {_cart.Total:N0}/-
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
                Notifier.Success($"Payment successful. Invoice {invoice.Number} saved.");
            }
            catch (Exception ex)
            {
                // Technical details go to the log file only; the customer gets a plain message
                Logger.Error($"Invoice PDF wasn't generated due to : {ex.Message}");
                Logger.Error(ex.ToString());
                Console.WriteLine("\n[ERROR] Payment done, but the PDF could not be created.");
                Notifier.Error("Payment done, but the invoice PDF could not be created.");
            }

            _cart.Clear();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            return true;
        }

        private void PrintInvoice(Invoice invoice)
        {
            // $ before @ so {invoice.Number} etc. are filled in instead of printed as text
            Console.WriteLine($@"==================================
INVOICE {invoice.Number}
Date: {invoice.Date:dd/MM/yyyy}  Time: {invoice.Date:HH:mm:ss}
User: {invoice.Customer.Username}
Name: {invoice.Customer.FullName}
==================================");
            foreach (var i in invoice.Items)
            {
                Console.WriteLine($"Id: {i.Product.Id} Name: {i.Product.Name} Quantity: {i.Quantity} * Product price:{i.Product.Price:N0} = {i.Total:N0}");
            }
            Console.WriteLine($@"
-----------------------------------
TOTAL: Rs {invoice.Total:N0} PAID
-----------------------------------");
        }

        private void EditOrRemove(bool remove)
        {
            if (_cart.IsEmpty)
            {
                Logger.Warn("Edit/remove attempted with an empty cart");
                Notifier.Warning("Your cart is empty.");
                return;
            }

            Console.Write(" Enter Product ID: ");
            var id = (Console.ReadLine() ?? "").Trim();
            var product = _products.GetById(id);
            if (product == null || !_cart.Contains(product.Id))
            {
                Logger.Warn($"Product Id- '{id}' is not in your cart");
                Notifier.Warning($"'{id}' is not in your cart.");
                return;
            }

            var qty = remove ? 0 : ReadInt(" New quantity (0 = remove): ", 0, CartService.MaxQty);
            ApplyQuantity(product, qty, alreadyInCart: true);
        }

        private void ProcessProductSelection(string productId)
        {
            var product = _products.GetById(productId);
            if (product == null)
            {
                Logger.Warn($"Invalid product ID '{productId}'");
                Notifier.Warning($"Product ID '{productId}' was not found.");
                return;
            }

            if (_cart.Contains(product.Id))
            {
                var current = _cart.GetQty(product.Id);
                var qty = ReadInt($"Already in cart ({current}). New quantity (0 = remove): ", 0, CartService.MaxQty);
                ApplyQuantity(product, qty, alreadyInCart: true);
            }
            else
            {
                var qty = ReadInt($"Quantity for {product.Name} (1-{CartService.MaxQty}): ", 1, CartService.MaxQty);
                ApplyQuantity(product, qty, alreadyInCart: false);
            }
        }

        // The cart service writes its own log entry; this adds the customer-facing message
        private void ApplyQuantity(Product product, int qty, bool alreadyInCart)
        {
            _cart.SetQuantity(product, qty);

            if (qty == 0)
            {
                Notifier.Success($"{product.Name} removed from cart");
            }
            else if (alreadyInCart)
            {
                Notifier.Success($"{product.Name} quantity updated to {qty}");
            }
            else
            {
                Notifier.Success($"{product.Name} x{qty} added to cart");
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