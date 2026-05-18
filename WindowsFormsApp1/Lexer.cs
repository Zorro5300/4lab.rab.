using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApp1
{
    public class Lexer
    {
        private string _sourceCode;
        private int _position;
        private int _line;
        private int _column;
        private char _currentChar;

        private static readonly HashSet<string> _pascalKeywords = new HashSet<string>
        {
            "program", "begin", "end", "var", "const", "type", "record",
            "procedure", "function", "if", "then", "else", "case", "of",
            "while", "do", "for", "to", "downto", "repeat", "until",
            "integer", "real", "boolean", "char", "string", "array"
        };

        public Lexer(string sourceCode)
        {
            _sourceCode = sourceCode;
            _position = 0;
            _line = 1;
            _column = 1;
            _currentChar = _sourceCode.Length > 0 ? _sourceCode[0] : '\0';
        }

        private void Advance()
        {
            if (_currentChar == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _position++;
            _currentChar = _position < _sourceCode.Length ? _sourceCode[_position] : '\0';
        }

        private char Peek()
        {
            return _position + 1 < _sourceCode.Length ? _sourceCode[_position + 1] : '\0';
        }

        private bool IsLatinLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private bool IsRussianLetter(char c)
        {
            return (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == 'ё' || c == 'Ё';
        }

        public Token GetNextToken()
        {
            while (_currentChar != '\0')
            {
                int currentLine = _line;
                int currentColumn = _column;

                // Пробелы (не переводы строк)
                if (char.IsWhiteSpace(_currentChar) && _currentChar != '\n')
                {
                    while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar) && _currentChar != '\n')
                        Advance();
                    continue;
                }

                // Перевод строки
                if (_currentChar == '\n')
                {
                    Advance();
                    return new Token(TokenType.NewLine, "\n", currentLine, currentColumn);
                }

                // ===== РУССКИЕ БУКВЫ - ОШИБКА! =====
                if (IsRussianLetter(_currentChar))
                {
                    char russianChar = _currentChar;
                    Advance();
                    return new Token(TokenType.Unknown, russianChar.ToString(), currentLine, currentColumn);
                }

                // Идентификаторы и ключевые слова (только латиница!)
                if (IsLatinLetter(_currentChar) || _currentChar == '_')
                {
                    StringBuilder sb = new StringBuilder();
                    while (_currentChar != '\0' && (IsLatinLetter(_currentChar) || char.IsDigit(_currentChar) || _currentChar == '_'))
                    {
                        sb.Append(_currentChar);
                        Advance();
                    }
                    string value = sb.ToString();
                    TokenType type = _pascalKeywords.Contains(value.ToLower()) ? TokenType.Keyword : TokenType.Identifier;
                    return new Token(type, value, currentLine, currentColumn);
                }

                // Числа
                if (char.IsDigit(_currentChar))
                {
                    StringBuilder sb = new StringBuilder();
                    while (_currentChar != '\0' && char.IsDigit(_currentChar))
                    {
                        sb.Append(_currentChar);
                        Advance();
                    }
                    return new Token(TokenType.Number, sb.ToString(), currentLine, currentColumn);
                }

                // Строки в кавычках
                if (_currentChar == '\'')
                {
                    Advance();
                    StringBuilder sb = new StringBuilder();
                    while (_currentChar != '\0' && _currentChar != '\'')
                    {
                        sb.Append(_currentChar);
                        Advance();
                    }
                    if (_currentChar == '\'')
                        Advance();
                    return new Token(TokenType.String, sb.ToString(), currentLine, currentColumn);
                }

                // Комментарии { ... }
                if (_currentChar == '{')
                {
                    Advance();
                    StringBuilder sb = new StringBuilder();
                    while (_currentChar != '\0' && _currentChar != '}')
                    {
                        sb.Append(_currentChar);
                        Advance();
                    }
                    if (_currentChar == '}')
                        Advance();
                    return new Token(TokenType.Comment, sb.ToString(), currentLine, currentColumn);
                }

                // Операторы и символы
                switch (_currentChar)
                {
                    case ';': Advance(); return new Token(TokenType.Semicolon, ";", currentLine, currentColumn);
                    case ':':
                        if (Peek() == '=') { Advance(); Advance(); return new Token(TokenType.Assign, ":=", currentLine, currentColumn); }
                        Advance(); return new Token(TokenType.Colon, ":", currentLine, currentColumn);
                    case ',': Advance(); return new Token(TokenType.Comma, ",", currentLine, currentColumn);
                    case '.':
                        if (Peek() == '.') { Advance(); Advance(); return new Token(TokenType.Operator, "..", currentLine, currentColumn); }
                        Advance(); return new Token(TokenType.Dot, ".", currentLine, currentColumn);
                    case '(': Advance(); return new Token(TokenType.OpenParen, "(", currentLine, currentColumn);
                    case ')': Advance(); return new Token(TokenType.CloseParen, ")", currentLine, currentColumn);
                    case '=': Advance(); return new Token(TokenType.Operator, "=", currentLine, currentColumn);
                    case '+': Advance(); return new Token(TokenType.Operator, "+", currentLine, currentColumn);
                    case '-': Advance(); return new Token(TokenType.Operator, "-", currentLine, currentColumn);
                    case '*': Advance(); return new Token(TokenType.Operator, "*", currentLine, currentColumn);
                    case '/': Advance(); return new Token(TokenType.Operator, "/", currentLine, currentColumn);
                    case '<':
                        Advance();
                        if (_currentChar == '=') { Advance(); return new Token(TokenType.Operator, "<=", currentLine, currentColumn); }
                        if (_currentChar == '>') { Advance(); return new Token(TokenType.Operator, "<>", currentLine, currentColumn); }
                        return new Token(TokenType.Operator, "<", currentLine, currentColumn);
                    case '>':
                        Advance();
                        if (_currentChar == '=') { Advance(); return new Token(TokenType.Operator, ">=", currentLine, currentColumn); }
                        return new Token(TokenType.Operator, ">", currentLine, currentColumn);
                    case '[': Advance(); return new Token(TokenType.OpenBracket, "[", currentLine, currentColumn);
                    case ']': Advance(); return new Token(TokenType.CloseBracket, "]", currentLine, currentColumn);
                    default:
                        char unknown = _currentChar;
                        Advance();
                        return new Token(TokenType.Unknown, unknown.ToString(), currentLine, currentColumn);
                }
            }

            return new Token(TokenType.EndOfFile, "", _line, _column);
        }

        public List<Token> GetAllTokens()
        {
            var tokens = new List<Token>();
            Token token;
            do
            {
                token = GetNextToken();
                tokens.Add(token);
            } while (token.Type != TokenType.EndOfFile);
            return tokens;
        }
    }
}
