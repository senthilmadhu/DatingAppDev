using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using API.DTO;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        public DataContext _context { get; }
        private readonly ITokenservice _tokenservice;
        public AccountController(DataContext context, ITokenservice tokenservice)
        {
            _tokenservice = tokenservice;
            _context = context;
        }

      
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto regDto)
        {
            if(await UserExists(regDto.Username))
            return BadRequest("User already exists");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {

                Username = regDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(regDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return new UserDto
            {
                Username = user.Username,
                Token = _tokenservice.CreateToken(user)
            };
        }

        [HttpPost("login")]

        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
            .SingleOrDefaultAsync(x=> x.Username == loginDto.Username);

            if(user ==  null) return Unauthorized("Invalid Username");

            using var mac = new HMACSHA512(user.PasswordSalt);
            
            var computedHash = mac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i=0; i<computedHash.Length;i++)
            {
                if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Password not valid");
            }

             return new UserDto
            {
                Username = user.Username,
                Token = _tokenservice.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string UserName)
        {
           return await _context.Users.AnyAsync(x=>x.Username==UserName.ToLower());
        }
    }
}