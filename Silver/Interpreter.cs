using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace Silver
{
    internal class Interpreter : Expression.IVisitor<object?>, Statement.IVisitor<object?>
    {
        public readonly Environment GlobalEnvironment = new();
        private readonly Dictionary<Expression, int> Locals = new();
        private Environment VarEnvironment;

        public Interpreter()
        {
            GlobalEnvironment.Define("clock", new Time());
            VarEnvironment = GlobalEnvironment;
        }

        public object? VisitLiteralExpression(Expression.Literal expression)
        {
            return expression.Value;
        }

        public object? VisitGroupingExpression(Expression.Grouping expression)
        {
            return Evaluate(expression.Expr);
        }

        public object? VisitUnaryExpression(Expression.Unary expression)
        {
            var right = Evaluate(expression.Right);

            switch (expression.Operation.type)
            {
                case TokenType.MINUS:
                    CheckNumberOperand(expression.Operation, right);
                    return -(double)right!;

                case TokenType.BANG:
                    return !IsTruthy(right);
            }

            return null!;
        }

        private static void CheckNumberOperand(Token operation, object? operand)
        {
            if (operand is double) return;

            throw new RuntimeErrorException(operation, "Operand must be a number!");
        }

        private static bool IsTruthy(Object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is bool boolean)
            {
                return boolean;
            }

            return true;
        }

        public object? VisitLogicalExpression(Expression.Logical expression)
        {
            var left = Evaluate(expression.Left);

            switch (expression.Operation.type)
            {
                case TokenType.OR:
                    if (IsTruthy(left))
                    {
                        return left;
                    }
                    break;

                case TokenType.AND:
                    if (!IsTruthy(left))
                    {
                        return left;
                    }
                    break;

                default:
                    return null!;
            }

            return Evaluate(expression.Right);
        }

        public object? VisitBinaryExpression(Expression.Binary expression)
        {
            var left = Evaluate(expression.Left);
            var right = Evaluate(expression.Right);

            switch (expression.Operation.type)
            {
                case TokenType.MINUS:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! - (double)right!;

                case TokenType.SLASH:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! / (double)right!;

                case TokenType.STAR:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! * (double)right!;

                case TokenType.PLUS:
                    if (left is double @ld && right is double @rd)
                    {
                        return @ld + @rd;
                    }

                    if (left is string @ls && right is string @rs)
                    {
                        return @ls + @rs;
                    }

                    throw new RuntimeErrorException(expression.Operation, "Operands must be two numbers or two strings!");

                case TokenType.EQUAL_EQUAL:
                    CheckNumberOperands(expression.Operation, left, right);
                    return IsEqual(left, right);

                case TokenType.BANG_EQUAL:
                    CheckNumberOperands(expression.Operation, left, right);
                    return !IsEqual(left, right);

                case TokenType.GREATER:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! > (double)right!;

                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! >= (double)right!;

                case TokenType.LESS:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! < (double)right!;

                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expression.Operation, left, right);
                    return (double)left! <= (double)right!;

                default:
                    return null!;
            }
        }

        public object? VisitExpressionStatement(Statement.Expression statement)
        {
            Evaluate(statement.Expr);
            return null!;
        }

        public object? VisitIfStatement(Statement.If statement)
        {
            if (IsTruthy(Evaluate(statement.Condition)))
            {
                Execute(statement.ThenBranch);
            }
            else if (statement.ElseBranch != null)
            {
                Execute(statement.ElseBranch);
            }

            return null!;
        }

        public object? VisitWhileStatement(Statement.While statement)
        {
            while (IsTruthy(Evaluate(statement.Condition)))
            {
                Execute(statement.Body);
            }
            return null!;
        }

        public object? VisitPrintStatement(Statement.Print statement)
        {
            var value = Evaluate(statement.Expr);
            AnsiConsole.WriteLine(Stringify(value));
            return null!;
        }

        public object? VisitVarStatement(Statement.Var statement)
        {
            object? value = null;

            if (statement.Initializer != null)
            {
                value = Evaluate(statement.Initializer);
            }

            VarEnvironment.Define(statement.Name.lexeme, value!);
            return null!;
        }

        public object? VisitAssignmentExpression(Expression.Assignment expression)
        {
            var value = Evaluate(expression.Value);

            var found = Locals.TryGetValue(expression, out var distance);

            if (found)
            {
                VarEnvironment.AssignAt(distance, expression.Name, value);
            }
            else
            {
                GlobalEnvironment.Assign(expression.Name, value);
            }

            return value;
        }

        public object? VisitVariableExpression(Expression.Variable expression)
        {
            return LookUpVariable(expression.Name, expression);
        }

        private object LookUpVariable(Token name, Expression expression)
        {
            var contains = Locals.TryGetValue(expression, out var distanceValue);

            if (contains)
            {
                return VarEnvironment.GetAt(distanceValue, name.lexeme);
            }

            return GlobalEnvironment.Get(name);

        }

        public object? VisitBlockStatement(Statement.Block statement)
        {
            ExecuteBlock(statement.Statements, new Environment(VarEnvironment));
            return null!;
        }

        public object? VisitCallExpression(Expression.Call expression)
        {
            object? callee = Evaluate(expression.Callee!);

            var arguments = new List<object>();

            foreach (var arg in expression.Arguments)
            {
                arguments.Add(Evaluate(arg)!);
            }

            if (callee is not ICallable)
            {
                throw new RuntimeErrorException(expression.Paren, "Can only call functions and classes!");
            }

            ICallable function = (ICallable)callee;

            if (arguments.Count != function.Arity())
            {
                throw new RuntimeErrorException(expression.Paren, $"Expected {function.Arity()} arguments but got {arguments.Count}");
            }

            return function.Call(this, arguments);
        }

        public void ExecuteBlock(List<Statement> statements, Environment environment)
        {
            var previous = VarEnvironment;

            try
            {
                VarEnvironment = environment;

                foreach (Statement statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                VarEnvironment = previous;
            }
        }

        private static void CheckNumberOperands(Token operation, object? left, object? right)
        {
            if (left is double && right is double)
            {
                return;
            }

            throw new RuntimeErrorException(operation, "Operands must be numbers!");
        }

        private static bool IsEqual(object? left, object? right)
        {
            if ((left == null) && (right == null))
            {
                return true;
            }

            if (left == null)
            {
                return false;
            }

            if (right == null)
            {
                return false;
            }

            return (double)left == (double)right;
        }

        private object? Evaluate(Expression expr)
        {
            return expr!.Accept(this);
        }

        private void Execute(Statement stmt)
        {
            stmt.Accept(this);
        }

        public void Interpret(List<Statement> statements)
        {
            try
            {
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            catch (RuntimeErrorException error)
            {
                Program.RuntimeError(error);
            }
        }

        private static string Stringify(object? value)
        {
            if (value == null)
            {
                return "nil";
            }

            if (value is double)
            {
                var text = value.ToString();
                if (text!.EndsWith(".0"))
                {
                    text = text[^2..];

                }
                return text;
            }

            return value.ToString()!;
        }

        public object? VisitFunctionStatement(Statement.Function statement)
        {
            Function function = new(statement, VarEnvironment, false);
            VarEnvironment.Define(statement.Name.lexeme, function);
            return null;
        }

        public object? VisitReturnStatement(Statement.Return statement)
        {
            object? value = null;
            if (statement.Value != null) { value = Evaluate(statement.Value); }
            throw new ReturnException(value!);
        }

        internal void Resolve(Expression expression, int depth)
        {
            Locals.Add(expression, depth);
        }

        public object? VisitClassStatement(Statement.Class statement)
        {
            object? superclass = null;

            if (statement.Superclass != null)
            {
                superclass = Evaluate(statement.Superclass);

                if (superclass is not Class)
                {
                    throw new RuntimeErrorException(statement.Superclass.Name, "Superclass must be a class!");
                }
            }

            VarEnvironment.Define(statement.Name.lexeme, null!);

            if (statement.Superclass != null)
            {
                VarEnvironment = new Environment(VarEnvironment);
                VarEnvironment.Define("super", superclass!);
            }

            Dictionary<string, Function> methods = new();

            foreach (var method in statement.Methods)
            {
                var function = new Function(method, VarEnvironment, method.Name.lexeme.Equals("init"));
                methods.Add(method.Name.lexeme, function);
            }

            var klass = new Class(statement.Name.lexeme, superclass as Class, methods);

            if (superclass != null)
            {
                VarEnvironment = VarEnvironment.Enclosing!;
            }

            VarEnvironment.Assign(statement.Name, klass);
            return null;
        }

        public object? VisitGetExpression(Expression.Get expression)
        {
            var obj = Evaluate(expression.Obj);

            if (obj is Instance instance)
            {
                return instance.Get(expression.Name);
            }

            throw new RuntimeErrorException(expression.Name, "Only instances have properties!");
        }

        public object? VisitSetExpression(Expression.Set expression)
        {
            var obj = Evaluate(expression.Obj);

            if (obj is not Instance)
            {
                throw new RuntimeErrorException(expression.Name, "Only instances have fields!");
            }

            var value = Evaluate(expression.Value);

            var instance = (Instance)obj;
            instance.Set(expression.Name, value);
            return value;
        }

        public object? VisitThisExpression(Expression.This expression)
        {
            return LookUpVariable(expression.Keyword, expression);
        }

        public object? VisitSuperExpression(Expression.Super expression)
        {
            var distance = Locals[expression];

            var superclass = VarEnvironment.GetAt(distance, "super") as Class;
            var obj = VarEnvironment.GetAt(distance - 1, "this") as Instance;

            var method = superclass!.FindMethod(expression.Method.lexeme) as Function;

            return method == null
                ? throw new RuntimeErrorException(expression.Method, $"Undefined property '{expression.Method.lexeme}'!")
                : (object)method!.Bind(obj!);
        }
    }
}
