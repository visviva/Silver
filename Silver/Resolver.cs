using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silver
{
    internal class Resolver : Expression.IVisitor<object?>, Statement.IVisitor<object?>
    {
        private enum FunctionType
        {
            NONE,
            FUNCTION,
            METHOD,
            INITIALIZER
        }

        private enum ClassType
        {
            NONE,
            CLASS,
            SUBCLASS
        }

        private FunctionType CurrentFunction = FunctionType.NONE;
        private ClassType CurrentClass = ClassType.NONE;
        private readonly Interpreter Interpreter;
        private readonly Stack<Dictionary<string, bool>> Scopes = new();

        public Resolver(Interpreter interpreter)
        {
            Interpreter = interpreter;
        }

        public void Resolve(List<Statement> statements)
        {
            foreach (Statement s in statements)
            {
                Resolve(s);
            }
        }

        private void Resolve(Statement statement)
        {
            statement.Accept(this);
        }

        private void Resolve(Expression expression)
        {
            expression.Accept(this);
        }

        private void BeginScope()
        {
            Scopes.Push(new Dictionary<string, bool>());
        }

        private void EndScope()
        {
            Scopes.Pop();
        }

        private void Define(Token name)
        {
            if (Scopes.Count == 0)
            {
                return;
            }

            var scope = Scopes.Peek();
            scope[name.lexeme] = true;
        }

        private void Declare(Token name)
        {
            if (Scopes.Count == 0)
            {
                return;
            }

            var scope = Scopes.Peek();

            if (scope.ContainsKey(name.lexeme))
            {
                Program.Error(name, "Already a variable with this name in this scope!");
            }

            scope.Add(name.lexeme, false);
        }

        private void ResolveLocal(Expression expression, Token name)
        {
            foreach (var (scope, index) in Scopes.Select((value, index) => (value, index)))
            {
                if (scope.ContainsKey(name.lexeme))
                {
                    Interpreter.Resolve(expression, index);
                    return;
                }
            }
        }
        private void ResolveFunction(Statement.Function statement, FunctionType type)
        {
            FunctionType enclosingFunction = CurrentFunction;
            CurrentFunction = type;

            BeginScope();

            foreach (var param in statement.Parameters)
            {
                Declare(param);
                Define(param);
            }

            Resolve(statement.Body);

            EndScope();
            CurrentFunction = enclosingFunction;
        }

        public object? VisitAssignmentExpression(Expression.Assignment expression)
        {
            Resolve(expression.Value);
            ResolveLocal(expression, expression.Name);
            return null;
        }

        public object? VisitBinaryExpression(Expression.Binary expression)
        {
            Resolve(expression.Left);
            Resolve(expression.Right);
            return null;
        }

        public object? VisitBlockStatement(Statement.Block statement)
        {
            BeginScope();
            Resolve(statement.Statements);
            EndScope();
            return null;
        }


        public object? VisitCallExpression(Expression.Call expression)
        {
            Resolve(expression.Callee);

            foreach (var call in expression.Arguments)
            {
                Resolve(call);
            }

            return null;
        }


        public object? VisitExpressionStatement(Statement.Expression statement)
        {
            Resolve(statement.Expr); return null;
        }

        public object? VisitFunctionStatement(Statement.Function statement)
        {
            Declare(statement.Name);
            Define(statement.Name);

            ResolveFunction(statement, FunctionType.FUNCTION);
            return null;
        }


        public object? VisitGroupingExpression(Expression.Grouping expression)
        {
            Resolve(expression.Expr); return null;
        }

        public object? VisitIfStatement(Statement.If statement)
        {
            Resolve(statement.Condition);
            Resolve(statement.ThenBranch);
            if (statement.ElseBranch != null) { Resolve(statement.ElseBranch); }
            return null;
        }

        public object? VisitLiteralExpression(Expression.Literal expression)
        {
            return null;
        }

        public object? VisitLogicalExpression(Expression.Logical expression)
        {
            Resolve(expression.Left);
            Resolve(expression.Right);
            return null;
        }

        public object? VisitPrintStatement(Statement.Print statement)
        {
            Resolve(statement.Expr);
            return null;
        }

        public object? VisitReturnStatement(Statement.Return statement)
        {
            if (CurrentFunction == FunctionType.NONE)
            {
                Program.Error(statement.Keyword, "Cannot return from top-level code!");
            }

            if (statement.Value != null)
            {
                if (CurrentFunction == FunctionType.INITIALIZER)
                {
                    Program.Error(statement.Keyword, "Cannot return a value from an initializer!");
                }
                Resolve(statement.Value);
            }
            return null;
        }

        public object? VisitUnaryExpression(Expression.Unary expression)
        {
            Resolve(expression.Right);
            return null;
        }

        public object? VisitVariableExpression(Expression.Variable expression)
        {
            if(Scopes.Count != 0)
            {
                var doesExist = Scopes.Peek().TryGetValue(expression.Name.lexeme, out var initialized);
                if (doesExist && !initialized)
                {
                    Silver.Program.Error(expression.Name, "Cannot read local variable in its own initializer!");
                }
            }

            ResolveLocal(expression, expression.Name);
            return null;
        }


        public object? VisitVarStatement(Statement.Var statement)
        {
            Declare(statement.Name);

            if (statement.Initializer != null)
            {
                Resolve(statement.Initializer);
            }

            Define(statement.Name);
            return null;
        }


        public object? VisitWhileStatement(Statement.While statement)
        {
            Resolve(statement.Condition);
            Resolve(statement.Body);
            return null;
        }

        public object? VisitClassStatement(Statement.Class statement)
        {
            var enclosingClass = CurrentClass;
            CurrentClass = ClassType.CLASS;

            Declare(statement.Name);
            Define(statement.Name);

            if (statement.Superclass != null && (statement.Name.lexeme == statement.Superclass.Name.lexeme))
            {
                Program.Error(statement.Superclass.Name, "A class cannot inherit from itself!");
            }

            if (statement.Superclass != null)
            {
                CurrentClass = ClassType.SUBCLASS;
                Resolve(statement.Superclass);
            }

            if (statement.Superclass != null)
            {
                BeginScope();
                Scopes.Peek().Add("super", true);
            }

            BeginScope();
            Scopes.Peek().Add("this", true);

            foreach (var method in statement.Methods)
            {
                var declaration = FunctionType.METHOD;

                if (method.Name.lexeme == "init")
                {
                    declaration = FunctionType.INITIALIZER;
                }

                ResolveFunction(method, declaration);
            }

            EndScope();

            if (statement.Superclass != null)
            {
                EndScope();
            }

            CurrentClass = enclosingClass;

            return null;
        }

        public object? VisitGetExpression(Expression.Get expression)
        {
            Resolve(expression.Obj);
            return null;
        }

        public object? VisitSetExpression(Expression.Set expression)
        {
            Resolve(expression.Value);
            Resolve(expression.Obj);
            return null;
        }

        public object? VisitThisExpression(Expression.This expression)
        {
            if (CurrentClass == ClassType.NONE)
            {
                Program.Error(expression.Keyword, "Cannot use 'this' outside of a class!");
                return null;
            }

            ResolveLocal(expression, expression.Keyword);
            return null;
        }

        public object? VisitSuperExpression(Expression.Super expression)
        {
            if (CurrentClass == ClassType.NONE)
            {
                Program.Error(expression.Keyword, "Cannot use 'super' outside of class!");
            }
            else if (CurrentClass != ClassType.SUBCLASS)
            {
                Program.Error(expression.Keyword, "Cannot use 'super' in a class with no superclass!");
            }

            ResolveLocal(expression, expression.Keyword);
            return null;
        }
    }
}
