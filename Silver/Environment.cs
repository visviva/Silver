using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Environment
    {
        public readonly Environment? Enclosing;
        private readonly Dictionary<string, object> Values = new();

        public Environment()
        {
            Enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.Enclosing = enclosing;
        }

        public void Define(string name, object value)
        {
            if(Values.ContainsKey(name))
            {
                Values[name] = value;
                return;
            }

            Values.Add(name, value);
        }

        public object Get(Token name)
        {
            if (Values.ContainsKey(name.lexeme))
            {
                return Values[name.lexeme];
            }

            if (Enclosing != null)
            {
                return Enclosing.Get(name);
            }

            throw new RuntimeErrorException(name, $"Undefined variable '{name.lexeme}'.");
        }

        public void Assign(Token name, object? value)
        {
            if (Values.ContainsKey(name.lexeme))
            {
                Values[name.lexeme] = value!;
                return;
            }

            if (Enclosing != null)
            {
                Enclosing.Assign(name, value);
                return;
            }

            throw new RuntimeErrorException(name, $"Undefined variable '{name.lexeme}'!");
        }

        internal object GetAt(int distanceValue, string name)
        {
            var env = (Environment)Ancestor(distanceValue);
            return env.Values[name];
        }

        private object Ancestor(int distanceValue)
        {
            var environment = this;

            for(int i =0; i<distanceValue; i++)
            {
                environment = environment!.Enclosing;
            }

            return environment!;
        }

        internal void AssignAt(int distance, Token name, object? value)
        {
            var env = (Environment)Ancestor(distance);
            env.Values.Add(name.lexeme, value!);
        }
    }
}
