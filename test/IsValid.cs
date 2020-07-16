using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HallScript
{
    public static class IsValid
    {
        public static string[] keywords =
        {
            "true",
            "false"
        };
        public static bool Identifier(string name)
        {
            if (name.Length == 0) return false;
            if (keywords.Contains(name)) return false;
            if (name[0] <= '9' && name[0] >= '0') return false;
            foreach(char c in name)
            {
                if (!((c <= '9' && c >= 0) || (c <= 'Z' && c >= 'A') || (c <= 'z' && c >= 'a') || "@#$%_".Contains(c)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
