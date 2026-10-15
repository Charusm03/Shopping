using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingApplication.Repository
{
    public class InvoiceRepository
    {
        public string OutputDir { get; } = Path.Combine(AppContext.BaseDirectory, "Output");

        public string PathFor(string invoiceNo)
        {
            Directory.CreateDirectory(OutputDir);
            return Path.Combine(OutputDir, $"{invoiceNo}.pdf");
        }
    }
}
