using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BlazorApp3.Server.Data;
using BlazorApp3.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public WalletController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        [HttpGet]
        public List<Wallet> GetWallets()
        {
            var userId = userManager.GetUserId(User);
            var wallets = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId).Wallets;
            return wallets;
        }

        [HttpPost]
        public IActionResult CreateWallet([FromQuery] string currency)
        {
            var userId = userManager.GetUserId(User);

            var wallet = new Wallet
            {
                Amount = 0,
                Currency = currency
            };

            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (user.Wallets == null)
            {
                user.Wallets = new List<Wallet>();
            }

            user.Wallets.Add(wallet);

            context.SaveChanges();

            return Ok();
        }
        [HttpDelete("delete/{id}")]
        public async IActionResult DeleteWallet (int id)
        {
            var _wallet = await context.Wallets.FindAsync(id);
            if (_wallet == null)
            {
                return NotFound();
            }
            context.Wallets.Remove(_wallet);
            await context.SaveChangesAsync();

            return _wallet;
        }
        /*
        private bool If_Wallet_Exists(int id)
        {
            return context.Wallets.Any(e => e.Id == id);
        }*/
    }
}
