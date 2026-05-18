using System;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public class SyntaxError
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class RecordField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class RecordInfo
    {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public List<RecordField> Fields { get; set; } = new List<RecordField>();
    }

    public class SyntaxParser
    {
        private List<Token> _tokens;
        private int _position;
        private Token _currentToken;
        private List<SyntaxError> _errors = new List<SyntaxError>();

        public List<SyntaxError> Errors => _errors;
        public RecordInfo FoundRecord { get; private set; }

        public SyntaxParser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _currentToken = _tokens.Count > 0 ? _tokens[0] : new Token(TokenType.EndOfFile, "", 1, 1);
        }

        public bool Parse()
        {
            _errors.Clear();
            FoundRecord = null;

            int maxIterations = _tokens.Count * 2; // Защита от бесконечного цикла
            int iterations = 0;

            while (_currentToken.Type != TokenType.EndOfFile && iterations < maxIterations)
            {
                iterations++;

                if (_currentToken.Type == TokenType.Keyword && _currentToken.Value.ToLower() == "type")
                {
                    ParseTypeSection();
                }
                else if (_currentToken.Type == TokenType.Identifier)
                {
                    string val = _currentToken.Value.ToLower();
                    if (val == "tpe" || val == "tye" || val == "typee" || val == "typ")
                    {
                        AddError($"Возможно, вы имели в виду 'type', а не '{_currentToken.Value}'",
                            _currentToken.Line, _currentToken.Column);
                    }
                    MoveNext();
                }
                else
                {
                    MoveNext();
                }
            }

            return _errors.Count == 0;
        }

        private void ParseTypeSection()
        {
            MoveNext(); // пропускаем "type"
            SkipEmptyLines();

            // Имя типа
            if (_currentToken.Type != TokenType.Identifier)
            {
                AddError($"Ожидается имя типа, найдено '{_currentToken.Value}'",
                    _currentToken.Line, _currentToken.Column);
                return;
            }

            string typeName = _currentToken.Value;
            int typeLine = _currentToken.Line;
            MoveNext();
            SkipEmptyLines();

            // Знак "="
            if (_currentToken.Type != TokenType.Operator || _currentToken.Value != "=")
            {
                AddError($"Ожидается '=', найдено '{_currentToken.Value}'",
                    _currentToken.Line, _currentToken.Column);
                return;
            }
            MoveNext();
            SkipEmptyLines();

            // Ключевое слово "record"
            if (_currentToken.Type == TokenType.Keyword && _currentToken.Value.ToLower() == "record")
            {
                ParseRecord(typeName, typeLine);
            }
            else
            {
                if (_currentToken.Type == TokenType.Identifier)
                {
                    string val = _currentToken.Value.ToLower();
                    if (val == "recor" || val == "recrd" || val == "recoed" || val == "ecord" || val == "rekord")
                    {
                        AddError($"Возможно, вы имели в виду 'record', а не '{_currentToken.Value}'",
                            _currentToken.Line, _currentToken.Column);
                    }
                    else
                    {
                        AddError($"Ожидается 'record', найдено '{_currentToken.Value}'",
                            _currentToken.Line, _currentToken.Column);
                    }
                }
                else
                {
                    AddError($"Ожидается 'record', найдено '{_currentToken.Value}'",
                        _currentToken.Line, _currentToken.Column);
                }
            }
        }

        private void ParseRecord(string typeName, int typeLine)
        {
            MoveNext(); // пропускаем "record"
            SkipEmptyLines();

            var recordInfo = new RecordInfo { StartLine = typeLine, StartColumn = 1 };

            bool firstField = true;
            int maxFields = 1000; // Защита от бесконечного цикла
            int fieldCount = 0;

            while (fieldCount < maxFields)
            {
                SkipEmptyLines();
                fieldCount++;

                // Проверка на конец файла
                if (_currentToken.Type == TokenType.EndOfFile)
                {
                    AddError($"Незавершенная запись '{typeName}' - не найден 'end'",
                        typeLine, 1);
                    break;
                }

                // Проверка на конец записи
                if (_currentToken.Type == TokenType.Keyword && _currentToken.Value.ToLower() == "end")
                {
                    // Проверяем точку с запятой перед end
                    if (!firstField)
                    {
                        int prevPos = _position - 1;
                        while (prevPos >= 0 && (_tokens[prevPos].Type == TokenType.NewLine ||
                               _tokens[prevPos].Type == TokenType.Comment ||
                               _tokens[prevPos].Type == TokenType.Whitespace))
                        {
                            prevPos--;
                        }

                        if (prevPos >= 0 && prevPos < _tokens.Count && _tokens[prevPos].Type != TokenType.Semicolon)
                        {
                            AddError("Пропущена ';' перед 'end'",
                                _currentToken.Line, _currentToken.Column);
                        }
                    }

                    recordInfo.EndLine = _currentToken.Line;
                    FoundRecord = recordInfo;
                    MoveNext();

                    // ВАЖНО: Пропускаем все пробелы, комментарии и переводы строк
                    while (_currentToken.Type == TokenType.NewLine ||
                           _currentToken.Type == TokenType.Comment ||
                           _currentToken.Type == TokenType.Whitespace)
                    {
                        MoveNext();
                    }


                    SkipEmptyLines();

                    // ПРОВЕРКА ПОСЛЕ end
                    if (_currentToken.Type == TokenType.Semicolon)
                    {
                        MoveNext();
                    }
                    else if (_currentToken.Type == TokenType.Dot)
                    {
                        MoveNext();
                    }
                    else if (_currentToken.Type == TokenType.EndOfFile)
                    {
                        // Если конец файла - тоже ошибка!
                        AddError("После 'end' ожидается ';' или '.' (достигнут конец файла)",
                            _currentToken.Line, _currentToken.Column);
                    }
                    else
                    {
                        AddError("После 'end' ожидается ';' или '.'",
                            _currentToken.Line, _currentToken.Column);
                    }

                    break;
                }

                // Парсим поле
                if (!ParseField(recordInfo))
                {
                    // Если ошибка при парсинге поля, пропускаем до следующего поля
                    SkipToNextField();
                    continue;
                }
                firstField = false;

                SkipEmptyLines();

                // Проверяем точку с запятой после поля
                if (_currentToken.Type == TokenType.Semicolon)
                {
                    MoveNext();
                }
                else if (!(_currentToken.Type == TokenType.Keyword && _currentToken.Value.ToLower() == "end"))
                {
                    AddError($"Пропущена ';' после объявления поля",
                        _currentToken.Line, _currentToken.Column);
                    // Не будет выходить, продолжаем
                }
            }
        }

        private bool ParseField(RecordInfo recordInfo)
        {
            List<string> identifiers = new List<string>();
            int fieldLine = _currentToken.Line;
            int fieldColumn = _currentToken.Column;

            // Имя поля
            if (_currentToken.Type != TokenType.Identifier)
            {
                AddError($"Ожидается имя поля, найдено '{_currentToken.Value}'",
                    _currentToken.Line, _currentToken.Column);
                return false;
            }

            identifiers.Add(_currentToken.Value);
            MoveNext();
            SkipEmptyLines();

            // Список полей через запятую
            while (_currentToken.Type == TokenType.Comma)
            {
                MoveNext();
                SkipEmptyLines();

                if (_currentToken.Type == TokenType.Identifier)
                {
                    identifiers.Add(_currentToken.Value);
                    MoveNext();
                    SkipEmptyLines();
                }
                else
                {
                    AddError($"Ожидается идентификатор после запятой, найдено '{_currentToken.Value}'",
                        _currentToken.Line, _currentToken.Column);
                    return false;
                }
            }

            // Двоеточие
            if (_currentToken.Type != TokenType.Colon)
            {
                AddError($"Ожидается ':' после имени поля, найдено '{_currentToken.Value}'",
                    _currentToken.Line, _currentToken.Column);
                return false;
            }
            MoveNext();
            SkipEmptyLines();

            // Тип поля
            if (_currentToken.Type != TokenType.Keyword)
            {
                AddError($"Ожидается тип поля (integer, real, string, boolean, char), найдено '{_currentToken.Value}'",
                    _currentToken.Line, _currentToken.Column);
                return false;
            }

            string fieldType = _currentToken.Value;
            MoveNext();

            // Обработка string[длина]
            if (fieldType.ToLower() == "string" && _currentToken.Type == TokenType.OpenBracket)
            {
                fieldType += "[";
                MoveNext(); // пропускаем [
                SkipEmptyLines();

                if (_currentToken.Type == TokenType.Number)
                {
                    fieldType += _currentToken.Value;
                    MoveNext();
                    SkipEmptyLines();
                }

                if (_currentToken.Type == TokenType.CloseBracket)
                {
                    fieldType += "]";
                    MoveNext();
                }
                else
                {
                    AddError("Ожидается ']' для закрытия длины строки",
                        _currentToken.Line, _currentToken.Column);
                }
                SkipEmptyLines();
            }

            // Проверка на допустимый тип
            string typeLower = fieldType.ToLower().Split('[')[0];
            if (typeLower != "integer" && typeLower != "real" &&
                typeLower != "string" && typeLower != "boolean" && typeLower != "char")
            {
                AddError($"Недопустимый тип '{fieldType}'. Допустимые: integer, real, string, boolean, char",
                    _currentToken.Line, _currentToken.Column);
            }

            // Добавляем поля
            foreach (string name in identifiers)
            {
                recordInfo.Fields.Add(new RecordField
                {
                    Name = name,
                    Type = fieldType,
                    Line = fieldLine,
                    Column = fieldColumn
                });
            }

            return true;
        }

        private void SkipToNextField()
        {
            int maxSkip = 50;
            int skipped = 0;

            while (skipped < maxSkip && _currentToken.Type != TokenType.EndOfFile &&
                   _currentToken.Type != TokenType.Identifier &&
                   !(_currentToken.Type == TokenType.Keyword && _currentToken.Value.ToLower() == "end"))
            {
                MoveNext();
                SkipEmptyLines();
                skipped++;
            }
        }

        private void SkipEmptyLines()
        {
            while (_currentToken.Type == TokenType.NewLine ||
                   _currentToken.Type == TokenType.Comment ||
                   _currentToken.Type == TokenType.Whitespace)
            {
                MoveNext();
            }
        }

        private void MoveNext()
        {
            if (_position + 1 < _tokens.Count)
            {
                _position++;
                _currentToken = _tokens[_position];
            }
            else
            {
                _currentToken = new Token(TokenType.EndOfFile, "", _currentToken.Line, _currentToken.Column);
                _position = _tokens.Count;
            }
        }

        private void AddError(string message, int line, int column)
        {
            _errors.Add(new SyntaxError { Message = message, Line = line, Column = column });
        }
    }
}
