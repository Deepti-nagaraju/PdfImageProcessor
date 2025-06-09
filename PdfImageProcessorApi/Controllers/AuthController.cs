using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PdfImageProcessor.Dtos;
using PdfImageProcessor.Helpers;
using PdfImageProcessor.Models;
using PdfImageProcessorApi.Models;

namespace PdfImageProcessorApi.Controllers // ✅ Updated namespace
{
    [EnableCors("AllowAllOrigins")]
    [Authorize]
    [ApiController]
    [Route("api/auth")] // ✅ Now under api route
    public class AuthController : ControllerBase // ✅ Changed to ControllerBase for API only
    {
        private readonly AuthHelper _authHelper;
        private readonly InvoiceDbContext _context;

        public AuthController(IConfiguration config, InvoiceDbContext context)
        {
            _authHelper = new AuthHelper(config);
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser([FromBody] RegisterUserDto registerUserDto)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(registerUserDto.UserName) ||
                !registerUserDto.UserName.Contains("@") ||
                !registerUserDto.UserName.Contains("."))
            {
                return BadRequest("Provide a valid Email!");
            }

            // Confirm password
            if (registerUserDto.Password != registerUserDto.PasswordConfirm)
            {
                return BadRequest("Passwords do not match!");
            }

            // Check if user already exists
            bool exists = _context.Auth.Any(a => a.Email == registerUserDto.UserName);
            if (exists)
            {
                return Conflict("User already exists!");
            }

            // Generate salt
            byte[] passwordSalt = new byte[128 / 8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(passwordSalt);
            }

            // Generate hash
            byte[] passwordHash = _authHelper.GetPasswordHash(registerUserDto.Password, passwordSalt);

            // Insert into Auth
            var auth = new Auth
            {
                Email = registerUserDto.UserName,
                PasswordSalt = Convert.ToBase64String(passwordSalt),
                PasswordHash = Convert.ToBase64String(passwordHash)
            };


            try
            {
                _context.Auth.Add(auth);
                int authInsertSuccess = _context.SaveChanges();

                if (authInsertSuccess > 0)
                {
                    // Step 2: Insert into User
                    var user = new User
                    {
                        FirstName = registerUserDto.FirstName,
                        LastName = registerUserDto.LastName,
                        UserName = registerUserDto.UserName,
                        Gender = registerUserDto.Gender,
                        PasswordSalt = Convert.ToBase64String(passwordSalt),
                        PasswordHash = Convert.ToBase64String(passwordHash),

                        Active = true
                    };

                    _context.Users.Add(user);
                    int userInsertSuccess = _context.SaveChanges();

                    if (userInsertSuccess == 0)
                    {
                        // Rollback Auth insert
                        var toDelete = _context.Auth.FirstOrDefault(a => a.Email == registerUserDto.UserName);
                        if (toDelete != null)
                        {
                            _context.Auth.Remove(toDelete);
                            _context.SaveChanges();
                        }

                        return Problem("Failed to register user in User table.");
                    }

                    return Ok("User registered successfully!");
                }

                return Problem("Failed to register user in Auth table.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Database error: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }


        //[AllowAnonymous]
        //[HttpPost("Login")]
        //public IActionResult Login([FromBody] LoginDto loginDto)
        //{
        //    Auth authDetails = null;
        //    /**
        //     * Query Auth table by Email.. 
        //     * authDetails = select * from Auth where Email = loginDto.Email
        //     * if(authDetails == null){
        //     *      return NotFound("User not found!");
        //     * }
        //     * 
        //     */

        //    byte[] passwordHash = _authHelper.GetPasswordHash(loginDto.Password, authDetails.PasswordSalt);

        //    for (int index = 0; index < passwordHash.Length; index++)
        //    {
        //        if (passwordHash[index] != authDetails.PasswordHash[index])
        //        {
        //            return StatusCode(401, "Incorrect password!");
        //        }
        //    }

        //    int userId = 0;
        //    /**
        //     * Get userId from User table
        //     * userId = select * from User where Email = loginDto.Email;
        //     * 
        //     */

        //    string token = _authHelper.CreateJwtToken(userId.ToString());
        //    return Ok(new { token });
        //}
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            string dataDirectory = Path.Combine(Environment.CurrentDirectory, "App_Data");
            Directory.CreateDirectory(dataDirectory);
            string usersFilePath = Path.Combine(dataDirectory, "Users.txt");
            //string usersFilePath = "Users.txt";
            if (!System.IO.File.Exists(usersFilePath))
            {
                return NotFound("No users found!");
            }

            string usersJson = System.IO.File.ReadAllText(usersFilePath);
            if (string.IsNullOrEmpty(usersJson))
            {
                return NotFound("No users found!");
            }

            usersJson = "[" + usersJson + "]";
            var users = JsonConvert.DeserializeObject<List<User>>(usersJson);
            if (users == null || !users.Any())
            {
                return NotFound("No users found!");



            }
            var authDetails = _context.Auth.FirstOrDefault(a => a.Email == loginDto.UserName);
            if (authDetails == null)
            {
                return NotFound("User not found!");
            }

            // Step 2: Hash the input password with stored salt
            byte[] passwordSalt = Convert.FromBase64String(authDetails.PasswordSalt);
            byte[] inputPasswordHash = _authHelper.GetPasswordHash(loginDto.Password, passwordSalt);
            byte[] storedPasswordHash = Convert.FromBase64String(authDetails.PasswordHash);

            // Step 3: Compare hashes
            if (!inputPasswordHash.SequenceEqual(storedPasswordHash))
            {
                return Unauthorized("Incorrect password!");
            }

            // Step 4: Lookup user (for userId or claims)
            var user = _context.Users.FirstOrDefault(u => u.UserName == loginDto.UserName);
            if (user == null)
            {
                return NotFound("User record not found in User table.");
            }

            // Step 5: Create JWT token
            string token = _authHelper.CreateJwtToken(user.Id.ToString()); // Or user.UserName
            return Ok(new { token });
        }
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            return Ok("User logged out successfully.");
        }
    }
}