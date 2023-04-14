using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Instance
    {
        private readonly Class Klass;
        private readonly Dictionary<string, object> Fields = new();

        public Instance(Class klass)
        {
            Klass = klass;
        }

        public override string ToString()
        {
            return $"{Klass} instance";
        }

        internal object Get(Token name)
        {
            if (Fields.ContainsKey(name.lexeme))
            {
                return Fields[name.lexeme];
            }

            var method = Klass.FindMethod(name.lexeme);
            if (method is Function function)
            {
                return function.Bind(this);
            }

            throw new RuntimeErrorException(name, $"Undefined property '{name.lexeme}'!");
        }

        internal void Set(Token name, object? value)
        {
            Fields.Add(name.lexeme, value!);
        }
    }
}
