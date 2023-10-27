using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using aspnet_blog_application.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string enteredPasswordHash = HashPassword(model.Password);

                using (var connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT id, name FROM user WHERE name = @Username AND password = @PasswordHash";
                        command.Parameters.AddWithValue("@Username", model.Name);
                        command.Parameters.AddWithValue("@PasswordHash", enteredPasswordHash);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Create claims for the authenticated user
                                var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, reader["id"].ToString()),
                            new Claim(ClaimTypes.Name, reader["name"].ToString())
                            // Add additional claims if needed
                        };

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                                var authProperties = new AuthenticationProperties
                                {
                                    // Customize authentication properties if needed
                                };

                                // Sign in the user and create the authentication cookie
                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);

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

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user and remove the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
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
