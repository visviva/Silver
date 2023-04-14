using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Function : ICallable
    {
        private readonly Statement.Function Declaration;
        private readonly Environment Closure;
        private readonly bool IsInitializer;

        public Function(Statement.Function function, Environment closure, bool isInitializer)
        {
            Declaration = function;
            Closure = closure;
            IsInitializer = isInitializer;
        }

        public int Arity()
        {
            return Declaration.Parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            var environment = new Environment(Closure);

            for (int i = 0; i < Declaration.Parameters.Count; i++)
            {
                environment.Define(Declaration.Parameters[i].lexeme, arguments[i]);
            }

            try
            {
                interpreter.ExecuteBlock(Declaration.Body, environment);
            }
            catch (ReturnException returnValue)
            {
                if (IsInitializer)
                {
                    return Closure.GetAt(0, "this");
                }

                return returnValue.Value;
            }

            if (IsInitializer)
            {
                return Closure.GetAt(0, "this");
            }

            return null!;
        }

        public Function Bind(Instance instance)
        {
            var environment = new Environment(Closure);
            environment.Define("this", instance);
            return new Function(Declaration, environment, IsInitializer);
        }

        string ICallable.ToString()
        {
            return $"<fn {Declaration.Name.lexeme}>";
        }
    }
}
