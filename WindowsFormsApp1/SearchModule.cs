using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    // Перечисление типов поиска
    public enum SearchPatternType
    {
        Email,
        PhoneNumber,
        Url,
        Date_DD_MM_YYYY,
        IpAddress,
        HexNumber,
        PascalComment,
        PascalRealNumber,
        Identifier
    }

    // Класс результата поиска
    public class SearchResult
    {
        public string MatchText { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public int AbsolutePosition { get; set; }

        public override string ToString()
        {
            return $"{MatchText} [строка {Line}, позиция {Column}]";
        }
    }

    // Модуль поиска с использованием регулярных выражений
    public class SearchModule
    {
        // Словарь регулярных выражений для каждого типа поиска
        private static readonly Dictionary<SearchPatternType, string> Patterns =
            new Dictionary<SearchPatternType, string>
        {
            // Email адрес
            { SearchPatternType.Email, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" },
            
            // Телефон (различные форматы: +7(123)456-78-90, 8-123-456-78-90, +7 123 456 78 90)
            { SearchPatternType.PhoneNumber, @"(\+7|8)[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}" },
            
            // URL
            { SearchPatternType.Url, @"https?://[^\s<>""{}|\\^`[\]]+[^\s<>""{}|\\^`[\].:,;!?()]" },
            
            // Дата ДД.ММ.ГГГГ или ДД/ММ/ГГГГ или ДД-ММ-ГГГГ
            { SearchPatternType.Date_DD_MM_YYYY, @"\b(0[1-9]|[12][0-9]|3[01])[./-](0[1-9]|1[0-2])[./-](19|20)\d{2}\b" },
            
            // IP-адрес
            { SearchPatternType.IpAddress, @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b" },
            
            // Шестнадцатеричное число (0xFF, 0x1A3, #FF00AA)
            { SearchPatternType.HexNumber, @"\b(0x[0-9A-Fa-f]+|#[0-9A-Fa-f]{6})\b" },
            
            // Комментарий Pascal { ... } или (* ... *)
            { SearchPatternType.PascalComment, @"\{[^}]*\}|\(\*[^*]*\*\)" },
            
            // Вещественное число Pascal (123.456, 123.456E-10)
            { SearchPatternType.PascalRealNumber, @"\b\d+\.\d+(?:E[+-]?\d+)?\b|\b\d+E[+-]?\d+\b" },
            
            // Идентификатор Pascal (начинается с буквы, затем буквы/цифры/подчеркивание)
            { SearchPatternType.Identifier, @"\b[A-Za-z_][A-Za-z0-9_]*\b" }
        };

        private static readonly Dictionary<SearchPatternType, string> PatternNames =
            new Dictionary<SearchPatternType, string>
        {
            { SearchPatternType.Email, "Email адреса" },
            { SearchPatternType.PhoneNumber, "Номера телефонов" },
            { SearchPatternType.Url, "URL адреса" },
            { SearchPatternType.Date_DD_MM_YYYY, "Даты (ДД.ММ.ГГГГ)" },
            { SearchPatternType.IpAddress, "IP-адреса" },
            { SearchPatternType.HexNumber, "Шестнадцатеричные числа" },
            { SearchPatternType.PascalComment, "Комментарии Pascal" },
            { SearchPatternType.PascalRealNumber, "Вещественные числа Pascal" },
            { SearchPatternType.Identifier, "Идентификаторы Pascal" }
        };

        public static string GetPatternName(SearchPatternType type)
        {
            return PatternNames.ContainsKey(type) ? PatternNames[type] : type.ToString();
        }

        public static SearchPatternType[] GetAllPatternTypes()
        {
            return (SearchPatternType[])Enum.GetValues(typeof(SearchPatternType));
        }

        // Выполнение поиска в тексте
        public List<SearchResult> Search(string text, SearchPatternType patternType)
        {
            List<SearchResult> results = new List<SearchResult>();

            if (string.IsNullOrEmpty(text))
                return results;

            string pattern = Patterns[patternType];

            // Настройки регулярного выражения
            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline;

            try
            {
                Regex regex = new Regex(pattern, options);
                MatchCollection matches = regex.Matches(text);

                // Предварительное вычисление позиций строк
                List<int> linePositions = GetLinePositions(text);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        int lineNumber = GetLineNumber(linePositions, match.Index);
                        int columnNumber = GetColumnNumber(linePositions, match.Index, lineNumber);

                        results.Add(new SearchResult
                        {
                            MatchText = match.Value,
                            Line = lineNumber,
                            Column = columnNumber,
                            Length = match.Length,
                            AbsolutePosition = match.Index
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в регулярном выражении: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return results;
        }

        // Получение позиций начала строк
        private List<int> GetLinePositions(string text)
        {
            List<int> positions = new List<int>();
            positions.Add(0);

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    positions.Add(i + 1);
                }
            }

            return positions;
        }

        // Определение номера строки по абсолютной позиции
        private int GetLineNumber(List<int> linePositions, int position)
        {
            for (int i = linePositions.Count - 1; i >= 0; i--)
            {
                if (position >= linePositions[i])
                    return i + 1;
            }
            return 1;
        }

        // Определение позиции в строке по абсолютной позиции
        private int GetColumnNumber(List<int> linePositions, int position, int lineNumber)
        {
            if (lineNumber <= linePositions.Count)
            {
                return position - linePositions[lineNumber - 1] + 1;
            }
            return position + 1;
        }

        // Валидация регулярного выражения
        public static bool ValidatePattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            try
            {
                new Regex(pattern);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Получение количества совпадений для предпросмотра
        public int GetMatchCount(string text, SearchPatternType patternType)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            string pattern = Patterns[patternType];
            try
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return regex.Matches(text).Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}