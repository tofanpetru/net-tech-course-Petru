using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityEx.Server.Data
{//Pentru doar un film. reprezinta doar un obiect in acest caz
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Actor> Actors { get; set; }
    }
}
