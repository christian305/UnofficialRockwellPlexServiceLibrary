using System;
using System.Collections.Generic;
using System.Text;

namespace RockwellPlexServiceLibrary.Utils
{
    public class Authenticator
    {
        public string Username { get; }
        public string Password { get; }

        public Authenticator(string Username, string Password) {
            this.Username = Username;
            this.Password = Password;
        }

    }
}


