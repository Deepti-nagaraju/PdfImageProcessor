using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PdfImageProcessor.Dtos;
using PdfImageProcessor.Helpers;
using PdfImageProcessor.Models;

namespace PdfImageProcessor.Controllers
{
    [Authorize]
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AuthHelper _authHelper;

        public AuthController(IConfiguration config)
        {
            _authHelper = new AuthHelper(config);
        }

        // ✅ Route to Login Page (View)
        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();  // Ensure you have a corresponding "Login.cshtml" view
        }

        // ✅ User Registration Endpoint
        [AllowAnonymous]
        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser([FromBody] RegisterUserDto registerUserDto)
        {
            if (registerUserDto.Password != registerUserDto.PasswordConfirm)
            {
                return StatusCode(400, "Passwords do not match!");
            }

            string usersFilePath = "Users.txt";
            if (!System.IO.File.Exists(usersFilePath))
            {
                System.IO.File.WriteAllText(usersFilePath, "");  // Create empty file if missing
            }

            string usersJson = System.IO.File.ReadAllText(usersFilePath);
            bool userFound = false;

            if (!string.IsNullOrEmpty(usersJson))
            {
                usersJson = "[" + usersJson + "]";
                var users = JsonConvert.DeserializeObject<List<User>>(usersJson) ?? new List<User>();
                userFound = users.Any(u => u.UserName == registerUserDto.UserName);
            }

            if (userFound)
            {
                return StatusCode(409, "User already exists!");
            }

            // ✅ Create password hash and salt
            byte[] passwordSalt = new byte[128 / 8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(passwordSalt);
            }

            byte[] passwordHash = _authHelper.GetPasswordHash(registerUserDto.Password, passwordSalt);

            User newUser = new()
            {
                UserName = registerUserDto.UserName,
                PasswordHash = Convert.ToBase64String(passwordHash),
                PasswordSalt = Convert.ToBase64String(passwordSalt),
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName,
                Gender = registerUserDto.Gender,
                Active = true
            };

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string userString = JsonConvert.SerializeObject(newUser, settings);

            using StreamWriter openFile = new(usersFilePath, append: true);
            openFile.WriteLine(",\n" + userString);
            openFile.Close();

            return Ok("User created successfully!");
        }

        // ✅ Login API (Authenticate User)
        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            string usersFilePath = "Users.txt";
            if (!System.IO.File.Exists(usersFilePath))
            {
                return StatusCode(404, "No users found!");
            }

            string usersJson = System.IO.File.ReadAllText(usersFilePath);
            if (string.IsNullOrEmpty(usersJson))
            {
                return StatusCode(404, "No users found!");
            }

            usersJson = "[" + usersJson + "]";
            var users = JsonConvert.DeserializeObject<List<User>>(usersJson);
            if (users == null || !users.Any())
            {
                return StatusCode(404, "No users found!");
            }

            var userDetails = users.FirstOrDefault(u => u.UserName == loginDto.UserName);

            if (userDetails == null)
            {
                return StatusCode(404, "Username not found!");
            }

            byte[] passwordHash = _authHelper.GetPasswordHash(loginDto.Password, Convert.FromBase64String(userDetails.PasswordSalt));
            byte[] userPasswordHash = Convert.FromBase64String(userDetails.PasswordHash);

            if (!passwordHash.SequenceEqual(userPasswordHash))
            {
                return StatusCode(401, "Incorrect password!");
            }

            string token = _authHelper.CreateJwtToken(userDetails.UserName);
            return Ok(new { token });
        }

        // ✅ Logout API (Optional)
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            return Ok("User logged out successfully.");
        }
    }
}
