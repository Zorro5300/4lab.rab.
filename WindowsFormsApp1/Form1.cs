using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private RichTextBox editorTextBox;
        private DataGridView dataGridView;
        private RichTextBox errorTextBox;
        private string currentFile = null;
        private bool isModified = false;
        private ComboBox searchTypeComboBox;
        private Button searchButton;
        private Label searchResultCountLabel;
        private DataGridView searchResultsGridView;
        private SearchModule searchModule;
        private Panel searchPanel;

        public Form1()
        {
            InitializeComponent();
            CreateInterface();
        }

        private void CreateInterface()
        {
            this.Text = "Лексический анализатор Pascal";
            this.WindowState = FormWindowState.Maximized;

            // МЕНЮ
            MenuStrip menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Новый", null, (s, e) => NewFile());
            fileMenu.DropDownItems.Add("Открыть", null, (s, e) => OpenFile());
            fileMenu.DropDownItems.Add("Сохранить", null, (s, e) => SaveFile());
            fileMenu.DropDownItems.Add("Сохранить как", null, (s, e) => SaveFileAs());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Выход", null, (s, e) => Close());

            var editMenu = new ToolStripMenuItem("Правка");
            editMenu.DropDownItems.Add("Отменить", null, (s, e) => Undo());
            editMenu.DropDownItems.Add("Повторить", null, (s, e) => Redo());
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Вырезать", null, (s, e) => editorTextBox.Cut());
            editMenu.DropDownItems.Add("Копировать", null, (s, e) => editorTextBox.Copy());
            editMenu.DropDownItems.Add("Вставить", null, (s, e) => editorTextBox.Paste());

            var runMenu = new ToolStripMenuItem("Анализ");
            runMenu.DropDownItems.Add("Запустить анализ", null, (s, e) => RunAnalysis());

            var helpMenu = new ToolStripMenuItem("Справка");
            helpMenu.DropDownItems.Add("Справка", null, (s, e) => ShowHelp());
            helpMenu.DropDownItems.Add("О программе", null, (s, e) => ShowAbout());

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            menu.Items.Add(runMenu);
            menu.Items.Add(helpMenu);

            // ПАНЕЛЬ ИНСТРУМЕНТОВ
            ToolStrip toolStrip = new ToolStrip();
            toolStrip.ImageScalingSize = new Size(40, 40);
            toolStrip.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            toolStrip.AutoSize = true;
            toolStrip.Padding = new Padding(5);

            var newButton = new ToolStripButton("📄 Новый", null, (s, e) => NewFile());
            var openButton = new ToolStripButton("📂 Открыть", null, (s, e) => OpenFile());
            var saveButton = new ToolStripButton("💾 Сохранить", null, (s, e) => SaveFile());
            var undoButton = new ToolStripButton("↩️ Отменить", null, (s, e) => Undo());
            var redoButton = new ToolStripButton("↪️ Повторить", null, (s, e) => Redo());
            var cutButton = new ToolStripButton("✂️ Вырезать", null, (s, e) => editorTextBox.Cut());
            var copyButton = new ToolStripButton("📋 Копировать", null, (s, e) => editorTextBox.Copy());
            var pasteButton = new ToolStripButton("📌 Вставить", null, (s, e) => editorTextBox.Paste());
            var runButton = new ToolStripButton("▶️ Запуск", null, (s, e) => RunAnalysis());
            var helpButton = new ToolStripButton("❓ Справка", null, (s, e) => ShowHelp());

            toolStrip.Items.Add(newButton);
            toolStrip.Items.Add(openButton);
            toolStrip.Items.Add(saveButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(undoButton);
            toolStrip.Items.Add(redoButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(cutButton);
            toolStrip.Items.Add(copyButton);
            toolStrip.Items.Add(pasteButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(runButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(helpButton);

            // ========== СОЗДАНИЕ ВСЕХ КОМПОНЕНТОВ ==========

            // РЕДАКТОР
            editorTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 14),
                BackColor = Color.White
            };
            editorTextBox.TextChanged += (s, e) => { isModified = true; UpdateTitle(); };
            editorTextBox.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z) Undo();
                else if (e.Control && e.KeyCode == Keys.Y) Redo();
            };

            // ТАБЛИЦА ДЛЯ ТОКЕНОВ
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dataGridView.Columns.Add("Type", "Тип");
            dataGridView.Columns.Add("Value", "Лексема");
            dataGridView.Columns.Add("Line", "Строка");
            dataGridView.Columns.Add("Column", "Позиция");
            dataGridView.Columns.Add("Error", "Ошибка");
            dataGridView.Columns["Error"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // ОКНО ОШИБОК
            errorTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 12),
                BackColor = Color.MistyRose,
                ReadOnly = true
            };

            // ========== ПАНЕЛЬ ПОИСКА ==========
            searchModule = new SearchModule();

            searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label searchLabel = new Label
            {
                Text = "Поиск по шаблону:",
                Location = new Point(10, 12),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            searchTypeComboBox = new ComboBox
            {
                Location = new Point(130, 10),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };

            // Заполнение комбобокса
            foreach (var type in SearchModule.GetAllPatternTypes())
            {
                searchTypeComboBox.Items.Add(SearchModule.GetPatternName(type));
            }
            searchTypeComboBox.SelectedIndex = 0;

            searchButton = new Button
            {
                Text = "🔍 Найти (Ctrl+R)",
                Location = new Point(360, 9),
                Size = new Size(120, 28),
                BackColor = Color.LightBlue,
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat
            };
            searchButton.Click += (s, e) => RunRegexSearch();

            searchResultCountLabel = new Label
            {
                Text = "Найдено: 0",
                Location = new Point(490, 12),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DarkBlue
            };

            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(searchTypeComboBox);
            searchPanel.Controls.Add(searchButton);
            searchPanel.Controls.Add(searchResultCountLabel);

            // ========== ТАБЛИЦА РЕЗУЛЬТАТОВ ПОИСКА ==========
            searchResultsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            searchResultsGridView.Columns.Add("Match", "Найденная подстрока");
            searchResultsGridView.Columns.Add("Line", "Строка");
            searchResultsGridView.Columns.Add("Column", "Позиция");
            searchResultsGridView.Columns.Add("Length", "Длина");

            searchResultsGridView.SelectionChanged += (s, e) =>
            {
                if (searchResultsGridView.CurrentRow != null && editorTextBox != null)
                {
                    HighlightSelectedMatch(searchResultsGridView);
                }
            };

            // Панель с заголовком для результатов
            Panel searchResultsContainer = new Panel { Dock = DockStyle.Fill };
            Label searchResultsTitle = new Label
            {
                Text = " РЕЗУЛЬТАТЫ ПОИСКА",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(230, 230, 230)
            };
            searchResultsContainer.Controls.Add(searchResultsTitle);
            searchResultsContainer.Controls.Add(searchResultsGridView);
            searchResultsTitle.BringToFront();

            // интерфейс

            // Панель редактора
            Label editorLabel = new Label { Text = " РЕДАКТОР КОДА", Dock = DockStyle.Top, Height = 25, ForeColor = Color.Black, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            Panel editorPanel = new Panel { Dock = DockStyle.Fill };
            editorPanel.Controls.Add(editorTextBox);
            editorPanel.Controls.Add(editorLabel);

            // Панель ошибок
            Label errorsLabel = new Label { Text = " ОКНО ОШИБОК", Dock = DockStyle.Top, Height = 25, ForeColor = Color.Black, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(220, 220, 220) };
            Panel errorsPanel = new Panel { Dock = DockStyle.Fill };
            errorsPanel.Controls.Add(errorTextBox);
            errorsPanel.Controls.Add(errorsLabel);
            errorsLabel.BringToFront();

            // Нижний сплиттер (ошибки | результаты поиска)
            SplitContainer bottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = this.Width / 2
            };
            bottomSplit.Panel1.Controls.Add(errorsPanel);
            bottomSplit.Panel2.Controls.Add(searchResultsContainer);

            // Средний сплиттер (токены | нижний сплиттер)
            SplitContainer middleSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 0
            };
            //middleSplit.Panel1.Controls.Add(tokensPanel);
            middleSplit.Panel2.Controls.Add(bottomSplit);

            // Главный сплиттер (редактор | средний сплиттер)
            SplitContainer mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350
            };
            mainSplit.Panel1.Controls.Add(editorPanel);
            mainSplit.Panel2.Controls.Add(bottomSplit);

            // Добавляем всё на форму
            this.Controls.Add(mainSplit);
            this.Controls.Add(searchPanel);
            this.Controls.Add(toolStrip);
            this.Controls.Add(menu);

            // Устанавливаем порядок
            menu.Dock = DockStyle.Top;
            toolStrip.Dock = DockStyle.Top;
            searchPanel.Top = toolStrip.Bottom;
            mainSplit.Top = searchPanel.Bottom;

            //ОБРАБОТЧИК КЛАВИШ
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.N) { NewFile(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.O) { OpenFile(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.S) { SaveFile(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.Z) { Undo(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.Y) { Redo(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.X) { editorTextBox.Cut(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.C) { editorTextBox.Copy(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.V) { editorTextBox.Paste(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.F) { searchTypeComboBox.Focus(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.R) { RunRegexSearch(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { RunAnalysis(); e.Handled = true; }
            };

            UpdateTitle();
        }

        // МЕТОДЫ ПОИСКА 

        private void SetSearchType(int index)
        {
            if (searchTypeComboBox != null && index >= 0 && index < searchTypeComboBox.Items.Count)
            {
                searchTypeComboBox.SelectedIndex = index;
                RunRegexSearch();
            }
        }

        private void RunRegexSearch()
        {
            if (editorTextBox == null || string.IsNullOrEmpty(editorTextBox.Text))
            {
                MessageBox.Show("Нет данных для поиска. Введите текст в редактор.",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            searchResultsGridView.Rows.Clear();
            string text = editorTextBox.Text;
            int selectedIndex = searchTypeComboBox.SelectedIndex;
            SearchPatternType selectedType = SearchModule.GetAllPatternTypes()[selectedIndex];
            List<SearchResult> results = searchModule.Search(text, selectedType);

            foreach (var result in results)
            {
                searchResultsGridView.Rows.Add(result.MatchText, result.Line, result.Column, result.Length);
            }

            searchResultCountLabel.Text = $"Найдено: {results.Count}";

            if (searchResultsGridView.Rows.Count > 0)
            {
                searchResultsGridView.Sort(searchResultsGridView.Columns["Line"],
                    System.ComponentModel.ListSortDirection.Ascending);
            }

            ClearHighlighting();

            if (results.Count > 0)
            {
                HighlightSubstring(text, results[0].AbsolutePosition, results[0].Length);
                searchResultsGridView.Rows[0].Selected = true;
            }

            errorTextBox.AppendText($"\n=== ПОИСК ПО ШАБЛОНУ: {SearchModule.GetPatternName(selectedType)} ===\n");
            errorTextBox.AppendText($"Найдено совпадений: {results.Count}\n\n");
        }

        private void HighlightSelectedMatch(DataGridView grid)
        {
            if (grid.CurrentRow == null || editorTextBox == null) return;

            int line = Convert.ToInt32(grid.CurrentRow.Cells["Line"].Value);
            int column = Convert.ToInt32(grid.CurrentRow.Cells["Column"].Value);
            int length = Convert.ToInt32(grid.CurrentRow.Cells["Length"].Value);
            int position = FindPositionInText(editorTextBox.Text, line, column);

            if (position >= 0)
            {
                ClearHighlighting();
                HighlightSubstring(editorTextBox.Text, position, length);
                editorTextBox.Select(position, length);
                editorTextBox.ScrollToCaret();
            }
        }

        private int FindPositionInText(string text, int line, int column)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            int currentLine = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (currentLine == line) return i + column - 1;
                if (text[i] == '\n') currentLine++;
            }
            return -1;
        }

        private void HighlightSubstring(string text, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > editorTextBox.Text.Length) return;
            editorTextBox.Select(startIndex, length);
            editorTextBox.SelectionBackColor = Color.Yellow;
            editorTextBox.SelectionColor = Color.Black;
            editorTextBox.Refresh();
        }

        private void ClearHighlighting()
        {
            int oldStart = editorTextBox.SelectionStart;
            int oldLen = editorTextBox.SelectionLength;
            editorTextBox.SelectAll();
            editorTextBox.SelectionBackColor = editorTextBox.BackColor;
            editorTextBox.Select(oldStart, oldLen);
            editorTextBox.Refresh();
        }

        // ========== ОСТАЛЬНЫЕ МЕТОДЫ (без изменений) ==========

        private void Undo() { if (editorTextBox.CanUndo) editorTextBox.Undo(); }
        private void Redo() { if (editorTextBox.CanRedo) editorTextBox.Redo(); }

        private void UpdateTitle()
        {
            string name = string.IsNullOrEmpty(currentFile) ? "Новый файл" : Path.GetFileName(currentFile);
            this.Text = $"Pascal Анализатор - {name}{(isModified ? "*" : "")}";
        }

        private void NewFile()
        {
            if (CheckSave())
            {
                editorTextBox.Clear();
                currentFile = null;
                isModified = false;
                UpdateTitle();
                dataGridView.Rows.Clear();
                errorTextBox.Clear();
                searchResultsGridView.Rows.Clear();
                searchResultCountLabel.Text = "Найдено: 0";
            }
        }

        private void OpenFile()
        {
            if (!CheckSave()) return;
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Pascal files|*.pas|Text files|*.txt";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    editorTextBox.Text = File.ReadAllText(dlg.FileName);
                    currentFile = dlg.FileName;
                    isModified = false;
                    UpdateTitle();
                }
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFile)) SaveFileAs();
            else File.WriteAllText(currentFile, editorTextBox.Text);
            isModified = false;
            UpdateTitle();
            MessageBox.Show($"Файл сохранен: {Path.GetFileName(currentFile)}", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveFileAs()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "Pascal files|*.pas";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, editorTextBox.Text);
                    currentFile = dlg.FileName;
                    isModified = false;
                    UpdateTitle();
                    MessageBox.Show($"Файл сохранен: {Path.GetFileName(currentFile)}", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private bool CheckSave()
        {
            if (isModified)
            {
                DialogResult res = MessageBox.Show("Сохранить изменения?", "Вопрос", MessageBoxButtons.YesNoCancel);
                if (res == DialogResult.Yes) SaveFile();
                return res != DialogResult.Cancel;
            }
            return true;
        }

        private void RunAnalysis()
        {
            dataGridView.Rows.Clear();
            errorTextBox.Clear();
            string code = editorTextBox.Text;

            if (string.IsNullOrWhiteSpace(code))
            {
                errorTextBox.AppendText("ОШИБКА: Нет кода для анализа\n");
                return;
            }

            errorTextBox.AppendText("НАЧАЛО АНАЛИЗА\n\n");
            Lexer lexer = new Lexer(code);
            List<Token> tokens = lexer.GetAllTokens();
            int lexicalErrors = 0;
            List<LexicalErrorInfo> lexicalErrorList = new List<LexicalErrorInfo>();

            foreach (Token t in tokens)
            {
                if (t.Type == TokenType.Unknown)
                {
                    lexicalErrors++;
                    lexicalErrorList.Add(new LexicalErrorInfo { Value = t.Value, Line = t.Line, Column = t.Column, Message = $"Недопустимый символ '{t.Value}'" });
                }
            }

            SyntaxParser parser = new SyntaxParser(tokens);
            parser.Parse();

            if (lexicalErrors == 0 && parser.Errors.Count == 0)
            {
                errorTextBox.AppendText("✓ ОШИБОК НЕ ОБНАРУЖЕНО\n");
            }
            else
            {
                errorTextBox.AppendText($"\nНАЙДЕНО ОШИБОК: {lexicalErrors + parser.Errors.Count}\n");
                foreach (var err in lexicalErrorList)
                    errorTextBox.AppendText($"Строка {err.Line}: {err.Message}\n");
                foreach (var err in parser.Errors)
                    errorTextBox.AppendText($"Строка {err.Line}: {err.Message}\n");
            }
            errorTextBox.AppendText("\n=== КОНЕЦ АНАЛИЗА ===\n");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckSave()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        private void ShowHelp()
        {
            MessageBox.Show("Лексический анализатор Pascal\n\nГорячие клавиши:\nCtrl+F - фокус на поиск\nCtrl+R - поиск\nF5 - анализ", "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show("Лексический анализатор Pascal\nВерсия 3.0\n\nДобавлен поиск по регулярным выражениям", "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    public class LexicalErrorInfo
    {
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
    }

    public class SyntaxErrorInfo
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
    }
}