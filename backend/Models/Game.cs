using SimpleChat.Models;
using System.Collections.Generic;

namespace WordleMultiplayer.Models
{
    public class Game
    {
        public string Name { get; set; }
        public string TargetWord { get; set; }
        public string Description { get; set; }
        public List<GuessRecord> Guesses { get; set; }
    }
}
