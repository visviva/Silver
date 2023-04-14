using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Time : ICallable
    {
        public int Arity() 
        {
            return 0;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            return (double)DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        string ICallable.ToString()
        {
            return $"<fn time>";
        }
    }
}
