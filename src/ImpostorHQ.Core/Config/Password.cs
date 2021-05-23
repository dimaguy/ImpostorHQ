using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpostorHQ.Core.Config
{
    public readonly struct Password
    {
        public string Key { get; }

        public string User { get; }

        private readonly string _str;

        public Password(string key, string user)
        {
            this.Key = key;
            this.User = user;
            this._str = $"{user}:{key}";
        }

        public override string ToString()
        {
            return _str;
        }
    }
}
