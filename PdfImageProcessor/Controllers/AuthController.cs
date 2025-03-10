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
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthHelper _authHelper;
        public AuthController(IConfiguration config)
        {
            _authHelper = new AuthHelper(config);
        }


        [AllowAnonymous]            //Allows request without authentication(JWT token)
        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser(RegisterUserDto registerUserDto)
        {
            //1. Check password mismatch
            if (registerUserDto.Password == registerUserDto.PasswordConfirm)
            {
                // 2. Check if user already exist or not
                bool userFound = false;
                string usersJson = System.IO.File.ReadAllText("Users.txt");
                if (usersJson != null)
                {
                    usersJson = "[" + usersJson + "]";
                    IEnumerable<User>? users = JsonConvert.DeserializeObject<IEnumerable<User>>(usersJson);
                    if (users != null)
                    {
                        foreach (User user in users)
                        {
                            if (user.UserName == registerUserDto.UserName)
                            {
                                userFound = true;
                            }
                        }
                    }
                }

                if (!userFound)
                {
                    Console.WriteLine("Creating user..");

                    // 3. Generate a password salt to create a passwordHash
                    byte[] passwordSalt = new byte[128 / 8];
                    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    {
                        rng.GetNonZeroBytes(passwordSalt);
                    }

                    // 4. Create a passwordHash
                    byte[] passwordHash = _authHelper.GetPasswordHash(registerUserDto.Password, passwordSalt);

                    // 5. Store hashed key, salt and other user details in db
                    User user = new()
                    {
                        UserName = registerUserDto.UserName,
                        PasswordHash = Convert.ToBase64String(passwordHash),
                        PasswordSalt = Convert.ToBase64String(passwordSalt),
                        FirstName = registerUserDto.FirstName,
                        LastName = registerUserDto.LastName,
                        Gender = registerUserDto.Gender,
                        Active = true
                    };

                    JsonSerializerSettings settings = new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    string userString = JsonConvert.SerializeObject(user, settings);

                    using StreamWriter openFile = new("Users.txt", append: true);
                    openFile.WriteLine(",\n" + userString);
                    openFile.Close();

                    return Ok("User created!");
                }
                else
                {
                    Console.WriteLine("User already exists!");
                    return StatusCode(401, "User already exists!");
                }
            }
            Console.WriteLine("Passwords do not match!");
            return StatusCode(401, "Passwords do not match!");
        }



        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(LoginDto loginDto)
        {
            //Get user details
            User userDetails = null;
            string usersJson = System.IO.File.ReadAllText("Users.txt");
            if (usersJson != null)
            {
                usersJson = "[" + usersJson + "]";
                IEnumerable<User>? users = JsonConvert.DeserializeObject<IEnumerable<User>>(usersJson);
                if (users != null)
                {
                    foreach (User user in users)
                    {
                        if (user.UserName == loginDto.UserName)
                        {
                            userDetails = new()
                            {
                                UserName = user.UserName,
                                PasswordHash = user.PasswordHash,
                                PasswordSalt = user.PasswordSalt
                            };
                        }
                    }
                }
            }

            if (userDetails != null)
            {
                byte[] passwordHash = _authHelper.GetPasswordHash(loginDto.Password, Convert.FromBase64String(userDetails.PasswordSalt));
                byte[] userPasswordHash = Convert.FromBase64String(userDetails.PasswordHash);
                for (int index = 0; index < passwordHash.Length; index++)
                {
                    if (passwordHash[index] != userPasswordHash[index])
                    {
                        return StatusCode(401, "Incorrect password!");
                    }
                }

                return Ok(new Dictionary<string, string> {
                    {"token", _authHelper.CreateJwtToken(userDetails.UserName)}
                });
            }
            else
            {
                return StatusCode(401, "Username not found!");
            }
        }


        /*[HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.User.FindFirstValue("userId");
            return Ok();
        }*/

    }
}