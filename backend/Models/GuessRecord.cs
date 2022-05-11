using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleChat.Models
{
    public class GuessRecord
    {
        public string Word { get; set; }
        public List<int> Score { get; set; }
        public bool Winner { get; set; } = false;
    }
}
