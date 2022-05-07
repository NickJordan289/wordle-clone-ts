using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleChat.Models
{
    public enum ActionDefinition : int
    {
        Default = 0,
        Join = 1,
        Leave = 2,
        Create = 3,
        Guess = 4
    }
}
