using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpostorHQ.Core.Extensions
{
    public static class StringExtensions
    {
        public static string[] Tokenize(this string str, char separator, char selector)
        {
            var tokens = new List<string>();
            var currentToken = string.Empty;
            var selecting = false;

            foreach (var @char in str)
            {
                if (@char == selector)
                {
                    // invert selection state
                    selecting = !selecting;
                    continue;
                }

                if (@char == separator)
                {
                    if (!selecting)
                    {
                        tokens.Add(currentToken);
                        currentToken = string.Empty;
                    }
                    else
                    {
                        currentToken += @char;
                    }
                }
                else
                {
                    currentToken += @char;
                }
            }

            tokens.Add(currentToken);
            return tokens.ToArray();
        }
    }
}
