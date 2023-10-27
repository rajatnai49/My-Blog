using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using aspnet_blog_application.Models.ViewModels;

namespace aspnet_blog_application.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Hash the password securely before storing it
                string passwordHash = HashPassword(model.Password);

                using (var connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO user (name, password) VALUES (@Username, @PasswordHash)";
                        command.Parameters.AddWithValue("@Username", model.Name);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        command.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Login");
            }
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string enteredPasswordHash = HashPassword(model.Password);

                using (var connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM user WHERE name = @Username AND password = @PasswordHash";
                        command.Parameters.AddWithValue("@Username", model.Name);
                        command.Parameters.AddWithValue("@PasswordHash", enteredPasswordHash);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return RedirectToAction("Index", "Home");
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                            }
                        }
                    }
                }
            }
            return View(model);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
