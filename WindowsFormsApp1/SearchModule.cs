using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    // Перечисление типов поиска (лабораторная работа 4)
    public enum SearchPatternType
    {
        /// <summary>Номер ВУ: 2 цифры + 2 заглавные латинские буквы + 6 цифр (например 12AB345678).</summary>
        DriverLicenseRu,
        /// <summary>ФИО на английском: Last Name, First Name [Middle Name].</summary>
        EnglishFullName,
        /// <summary>Надёжный пароль (длина и набор символов по заданию).</summary>
        StrongPassword
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
            // ВУ: 2 цифры + 2 заглавные латинские буквы + 6 цифр (12AB345678)
            { SearchPatternType.DriverLicenseRu, @"\b\d{2}[A-Z]{2}\d{6}\b" },

            // Last Name, First Name [Middle Name] — слова с заглавной буквы, через запятую
            { SearchPatternType.EnglishFullName,
                @"\b[A-Z][A-Za-z]+(?:[-'][A-Z][A-Za-z]+)*(?:\s[A-Z][A-Za-z]+(?:[-'][A-Z][A-Za-z]+)*)*,\s+[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?\b" },

            // Пароль: ≥8 символов, заглавная, строчная, цифра, спецсимвол из {}#?!@$_%/^|&*-
            { SearchPatternType.StrongPassword,
                @"^(?=.{8,})(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[{}#?!@$_%/\\^|&*-])\S+$" }
        };

        private static readonly Dictionary<SearchPatternType, string> PatternNames =
            new Dictionary<SearchPatternType, string>
        {
            { SearchPatternType.DriverLicenseRu, "Водительское удостоверение (РФ)" },
            { SearchPatternType.EnglishFullName, "ФИО на английском (Last, First Middle)" },
            { SearchPatternType.StrongPassword, "Надёжный пароль" }
        };

        private static readonly Dictionary<SearchPatternType, RegexOptions> PatternOptions =
            new Dictionary<SearchPatternType, RegexOptions>
        {
            { SearchPatternType.DriverLicenseRu, RegexOptions.Compiled | RegexOptions.Multiline },
            { SearchPatternType.EnglishFullName, RegexOptions.Compiled | RegexOptions.Multiline },
            { SearchPatternType.StrongPassword, RegexOptions.Compiled | RegexOptions.Multiline }
        };

        public static string GetPatternName(SearchPatternType type)
        {
            return PatternNames.ContainsKey(type) ? PatternNames[type] : type.ToString();
        }

        public static string GetPattern(SearchPatternType type)
        {
            return Patterns.ContainsKey(type) ? Patterns[type] : string.Empty;
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
            RegexOptions options = PatternOptions[patternType];

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
                Regex regex = new Regex(pattern, PatternOptions[patternType]);
                return regex.Matches(text).Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}