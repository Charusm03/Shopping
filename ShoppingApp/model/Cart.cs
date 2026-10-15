using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingApplication.Model
{
    public class CartItem
    {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal Total
        {
            get
            {
                return Product.Price * Quantity;
            }
        }
    }
}
