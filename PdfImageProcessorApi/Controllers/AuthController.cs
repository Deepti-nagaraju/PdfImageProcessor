using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PdfImageProcessor.Dtos;
using PdfImageProcessor.Helpers;
using PdfImageProcessor.Models;

namespace PdfImageProcessorApi.Controllers // ✅ Updated namespace
{
    [EnableCors("AllowAllOrigins")]
    [Authorize]
    [ApiController]
    [Route("api/auth")] // ✅ Now under api route
    public class AuthController : ControllerBase // ✅ Changed to ControllerBase for API only
    {
        private readonly AuthHelper _authHelper;

        public AuthController(IConfiguration config)
        {
            _authHelper = new AuthHelper(config);
        }

        [AllowAnonymous]
        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser([FromBody] RegisterUserDto registerUserDto)
        {
            /**
             * Validate if Email is proper email or not. It should contain @ and .
             * if not, return BadRequest("Provide a valid Email!");
             */

            if (registerUserDto.Password != registerUserDto.PasswordConfirm)
            {
                return BadRequest("Passwords do not match!");
            }

            bool userFound = false;
            /**
             * Check if user already exist or not
             * SELECT Email FROM Auth WHERE Email = '" + registerUserDto.Email + "'";
             * 
             * if found then, userFound = true;
             */

            if (userFound)
            {
                return Conflict("User already exists!");
            }

            byte[] passwordSalt = new byte[128 / 8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(passwordSalt);
            }

            byte[] passwordHash = _authHelper.GetPasswordHash(registerUserDto.Password, passwordSalt);


            /**
             *  bool authInsertSuccess = Insert into Auth (Email,PasswordHash,PasswordSalt);
             *  if(authInsertSuccess){
             *      bool userInsertSuccess = Insert into User (FirstName,LastName,Email,Gender,Active=1);
             *      if(!userInsertSuccess){
             *          delete from Auth where Email = Email;
             *          return Problem("Failed to register user");
             *      }
             *  }
             * 
             */

            return Ok("User created successfully!");
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
        public IActionResult Login([FromBody] LoginDto loginDto)
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

            var userDetails = users.FirstOrDefault(u => u.UserName == loginDto.UserName);

            if (userDetails == null)
            {
                return NotFound("Username not found!");
            }

            byte[] passwordHash = _authHelper.GetPasswordHash(loginDto.Password, Convert.FromBase64String(userDetails.PasswordSalt));
            byte[] userPasswordHash = Convert.FromBase64String(userDetails.PasswordHash);

            if (!passwordHash.SequenceEqual(userPasswordHash))
            {
                return Unauthorized("Incorrect password!");
            }

            string token = _authHelper.CreateJwtToken(userDetails.UserName);
            return Ok(new { token });
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            return Ok("User logged out successfully.");
        }
    }
}