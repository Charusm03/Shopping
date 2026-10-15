using ShoppingApplication.Model;
using ShoppingApplication.Repository;
using ShoppingApplication.Service;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ShoppingApplication
{
    internal class AuthService
    {
        public const int MaxAddresses = 5; // saved addresses allowed per user

        private readonly UserRepository _repo;
        public User? CurrentUser { get; private set; }

        public AuthService(UserRepository repo)
        {
            _repo = repo;
        }

        public bool UserNameExists(string username)
        {
            return _repo.Exists(username);
        }

        internal bool Login(string username, string password)
        {
            var user = _repo.GetUser(username);
            if (user == null || user.PasswordHash != Hash(password))
            {
                Logger.Warn($"Failed login - '{username}'");
                return false;
            }
            CurrentUser = user;
            Logger.Info($"User - {user.Username} logged in.");
            return true;
        }

        public void Logout()
        {
            if (CurrentUser != null)
            {
                Logger.Info($"User - {CurrentUser.Username} logged out.");
            }
            CurrentUser = null;
        }

        internal void Register(User newUser, string password)
        {
            newUser.PasswordHash = Hash(password);
            _repo.RegisterUser(newUser);
            CurrentUser = newUser;
            Logger.Info($"New user - {newUser.Username} registered.");
        }

        // ---------- Password reset ----------

        // Step 1: the person must match username + registered phone + date of birth
        internal bool VerifyIdentity(string username, string phone, string dobText)
        {
            var user = _repo.GetUser(username);
            var dobOk = DateTime.TryParseExact(dobText, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dob);

            if (user == null || !dobOk || user.Phone != phone || user.Dob.Date != dob.Date)
            {
                Logger.Warn($"Password reset verification failed - '{username}'");
                return false;
            }
            return true;
        }

        internal bool IsCurrentPassword(string username, string password)
        {
            var user = _repo.GetUser(username);
            return user != null && user.PasswordHash == Hash(password);
        }

        // Step 2: only call after VerifyIdentity succeeded
        internal bool ResetPassword(string username, string newPassword)
        {
            var user = _repo.GetUser(username);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = Hash(newPassword);
            _repo.UpdateUser(user);
            Logger.Info($"Password reset - {username}");
            return true;
        }

        // ---------- Addresses ----------

        // Adds the address to the logged-in user and saves it
        internal bool AddAddress(Address address)
        {
            if (CurrentUser == null || CurrentUser.Addresses.Count >= MaxAddresses)
            {
                return false;
            }

            CurrentUser.Addresses.Add(address);
            _repo.UpdateUser(CurrentUser);
            Logger.Info($"Address added - {CurrentUser.Username} now has {CurrentUser.Addresses.Count} address(es).");
            return true;
        }

        private string Hash(string password)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        }
    }
}
