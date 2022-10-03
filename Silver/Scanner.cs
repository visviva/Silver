using Spectre.Console;
using System.Data.Common;
using System.Globalization;

namespace Silver
{
    internal class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new();

        private int start = 0;
        private int current = 0;
        private int line = 1;

        private static readonly Dictionary<string, TokenType> keywords = new()
        {
            ["and"] = TokenType.AND,
            ["class"] = TokenType.CLASS,
            ["else"] = TokenType.ELSE,
            ["false"] = TokenType.FALSE,
            ["for"] = TokenType.FOR,
            ["fun"] = TokenType.FUN,
            ["if"] = TokenType.IF,
            ["nil"] = TokenType.NIL,
            ["or"] = TokenType.OR,
            ["print"] = TokenType.PRINT,
            ["return"] = TokenType.RETURN,
            ["super"] = TokenType.SUPER,
            ["this"] = TokenType.THIS,
            ["true"] = TokenType.TRUE,
            ["var"] = TokenType.VAR,
            ["while"] = TokenType.WHILE
        };

        public Scanner(string program)
        {
            this.source = program;
        }

        private bool IsAtEnd()
        {
            return current >= this.source.Length;
        }

        private char Advance()
        {
            return source.ElementAt(current++);
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object? literal)
        {
            string text = source[start..current];
            Token newToken = new(type, text, literal, line);
            tokens.Add(newToken);
        }

        private bool Match(char expected)
        {
            if (IsAtEnd())
            {
                return false;
            }

            if (source[current] != expected)
            {
                return false;
            }

            Advance();
            return true;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        private char PeekNext()
        {
            if (current + 1 >= source.Length)
            {
                return '\0';
            }

            return source[current + 1];
        }

        private void String()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                {
                    line++;
                }

                Advance();
            }

            if (IsAtEnd())
            {
                Silver.Program.Error(line, "Unterminated string.");
                return;
            }

            // The closing ".
            Advance();

            // Trim the surrounding quotes.
            var value = source[(start + 1)..(current - 1)];
            AddToken(TokenType.STRING, value);
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private void Number()
        {
            while (IsDigit(Peek()))
            {
                Advance();
            }

            // Look for fractional part.
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the "."
                Advance();

                while (IsDigit(Peek()))
                {
                    Advance();
                }
            }

            AddToken(TokenType.NUMBER, double.Parse(source[start..current], CultureInfo.CurrentCulture));
        }

        private static bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_');
        }

        private static bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        private void Identifier()
        {
            while (IsAlphaNumeric(Peek()))
            {
                Advance();
            }

            var text = source[start..current];

            if (keywords.ContainsKey(text))
            {
                var type = keywords[text];
                AddToken(type);
            }
            else
            {
                AddToken(TokenType.IDENTIFIER);
            }
        }

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                // Single tokens
                case '(': AddToken(TokenType.LEFT_PAREN); break;
                case ')': AddToken(TokenType.RIGHT_PAREN); break;
                case '{': AddToken(TokenType.LEFT_BRACE); break;
                case '}': AddToken(TokenType.RIGHT_BRACE); break;
                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.DOT); break;
                case '-': AddToken(TokenType.MINUS); break;
                case '+': AddToken(TokenType.PLUS); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '*': AddToken(TokenType.STAR); break;

                // Operators
                case '!': AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                case '=': AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break;
                case '<': AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                case '>': AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                // Special handling for slash, as it is used as comment starter too
                case '/':
                    {
                        if (Match('/'))
                        {
                            // A comment goes until the end of the line
                            while (Peek() != '\n' & !IsAtEnd())
                            {
                                Advance();
                            }
                        }
                        else
                        {
                            AddToken(TokenType.SLASH);
                        }
                    }
                    break;

                // Whitespaces
                case ' ':
                case '\r':
                case '\t':
                    break;

                // Line ending
                case '\n':
                    {
                        line++;
                    }
                    break;

                // Strings
                case '"': String(); break;

                default:
                    {
                        if (IsDigit(c))
                        {
                            Number();
                        }
                        else if (IsAlpha(c))
                        {
                            Identifier();
                        }
                        else
                        {
                            Silver.Program.Error(line, "Unexpected character.");
                        }
                    }
                    break;
            }
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // We are at the beginning of the next lexme.
                start = current;
                ScanToken();
            }

            var newToken = new Token(TokenType.EOF, "", null, line);
            tokens.Add(newToken);

            return tokens;
        }

    }
}
