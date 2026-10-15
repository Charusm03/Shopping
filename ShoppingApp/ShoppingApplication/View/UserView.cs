using ShoppingApplication.Model;
using ShoppingApplication.Repository;
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
        private const int NoticeLines = 3;
        private const int MaxCartLines = 4;

        private const int MinAddQty = 1, MaxAddQty = 5;

        private const int MinCols = 70, MinRows = 30;

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
3. Reset Password
4. Exit
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
                        ResetPasswordPage();
                        break;
                    case "4":
                        Logger.Info("Application exited");
                        Console.WriteLine(" Happy shopping!");
                        return;
                    default:
                        Logger.Warn("Invalid menu choice");
                        Console.WriteLine(" Invalid choice. Please enter 1, 2, 3 or 4.");
                        Pause();
                        break;
                }
            }
        }

        private static void ClearScreen()
        {
            Console.Clear();
            if (AnsiConsole.Profile.Capabilities.Ansi)
            {
                Console.Write("\u001b[3J");
            }
        }

        private static string Line(char c) =>
            new string(c, Math.Max(10, Math.Min(50, Console.WindowWidth - 1)));

        private bool LoginPage()
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                ClearScreen();
                Console.WriteLine("============ LOGIN ============");
                AnsiConsole.MarkupLine("[grey]B = Back to menu   S = If New user then Sign Up   F = Forgot password[/]");
                Console.Write("Username : ");
                var username = (Console.ReadLine() ?? "").Trim();

                if (username.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (username.Equals("S", StringComparison.OrdinalIgnoreCase))
                {
                    return SignUpPage();
                }
                if (username.Equals("F", StringComparison.OrdinalIgnoreCase))
                {
                    attempt = ResetPasswordPage() ? 0 : attempt - 1;
                    continue;
                }

                Console.Write("Password : ");
                var password = ReadPassword();

                if (_auth.Login(username, password))
                {
                    Console.WriteLine("\n[SUCCESS] Login successful!");
                    Thread.Sleep(1000);
                    return true;
                }

                Console.WriteLine($"\n[ERROR] ({attempt}/{MaxAttempts}). Invalid username or password");
                if (attempt < MaxAttempts)
                {
                    Console.WriteLine("Tip: type F as the username if you forgot your password, or S to sign up.");
                }
                Pause();
            }

            Logger.Warn("Too many failed login attempts");
            ClearScreen();
            Console.WriteLine("[ERROR] Too many failed attempts. Returning to the main menu.");
            Pause();
            return false;
        }

        private bool ResetPasswordPage()
        {
            ClearScreen();
            Console.WriteLine("========== RESET PASSWORD ==========");
            AnsiConsole.MarkupLine("[grey]Confirm your details first. Type B as the username to cancel.[/]");

            Console.Write("Username             : ");
            var username = (Console.ReadLine() ?? "").Trim();
            if (username.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            Console.Write("Registered Phone     : ");
            var phone = (Console.ReadLine() ?? "").Trim();
            Console.Write("DOB (DD/MM/YYYY)     : ");
            var dob = (Console.ReadLine() ?? "").Trim();

            if (!_auth.VerifyIdentity(username, phone, dob))
            {
                Console.WriteLine("\n[ERROR] The details do not match our records. Password was not changed.");
                Pause();
                return false;
            }

            Console.WriteLine("\n[SUCCESS] Details verified.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Password: 8+ chars, 1 upper, 1 lower, 1 number, 1 special");
            Console.ResetColor();

            string newPassword;
            while (true)
            {
                Console.Write("New Password         : ");
                newPassword = ReadPassword();
                var formatValidation = Validator.Password(newPassword);
                if (formatValidation != null)
                {
                    Console.WriteLine($"[ERROR] {formatValidation}");
                    continue;
                }
                if (_auth.IsCurrentPassword(username, newPassword))
                {
                    Console.WriteLine("[ERROR] New password must be different from the old password.");
                    continue;
                }

                Console.Write("Confirm New Password : ");
                if (ReadPassword() == newPassword)
                {
                    break;
                }
                Console.WriteLine("[ERROR] Passwords do not match. Try again.");
            }

            _auth.ResetPassword(username, newPassword);
            Console.WriteLine("\n[SUCCESS] Password reset successful! Please login with your new password.");
            Pause();
            return true;
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

            var addresses = new List<Address>();
            Console.Write("\nAdd a delivery address now? (Y/N, optional): ");
            while ((Console.ReadLine() ?? "").Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                addresses.Add(ReadAddress());
                if (addresses.Count >= AuthService.MaxAddresses)
                {
                    Console.WriteLine($"You can save up to {AuthService.MaxAddresses} addresses.");
                    break;
                }
                Console.Write("\nAdd another address? (Y/N): ");
            }

            var user = new User
            {
                FirstName = first,
                LastName = last,
                Dob = DateTime.ParseExact(dobText, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                Phone = phone,
                Username = username,
                Addresses = addresses
            };
            _auth.Register(user, password);
            Console.WriteLine("\n[SUCCESS] Account created! Redirecting to Dashboard...");
            Thread.Sleep(1000);
            return true;
        }

        private static Address ReadAddress()
        {
            Console.WriteLine("\n--- Delivery Address ---");
            var street = ReadValid("Street / House No  : ", Validator.Street);
            var city = ReadValid("City               : ", Validator.City);
            var pincode = ReadValid("Pincode            : ", Validator.Pincode);
            var phone = ReadValid("Phone (this address): ", Validator.Phone);
            return new Address { Street = street, City = city, Pincode = pincode, Phone = phone };
        }

        private void DashboardPage()
        {
            while (true)
            {
                DrawDashboard();
                var input = ReadLineWithResize(DashboardPrompt, DrawDashboard).Trim();

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

        private void DrawDashboard()
        {
            ClearScreen();
            Console.WriteLine($"Welcome, {_auth.CurrentUser!.FirstName}");
            AnsiConsole.MarkupLine("[bold white on blue] [[C]] CART [/]   [bold white on red] [[X]] LOGOUT [/]");
            Console.WriteLine(Line('='));

            DrawProducts();
            DrawNotificationsAndCart();

            AnsiConsole.Write(new Rule("[bold yellow]YOUR CHOICE[/]").LeftJustified());
            if (Console.WindowWidth < MinCols || Console.WindowHeight < MinRows)
            {
                AnsiConsole.MarkupLine("[yellow]Window is small - make it larger to see the whole screen.[/]");
            }
        }

        private static string DashboardPrompt() =>
            Console.WindowWidth >= MinCols
                ? "Enter Product ID to add (or C = Cart, X = Logout): "
                : "Product ID (C=Cart, X=Logout): ";

        private void DrawProducts()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Category[/]")
                .AddColumn(new TableColumn("[blue]ID[/]").Centered())
                .AddColumn("[green]Product Name[/]")
                .AddColumn(new TableColumn("[magenta]Price[/]").RightAligned());

            foreach (var group in _products.GetAll().GroupBy(p => p.Category))
            {
                var firstRow = true;
                foreach (var p in group)
                {
                    table.AddRow(
                        firstRow ? $"[bold yellow]{Markup.Escape(group.Key.ToUpper())}[/]" : "",
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
            var columnWidth = Math.Max(20, (Console.WindowWidth - 9) / 2);

            var notices = Notifier.Latest(NoticeLines)
                .Select(n => $"[{ColorOf(n.Level)}]> {Markup.Escape(Trim(n.Message, columnWidth - 2))}[/]")
                .ToList();
            if (notices.Count == 0)
            {
                notices.Add("[grey]> No notifications yet[/]");
            }

            var items = _cart.Items.ToList();
            var cartLines = items
                .Take(MaxCartLines)
                .Select(i => Trim($"{i.Product.Id} {Trim(i.Product.Name, 18)} x{i.Quantity}  Rs. {i.Total:N0}", columnWidth))
                .ToList();
            if (items.Count == 0)
            {
                cartLines.Add("Cart is empty");
            }
            else if (items.Count > MaxCartLines)
            {
                cartLines.Add($"...and {items.Count - MaxCartLines} more item(s)");
            }
            cartLines.Add(Trim($"Items: {_cart.Count}   Total: Rs. {_cart.Total:N0}/-", columnWidth));

            var panel = new Table()
                .Border(TableBorder.Rounded)
                .Expand()
                .AddColumn("[bold cyan]NOTIFICATIONS[/]")
                .AddColumn("[bold green]LIVE CART[/]");

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
            var mark = Notifier.Mark; 

            while (true)
            {
                DrawCartPage(mark);
                var key = ReadKeyWithResize(() => DrawCartPage(mark));

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
                            Notifier.Warning("Your cart is empty. Add products before checkout.");
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

        private void DrawCartPage(int mark)
        {
            ClearScreen();
            AnsiConsole.Write(new Rule("[bold yellow]YOUR CART[/]").LeftJustified());

            if (_cart.IsEmpty)
            {
                AnsiConsole.MarkupLine("\n[grey][[ Your cart is empty ]][/]\n");
            }
            else
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[blue]ID[/]")
                    .AddColumn("[green]Product[/]")
                    .AddColumn(new TableColumn("[bold]Qty[/]").RightAligned())
                    .AddColumn(new TableColumn("[magenta]Total[/]").RightAligned());

                foreach (var i in _cart.Items)
                {
                    table.AddRow(
                        Markup.Escape(i.Product.Id),
                        Markup.Escape(i.Product.Name),
                        i.Quantity.ToString(),
                        $"Rs. {i.Total:N0}");
                }
                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"[bold]Grand Total: Rs. {_cart.Total:N0}/-[/]");
            }

            Console.WriteLine(Line('='));
            DrawCartNotifications(mark);

            Console.Write(@"
[A] Add   [E] Edit Qty   [R] Remove
[K] Checkout   [B] Back

Choice: ");
        }

        // Notification area of the cart page (same style as the dashboard)
        private void DrawCartNotifications(int mark)
        {
            var textWidth = Math.Max(20, Console.WindowWidth - 8);

            var lines = Notifier.Since(mark, NoticeLines)
                .Select(n => $"[{ColorOf(n.Level)}]> {Markup.Escape(Trim(n.Message, textWidth))}[/]")
                .ToList();

            if (lines.Count == 0 && _cart.IsEmpty)
            {
                lines.Add("[yellow]> Your cart is empty. Press A to add a product or B to go back.[/]");
            }
            if (lines.Count == 0)
            {
                lines.Add("[grey]> No new notifications[/]");
            }

            var panel = new Table()
    .Border(TableBorder.Rounded)
    .Expand()
    .AddColumn("[bold cyan]NOTIFICATIONS[/]");

            panel.AddRow(new Markup(string.Join("\n", lines)));

            AnsiConsole.Write(panel);
        }

        private void EditOrRemove(bool remove)
        {
            if (_cart.IsEmpty)
            {
                Logger.Warn("Edit/remove attempted with an empty cart");
                Notifier.Warning(remove
                    ? "Your cart is empty. There is nothing to remove."
                    : "Your cart is empty. There is nothing to edit.");
                return;
            }

            Console.Write("\nEnter Product ID: ");
            var id = (Console.ReadLine() ?? "").Trim();
            var product = _products.GetById(id);
            if (product == null || !_cart.Contains(product.Id))
            {
                Logger.Warn($"Product Id- '{id}' is not in your cart");
                Notifier.Warning($"'{id}' is not in your cart.");
                return;
            }

            var qty = remove ? 0 : ReadInt("New quantity (0 = remove): ", 0, CartService.MaxQty);
            ChangeQuantity(product, qty);
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

            var current = _cart.GetQty(product.Id);
            var room = Math.Min(MaxAddQty, CartService.MaxQty - current);
            if (room < MinAddQty)
            {
                Logger.Warn($"Max quantity already in cart for {product.Id}");
                Notifier.Warning($"{product.Name}: maximum quantity ({CartService.MaxQty}) already in cart. Change it from the cart.");
                return;
            }

            var prompt = current == 0
                ? $"Quantity for {product.Name} ({MinAddQty}-{room}): "
                : $"{product.Name} is already in cart (x{current}). Add how many more? ({MinAddQty}-{room}): ";

            var qty = ReadInt(prompt, MinAddQty, room);
            AddQuantity(product, qty);
        }

        private void AddQuantity(Product product, int qty)
        {
            var current = _cart.GetQty(product.Id);
            _cart.SetQuantity(product, current + qty);

            Notifier.Success(current == 0
                ? $"{product.Name} x{qty} added to cart"
                : $"{product.Name} +{qty} added (now x{current + qty})");
        }

        private void ChangeQuantity(Product product, int qty)
        {
            _cart.SetQuantity(product, qty);

            Notifier.Success(qty == 0
                ? $"{product.Name} removed from cart"
                : $"{product.Name} quantity changed to {qty}");
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

            var address = ChooseAddress();
            if (address == null)
            {
                Logger.Info("Checkout cancelled - no address chosen");
                return false;
            }

            Console.WriteLine($"\nDeliver to : {address}");
            Console.Write("Confirm payment? (Y/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "Y")
            {
                Logger.Info("Checkout cancelled by user");
                return false;
            }


            Logger.Info($"Payment of Rs. {_cart.Total}/- successful (PAID)");
            Console.WriteLine("\n[SUCCESS] Payment successful!!!");

            var invoice = _invoices.TopLayer(user, _cart.Items.ToList());
            invoice.DeliveryAddress = address;
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
                Logger.Error($"Invoice PDF wasn't generated due to : {ex.Message}");
                Logger.Error(ex.ToString());
                Console.WriteLine("\n[ERROR] Payment done, but the PDF could not be created.");
                Notifier.Error("Payment done, but the invoice PDF could not be created.");
            }

            _cart.Clear();
            Pause();
            return true;
        }

        private Address? ChooseAddress()
        {
            var user = _auth.CurrentUser!;
            while (true)
            {
                Console.WriteLine("\nDELIVERY ADDRESS");
                for (var i = 0; i < user.Addresses.Count; i++)
                {
                    Console.WriteLine($" {i + 1}. {user.Addresses[i]}");
                }

                Console.Write(user.Addresses.Count == 0
                    ? "No saved address yet. [A] Add address, [B] Back: "
                    : $"Choose 1-{user.Addresses.Count}, [A] Add new address, [B] Back: ");
                var input = (Console.ReadLine() ?? "").Trim();

                if (input.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                if (input.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    var added = AddNewAddress(user);
                    if (added != null)
                    {
                        return added;
                    }
                    continue;
                }
                if (int.TryParse(input, out var number) && number >= 1 && number <= user.Addresses.Count)
                {
                    return user.Addresses[number - 1];
                }
                Console.WriteLine("[ERROR] Invalid choice.");
            }
        }

        private Address? AddNewAddress(User user)
        {
            if (user.Addresses.Count >= AuthService.MaxAddresses)
            {
                Console.WriteLine($"[ERROR] You can save up to {AuthService.MaxAddresses} addresses. Please choose a saved one.");
                return null;
            }

            var address = ReadAddress();
            _auth.AddAddress(address);
            Console.WriteLine("[SUCCESS] Address saved to your account.");
            return address;
        }

        private void PrintInvoice(Invoice invoice)
        {
            Console.WriteLine($@"==================================
INVOICE {invoice.Number}
Date: {invoice.Date:dd/MM/yyyy}  Time: {invoice.Date:HH:mm:ss}
User: {invoice.Customer.Username}
Name: {invoice.Customer.FullName}
Deliver to: {invoice.DeliveryAddress}
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

        private static void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
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

        private static string ReadLineWithResize(Func<string> prompt, Action redraw)
        {
            var buffer = new StringBuilder();
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;
            Console.Write(prompt());

            while (true)
            {
                if (WindowResized(ref width, ref height))
                {
                    redraw();
                    Console.Write(prompt() + buffer);
                    continue;
                }

                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        private static ConsoleKey ReadKeyWithResize(Action redraw)
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            while (true)
            {
                if (WindowResized(ref width, ref height))
                {
                    redraw();
                    continue;
                }

                if (Console.KeyAvailable)
                {
                    return Console.ReadKey(intercept: true).Key;
                }
                Thread.Sleep(50);
            }
        }

        private static bool WindowResized(ref int width, ref int height)
        {
            if (Console.WindowWidth == width && Console.WindowHeight == height)
            {
                return false;
            }

            int w, h;
            do
            {
                w = Console.WindowWidth;
                h = Console.WindowHeight;
                Thread.Sleep(150);
            }
            while (w != Console.WindowWidth || h != Console.WindowHeight);

            width = w;
            height = h;
            return true;
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
