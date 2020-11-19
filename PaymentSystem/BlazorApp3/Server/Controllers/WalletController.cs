﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BlazorApp3.Server.Data;
using BlazorApp3.Server.Helpers;
using BlazorApp3.Server.Models;
using BlazorApp3.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wallet = BlazorApp3.Server.Models.Wallet;

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

        [HttpGet("{id}")]
        public Wallet GetWallet(Guid id)
        {
            var userId = userManager.GetUserId(User);
            var wallet = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId).Wallets.FirstOrDefault(x => x.Id == id);
            return wallet;
        }

        [HttpPost]
        public IActionResult CreateWallet([FromQuery] string currency)
        {
            if (CurrencyManager.Currencies.Contains(currency))
            {
                return BadRequest();
            }

            var userId = userManager.GetUserId(User);

            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (user.Wallets.Any(x => x.Currency == currency))
            {
                return BadRequest();
            }

            var wallet = new Wallet
            {
                Amount = 0,
                Currency = currency
            };

            if (user.Wallets == null)
            {
                user.Wallets = new List<Wallet>();
            }

            user.Wallets.Add(wallet);

            context.SaveChanges();

            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteWallet([FromRoute] Guid id)
        {
            var userId = userManager.GetUserId(User);
            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (!user.Wallets.Any(x => x.Id == id))
            {
                return BadRequest();
            }

            var wallet = context.Wallets.Find(id);
            context.Wallets.Remove(wallet);
            context.SaveChanges();

            return Ok();
        }

        [HttpPost("transfer")]
        public ActionResult MakeTransfer([FromBody] TransferDto data)
        {
            var userId = userManager.GetUserId(User);
            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (!user.Wallets.Any(x => x.Currency == data.Currency))
            {
                return BadRequest();
            }

            var source = user.Wallets.FirstOrDefault(x => x.Currency == data.Currency);

            if (source.Amount < data.Amount)
            {
                return BadRequest();
            }

            var destinationUser = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.UserName == data.Username);

            var destination = destinationUser.Wallets.FirstOrDefault(x => x.Currency == data.Currency);

            if (destination == null)
            {
                destination = new Wallet
                {
                    Amount = 0,
                    Currency = data.Currency
                };

                destinationUser.Wallets.Add(destination);
            }

            source.Amount -= data.Amount;
            destination.Amount += data.Amount;

            var transaction = new Transaction
            {
                Amount = data.Amount,
                Date = DateTime.Now,
                DestinationWalletId = destination.Id,
                SourceWalletId = source.Id
            };
            context.Add(transaction);

            context.SaveChanges();

            return Ok();
        }

        [HttpGet]
        [Route("transfers/{itemsPerPage}/{pageNumber}")]
        public TransactionsHistoryData GetTransactions(int itemsPerPage, int pageNumber, [FromQuery] Direction direction)
        {
            var userId = userManager.GetUserId(User);

            var walletIds = context.Wallets.Where(w => w.ApplicationUserId == userId).Select(w => w.Id).ToList();

            IQueryable<Transaction> queryForCount;
            Transaction[] transactions;

            switch (direction)
            {
                case Direction.Inbound:
                    queryForCount = context.Transactions.Where(t => walletIds.Contains(t.SourceWalletId));
                    transactions = queryForCount.OrderByDescending(x => x.Date)
                        .Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage).ToArray();
                    break;

                case Direction.Outbound:
                    queryForCount = context.Transactions.Where(t => walletIds.Contains(t.DestinationWalletId));
                    transactions = queryForCount.OrderByDescending(x => x.Date)
                        .Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage).ToArray();
                    break;
                case Direction.None:
                default:
                    queryForCount = context.Transactions.Where(t =>
                        walletIds.Contains(t.DestinationWalletId) || walletIds.Contains(t.SourceWalletId));
                    transactions = queryForCount.OrderByDescending(x => x.Date)
                        .Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage).ToArray();
                    break;
            }

            var transactionsData = new TransactionsHistoryData
            {
                Transactions = transactions.Select(DomainMapper.ToDto).ToArray(),
                ItemCount = queryForCount.Count()
            };

            return transactionsData;
        }
    }
}
