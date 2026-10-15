using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingApplication.Model
{
    public class User
    {
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public DateTime Dob { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string FullName
        {
            get
            {
                return string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";
            }
        }
    }
}
