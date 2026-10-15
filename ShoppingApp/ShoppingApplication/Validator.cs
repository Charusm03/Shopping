using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShoppingApplication
{
    public static class Validator
    {
        public static string? FirstName(string firstName)
        {
            if (Regex.IsMatch(firstName, @"^[A-Za-z ]{1,20}$"))
            {
                return null;
            }
            else
            {
                return "[First name] Needs letters only and Maximum of 20 characters only]";
            }
        }

        public static string? LastName(string v)
        {
            if (v.Length == 0 || Regex.IsMatch(v, @"^[A-Za-z ]{1,20}$"))
            {
                return null;
            }
            else
            {
                return "[Last name] Needs letters only and maximum of 20 characters only.";
            }
        }
        public static string? Dob(string doB)
        {
            if (!DateTime.TryParseExact(doB, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
            {
                return "Use format DD/MM/YYYY.";
            }
            if (dob > DateTime.Today)
            {
                return "DOB cannot be in the future.";
            }
            if (dob.Year < 1800)
            {
                return "Enter a valid year.";
            }
            return null;
        }

        public static string? Phone(string phoneNumber)
        {
            if (Regex.IsMatch(phoneNumber, @"^[6-9]\d{9}$"))
            {
                return null;
            }
            else
            {
                return "Enter a 10-digit valid phone number starting with 6-9.";
            }
        }
        public static string? Username(string username)
        {
            if (Regex.IsMatch(username, @"^[A-Za-z0-9_]{4,15}$"))
            {
                return null;
            }

            return "Username must be between 4 and 15 characters, containing only letters, digits, or underscores.";
        }

        public static string? Password(string password)
        {
            if (password.Length < 8)
            {
                return "Password must have atleast 8 characters.";
            }
            if (!password.Any(char.IsUpper))
            {
                return "Password must have atleast 1 uppercase letter.";
            }
            if (!password.Any(char.IsLower))
            {
                return "Password must have atleast 1 lowercase letter.";
            }
            if (!password.Any(char.IsDigit))
            {
                return "Password must have atleast 1 number.";
            }
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                return "Password must have atleast 1 special character.";
            }
            return null;
        }

        // ---------- Address ----------
        public static string? Street(string street)
        {
            if (street.Length >= 5 && street.Length <= 60)
            {
                return null;
            }
            return "Street / house details must be 5 to 60 characters.";
        }

        public static string? City(string city)
        {
            if (Regex.IsMatch(city, @"^[A-Za-z ]{2,30}$"))
            {
                return null;
            }
            return "City needs letters only (2 to 30 characters).";
        }

        public static string? Pincode(string pincode)
        {
            if (Regex.IsMatch(pincode, @"^[1-9]\d{5}$"))
            {
                return null;
            }
            return "Enter a valid 6-digit pincode.";
        }
    }
}
