using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RednakoSharp.Helpers
{
    public static class General
    {
        public static string Proper(string unformatted)
        {
            if(unformatted == null)
            {
                throw new ArgumentException(paramName: nameof(unformatted), message: "Missing string to turn proper");
            }
            return char.ToUpper(unformatted[0], CultureInfo.InvariantCulture) + unformatted[1..];
        }
    }
}
