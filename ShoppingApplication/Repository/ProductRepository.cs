using ShoppingApplication.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShoppingApplication.Repository
{
    internal class ProductRepository
    {
        private readonly string _path = Path.Combine(AppContext.BaseDirectory, "ShoppingAppData", "products.json");
        private readonly List<Product> _products;

        public ProductRepository()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (!File.Exists(_path))
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(Seed(), Formatting.Indented));
            }

            _products = JsonConvert.DeserializeObject<List<Product>>(File.ReadAllText(_path)) ?? new();
        }

        public List<Product> GetAll()
        {
            return _products;
        }

        public Product? GetById(string id)
        {
            return _products.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        private static List<Product> Seed() => new()
        {
        new() { Id = "G101", Name = "Game Controller",    Category = "Gaming",      Price = 2499 },
        new() { Id = "G102", Name = "Gaming Headset",     Category = "Gaming",      Price = 1999 },
        new() { Id = "G103", Name = "Gaming Mouse",       Category = "Gaming",      Price = 1299 },
        new() { Id = "E201", Name = "Bluetooth Speaker",  Category = "Electronics", Price = 1799 },
        new() { Id = "E202", Name = "Power Bank 10000",   Category = "Electronics", Price = 999 },
        new() { Id = "E203", Name = "Smart Watch",        Category = "Electronics", Price = 3499 },
        new() { Id = "C301", Name = "T-Shirt",            Category = "Clothing",    Price = 499 },
        new() { Id = "C302", Name = "Hoodie",             Category = "Clothing",    Price = 1299 },
        new() { Id = "C303", Name = "Jeans",              Category = "Clothing",    Price = 1599 },
        };
    }
}
