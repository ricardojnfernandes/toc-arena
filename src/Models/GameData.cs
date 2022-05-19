using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace toc_arena.Models
{
    public class GameData
    {
        public bool Running { get; set; }

        public Dictionary<ChampionEnum, int> Standings { get; set; }
    }
}
