using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    interface ICallable
    {
        object? Call(Interpreter interpreter, List<object> arguments);
        int Arity();

        public string ToString();
    }
}
