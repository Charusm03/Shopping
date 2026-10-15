using ShoppingApplication.Model;
using ShoppingApplication.Service;
using System;
using System.Security.Cryptography;
using System.Text;
using ShoppingApplication.Repository;

namespace ShoppingApplication.Service
{
    internal class AuthService
    {
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

        private string Hash(string password)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        }
    }
}
