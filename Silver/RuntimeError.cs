using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    public class RuntimeErrorException : Exception
    {
        public readonly Token token;

        public RuntimeErrorException(Token token, string message) : base(message)
        {
            this.token = token;
        }
    }
}
