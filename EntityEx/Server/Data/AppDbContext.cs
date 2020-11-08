using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityEx.Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Actor> Actors { get; set; }
        //constructor, primeste optiunile de la bazele de date
        public AppDbContext(DbContextOptions opts) :base(opts)
        {
            
        }
    }
}
