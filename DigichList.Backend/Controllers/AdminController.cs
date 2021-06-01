﻿using DigichList.Backend.Options;
using DigichList.Backend.ViewModel;
using DigichList.Core.Entities;
using DigichList.Core.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DigichList.Backend.Controllers
{
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepositury _repo;
        private readonly IOptions<AuthOptions> authOptions;

        public AdminController(IAdminRepositury repo, IOptions<AuthOptions> authOptions)
        {
            _repo = repo;
            this.authOptions = authOptions;
        }
        [HttpGet]
        [Route("api/[controller]")]
        public async Task<IActionResult> GetAdmins()
        {
            return Ok(await _repo.GetAllAsync());
        }

        [HttpGet]
        [Route("api/[controller]/{id}")]
        public async Task<IActionResult> GetAdmin(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user != null)
            {
                return Ok(user);
            }
            return NotFound($"admin whith id: {id} was not found");
        }

        [HttpPost]
        [Route("api/[controller]")]
        public async Task<IActionResult> CreateAdmin(Admin admin)
        {
            await _repo.AddAsync(admin);
            return Created(HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.Path + "/" + admin.Id, admin);
        }

        [HttpDelete]
        [Route("api/[controller]/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _repo.GetByIdAsync(id);
            if (admin != null)
            {
                await _repo.DeleteAsync(admin);
                return Ok();
            }
            return NotFound($"admin whith id: {id} was not found");
        }

        [HttpPost]
        [Route("api/[controller]/UpdateAdmin")]
        public async Task<IActionResult> UpdatePost([FromBody] Admin admin)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _repo.UpdateAsync(admin);

                    return Ok();
                }
                catch (Exception ex)
                {
                    if (ex.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException")
                    {
                        return NotFound();
                    }

                    return BadRequest();
                }
            }
            return BadRequest();
        }

        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] Login request)
        {
            var admin = await AuthenticateUser(request.Email, request.Password);
            if (admin != null)
            {
                var token = GenerateJWT(admin);
                return Ok(new
                {
                    acceess_token = token
                });
            }
            return Unauthorized();

        }

        private async Task<Admin> AuthenticateUser(string email, string password)
        {
            return await _repo.GetAdminByEmailAndPassword(email, password);
        }

        private string GenerateJWT(Admin admin)
        {
            var authParams = authOptions.Value;
            var securityKey = authParams.GetSymmetricSecurutyKey();
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Email, admin.Email),
                new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new Claim("role", admin.AccessLevel.ToString())
            };

            var token = new JwtSecurityToken(authParams.Issuer,
                authParams.Audience,
                claims,
                expires: DateTime.Now.AddSeconds(authParams.TokenLifetime),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}