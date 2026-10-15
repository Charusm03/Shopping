using ShoppingApplication.Repository;

namespace ShoppingApplication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UserRepository userRepository = new UserRepository();
            AuthService authService = new AuthService(userRepository);
            CartService cartService = new CartService();
            ProductRepository productRepository = new ProductRepository();
            InvoiceRepository invoiceRepository = new InvoiceRepository();
            InvoiceService invoiceService = new InvoiceService(invoiceRepository);
            AuthService userService = new AuthService(userRepository);
            UserView userView = new UserView(authService, cartService, productRepository, invoiceService);
            userView.RunApplicationLoop();
            Console.WriteLine("Thank you for your visit...");
        }
    }
}
