using SimpleChat.Models;
using System;
using System.Collections.Generic;

namespace WordleMultiplayer.Models
{
    public class Game
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string TargetWord { get; set; }
        public List<GuessRecord> Guesses { get; set; }
    }
}
