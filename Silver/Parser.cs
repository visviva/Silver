using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Silver
{
    internal class Parser
    {
        readonly List<Token> tokens;
        int current;



        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Statement> Parse()
        {
            List<Statement> statements = new();

            try
            {
                while (!IsAtEnd())
                {
                    statements.Add(ParseDeclaration()!);
                }
            }
            catch (Exception)
            {

                return new();
            }

            return statements;
        }

        private Statement? ParseDeclaration()
        {
            try
            {
                if (Match(TokenType.FUN)) return ParseFunction("function");
                if (Match(TokenType.VAR)) return ParseVarDeclaration();
                if (Match(TokenType.CLASS)) return ParseClassDeclaration();
                return ParseStatement();
            }
            catch (ParseErrorException)
            {
                Synchronize();
            }

            return null;
        }

        private Statement? ParseClassDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect class name!");

            Expression.Variable? superclass = null;

            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name!");
                superclass = new Expression.Variable(Previous());
            }

            Consume(TokenType.LEFT_BRACE, "Expect '{' before class body!");

            List<Statement.Function> methods = new();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                methods.Add(ParseFunction("method"));
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body!");

            return new Statement.Class(name, superclass, methods);
        }

        private Statement.Function ParseFunction(string v)
        {
            var name = Consume(TokenType.IDENTIFIER, $"Expect {v} name!");
            Consume(TokenType.LEFT_PAREN, $"Expect '(' after {v} name!");

            List<Token> parameters = new();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 parameters");
                    }

                    parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name!"));
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters!");
            Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {v} body!");

            var body = ParseBlock();
            return new Statement.Function(name, parameters, body);
        }

        private Statement ParseVarDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect variable name!");

            Expression? initializer = null;

            if (Match(TokenType.EQUAL))
            {
                initializer = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration!");
            return new Statement.Var(name, initializer);
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.PRINT))
            {
                return ParsePrintStatement();
            }

            if (Match(TokenType.LEFT_BRACE))
            {
                return new Statement.Block(ParseBlock());
            }

            if (Match(TokenType.RETURN))
            {
                return ParseReturn();
            }

            if (Match(TokenType.IF))
            {
                return ParseIfStatement();
            }

            if (Match(TokenType.WHILE))
            {
                return ParseWhileStatement();
            }

            if (Match(TokenType.FOR))
            {
                return ParseForStatement();
            }

            return ParseExpressionStatement();
        }

        private Statement ParseReturn()
        {
            var keyword = Previous();
            Expression? value = null;

            if (!Check(TokenType.SEMICOLON))
            {
                value = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after return value!");
            return new Statement.Return(keyword, value);
        }

        private Statement ParseForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after for!");

            Statement? initializer;

            if (Match(TokenType.SEMICOLON))
            {
                initializer = null;
            }
            else if (Match(TokenType.VAR))
            {
                initializer = ParseVarDeclaration();
            }
            else
            {
                initializer = ParseExpressionStatement();
            }

            Expression? condition = null;

            if (!Check(TokenType.SEMICOLON))
            {
                condition = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after for loop condition!");

            Expression? increment = null;

            if (!Check(TokenType.RIGHT_PAREN))
            {
                increment = ParseExpression();
            }

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses!");

            var body = ParseStatement();

            if (increment != null)
            {
                body = new Statement.Block(new List<Statement> { body, new Statement.Expression(increment) });
            }

            condition ??= new Expression.Literal("true");

            body = new Statement.While(condition, body);

            if (initializer != null)
            {
                body = new Statement.Block(new List<Statement> { initializer, body });
            }

            return body;
        }

        private Statement ParseWhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'!");
            var condition = ParseExpression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition!");
            var body = ParseStatement();
            return new Statement.While(condition, body);
        }

        private Statement ParseIfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'!");
            var condition = ParseExpression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after 'if' condition!");

            var thenBranch = ParseStatement();

            Statement? elseBranch = null;
            if (Match(TokenType.ELSE))
            {
                elseBranch = ParseStatement();
            }

            return new Statement.If(condition, thenBranch, elseBranch);
        }

        private List<Statement?> ParseBlock()
        {
            List<Statement?> statements = new();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block!");
            return statements;
        }

        private Statement ParsePrintStatement()
        {
            var value = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value!");
            return new Statement.Print(value);
        }

        private Statement ParseExpressionStatement()
        {
            var expr = ParseExpression();
            var nextToken = Peek();

            // Assume this is just a expression and make it a print statement
            if (nextToken.type == TokenType.EOF)
            {
                return new Statement.Print(expr);
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after expression!");
            return new Statement.Expression(expr);
        }

        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        private Expression ParseAssignment()
        {
            Expression expr = ParseLogicalOr();

            if (Match(TokenType.EQUAL))
            {
                var equals = Previous();
                var value = ParseAssignment();

                if (expr is Expression.Variable variable)
                {
                    return new Expression.Assignment(variable.Name, value);
                }
                else if (expr is Expression.Get get)
                {
                    return new Expression.Set(get.Obj, get.Name, value);
                }

                Error(equals, "Invalid assignment target!");
            }

            return expr;
        }

        private Expression ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();

            while (Match(TokenType.OR))
            {
                var operation = Previous();
                var right = ParseLogicalAnd();
                expr = new Expression.Logical(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            var expr = ParseEquality();

            while (Match(TokenType.AND))
            {
                var operation = Previous();
                var right = ParseEquality();
                expr = new Expression.Logical(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseEquality()
        {
            var expr = ParseComparison();

            while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                var operation = Previous();
                var right = ParseComparison();
                expr = new Expression.Binary(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseComparison()
        {
            Expression expr = ParseTerm();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                var operation = Previous();
                var right = ParseTerm();
                expr = new Expression.Binary(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseTerm()
        {
            Expression expr = ParseFactor();

            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                var operation = Previous();
                var right = ParseFactor();
                expr = new Expression.Binary(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseFactor()
        {
            Expression expr = ParseUnary();

            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                var operation = Previous();
                var right = ParseUnary();
                expr = new Expression.Binary(expr, operation, right);
            }

            return expr;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                var operation = Previous();
                var right = ParseUnary();
                return new Expression.Unary(operation, right);
            }

            return ParseCall();
        }

        private Expression ParseCall()
        {
            var expr = ParsePrimary();

            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.DOT))
                {
                    var name = Consume(TokenType.IDENTIFIER, "Expect property after '.'!");
                    expr = new Expression.Get(expr, name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expression FinishCall(Expression callee)
        {
            List<Expression> args = new();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (args.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 arguments!");
                    }
                    args.Add(ParseExpression());
                }
                while (Match(TokenType.COMMA));
            }

            Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments");
            return new Expression.Call(callee, paren, args);
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenType.FALSE))
            {
                return new Expression.Literal(false);
            }

            if (Match(TokenType.TRUE))
            {
                return new Expression.Literal(true);
            }

            if (Match(TokenType.NIL))
            {
                return new Expression.Literal(null);
            }

            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expression.Literal(Previous().literal);
            }

            if (Match(TokenType.THIS))
            {
                return new Expression.This(Previous());
            }

            if (Match(TokenType.SUPER))
            {
                var keyword = Previous();
                Consume(TokenType.DOT, "Expect '.' after 'super'!");
                var method = Consume(TokenType.IDENTIFIER, "Expect superclass method name!");
                return new Expression.Super(keyword, method);
            }

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expression.Variable(Previous());
            }

            if (Match(TokenType.LEFT_PAREN))
            {
                var expr = ParseExpression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression!");
                return new Expression.Grouping(expr);
            }

            throw Error(Peek(), "Expect expression!");
        }

        private bool Match(params TokenType[] tokens)
        {
            foreach (var token in tokens)
            {
                if (Check(token))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Consume(TokenType type, String message)
        {
            if (Check(type))
            {
                return Advance();
            }

            throw Error(Peek(), message);
        }

        private bool Check(TokenType token)
        {
            if (IsAtEnd())
            {
                return false;
            }

            return Peek().type == token;
        }

        private Token Advance()
        {
            if (!IsAtEnd())
            {
                current++;
            }

            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private static ParseErrorException Error(Token token, string message)
        {
            Program.Error(token, message);
            return new ParseErrorException();
        }

        private void Synchronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().type == TokenType.SEMICOLON)
                {
                    return;
                }

                switch (Peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                Advance();
            }
        }

    }
}
