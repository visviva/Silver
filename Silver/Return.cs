using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class ReturnException : RuntimeErrorException
    {
        public readonly object Value;

        public ReturnException(object value) : base(null!, null!)
        {         
            this.Value = value;
        }
    }
}
