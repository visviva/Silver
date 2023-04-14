using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Class : ICallable
    {
        public string Name;
        private readonly Dictionary<string, Function> Methods;
        private readonly Class? Superclass;

        public Class(string name, Class? superclass, Dictionary<string, Function> methods)
        {
            Name = name;
            Methods = methods;
            Superclass = superclass;
        }

        public int Arity()
        {
            if (FindMethod("init") is not Function initializer)
            {
                return 0;
            }

            return initializer.Arity();
        }

        public object? Call(Interpreter interpreter, List<object> arguments)
        {
            var instance = new Instance(this);

            Function? initializer = FindMethod("init") as Function;
            initializer?.Bind(instance).Call(interpreter, arguments);

            return instance;
        }

        public override string ToString()
        {
            return Name;
        }

        internal object? FindMethod(string lexeme)
        {
            if(Methods.ContainsKey(lexeme))
            {
                return Methods[lexeme];
            }

            if(Superclass != null)
            {
                return Superclass.FindMethod(lexeme);
            }

            return null;
        }
    }
}
