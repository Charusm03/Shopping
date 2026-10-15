using ShoppingApplication.Repository;
using ShoppingApplication.Service;
using ShoppingApplication.View;

namespace ShoppingApplication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Last-resort crash logging (also covers errors that escape the try/catch below)
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Logger.Error($"CRASH: {e.ExceptionObject}");

            try
            {
                UserRepository userRepository = new UserRepository();
                AuthService authService = new AuthService(userRepository);
                CartService cartService = new CartService();
                ProductRepository productRepository = new ProductRepository();
                InvoiceRepository invoiceRepository = new InvoiceRepository();
                InvoiceService invoiceService = new InvoiceService(invoiceRepository);
                UserView userView = new UserView(authService, cartService, productRepository, invoiceService);
                userView.RunApplicationLoop();
                Console.WriteLine("Thank you for your visit...");
            }
            catch (Exception ex)
            {
                Logger.Error($"CRASH: {ex}");
                Console.WriteLine("\nSomething went wrong and the application has to close. Details are saved in the log file.");
            }
        }
    }
}
