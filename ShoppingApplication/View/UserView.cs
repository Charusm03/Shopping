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
        private const int NoticeLines = 3;   // notifications shown on the dashboard
        private const int MaxCartLines = 4;  // cart items listed in the live cart before "...and N more"

        // Home screen can only ADD 1-5 units per entry. Removing / changing quantity is done in the cart only.
        private const int MinAddQty = 1, MaxAddQty = 5;

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

        // Clears the screen AND the scroll-back, so only one copy of the current page exists
        private static void ClearScreen()
        {
            Console.Clear();
            if (AnsiConsole.Profile.Capabilities.Ansi)
            {
                Console.Write("\u001b[3J");
            }
        }

        // ================= LOGIN =================
        // 3 attempts, but the person is never trapped: B = back, S = sign up, F = forgot password
        // (usernames are at least 4 characters, so these single letters can never be a real username)
        private bool LoginPage()
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                ClearScreen();
                Console.WriteLine("============ LOGIN ============");
                AnsiConsole.MarkupLine("[grey]B = Back to menu   S = New user? Sign Up   F = Forgot password[/]");
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
                    // Reset does not use up a login attempt; after a successful reset the attempts start again
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

                // Same message whether the username or the password was wrong (do not reveal which)
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

        // ================= RESET PASSWORD =================
        // Returns true when the password was changed
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

        // ================= SIGN UP =================
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

            // Optional: any number of addresses (up to the limit), each with its own phone number
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

        // ================= DASHBOARD =================
        // Everything is drawn once, top to bottom, with no cursor jumping:
        //   1. welcome + coloured CART / LOGOUT bar
        //   2. one product table (all categories)
        //   3. notifications (left)  |  live cart (right)
        //   4. YOUR CHOICE section with the prompt (always the last thing on screen)
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
                Console.Write("Enter Product ID to add (or C = Cart, X = Logout): ");
                var input = (Console.ReadLine() ?? "").Trim();

                // Typed + Enter (not a single key) so product IDs like C301 are not mistaken for the Cart key
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
                        Logger.Info("Cart opened");   // log only - not something the customer needs to be told
                        CartPage();
                        break;

                    default:
                        ProcessProductSelection(input);
                        break;
                }
            }
        }

        // One compact table; the category is shown on the first row of each group
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

        // Left: customer notifications (NOT the technical log).  Right: live cart.
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

        // ================= CART =================
        // The only place where quantities can be changed or products removed
        private void CartPage()
        {
            var mark = Notifier.Mark; // the notification area on this page shows only messages raised here

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
                DrawCartNotifications(mark);

                Console.Write(@"
[A] Add   [E] Edit Qty   [R] Remove
[K] Checkout   [B] Back

Choice: ");
                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.A:
                        AddFromCart();
                        break;

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
                        Notifier.Warning("Please choose A, E, R, K or B.");
                        break;
                }
            }
        }

        // Notification area of the cart page (same style as the dashboard)
        private void DrawCartNotifications(int mark)
        {
            var lines = Notifier.Since(mark, NoticeLines)
                .Select(n => $"[{ColorOf(n.Level)}]> {Markup.Escape(n.Message)}[/]")
                .ToList();

            if (lines.Count == 0 && _cart.IsEmpty)
            {
                lines.Add("[yellow]> Your cart is empty. Press A to add a product or B to go back.[/]");
            }
            if (lines.Count == 0)
            {
                lines.Add("[grey]> No new notifications[/]");
            }

            AnsiConsole.Write(new Panel(new Markup(string.Join("\n", lines)))
                .Header("[bold cyan] NOTIFICATIONS [/]")
                .Border(BoxBorder.Rounded)
                .Expand());
        }

        private void AddFromCart()
        {
            Console.Write("\nEnter Product ID to add: ");
            var id = (Console.ReadLine() ?? "").Trim();
            var product = _products.GetById(id);
            if (product == null)
            {
                Logger.Warn($"Invalid product ID '{id}'");
                Notifier.Warning($"Product ID '{id}' was not found.");
                return;
            }

            var room = CartService.MaxQty - _cart.GetQty(product.Id);
            if (room < 1)
            {
                Notifier.Warning($"{product.Name}: maximum quantity ({CartService.MaxQty}) is already in the cart.");
                return;
            }

            var qty = ReadInt($"Quantity to add (1-{room}): ", 1, room);
            AddQuantity(product, qty);
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

        // ================= HOME SCREEN: ADD ONLY =================
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

        // The cart service writes its own log entry; these add the customer-facing message
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

        // ================= CHECKOUT =================
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
                // Technical details go to the log file only; the customer gets a plain message
                Logger.Error($"Invoice PDF wasn't generated due to : {ex.Message}");
                Logger.Error(ex.ToString());
                Console.WriteLine("\n[ERROR] Payment done, but the PDF could not be created.");
                Notifier.Error("Payment done, but the invoice PDF could not be created.");
            }

            _cart.Clear();
            Pause();
            return true;
        }

        // Pick a saved address, or add a new one (it is saved on the user and used for this order).
        // Returns null if the customer goes back.
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
            // $ before @ so {invoice.Number} etc. are filled in instead of printed as text
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

        // ================= HELPERS =================
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
