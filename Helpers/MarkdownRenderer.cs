using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SwanCode.Core.Helpers
{
    /// <summary>
    /// Лёгкий markdown → FlowDocument рендер для сообщений чата (T-000103).
    /// Блоки: заголовки #–######, fenced-код ```, списки -/*/1., цитаты >, ---.
    /// Инлайн: **жирный**, *курсив*, `код`. Кисти — через SetResourceReference,
    /// чтобы работала живая смена темы.
    /// </summary>
    public static class MarkdownRenderer
    {
        public static FlowDocument Render(string? markdown)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                FontSize = 13,
                LineHeight = 19,
                // Fallback-цепочка: Segoe UI не содержит эмодзи, и ⏳/✅ из ответа модели
                // рисовались квадратиками-крокозябрами (смок 12.07).
                FontFamily = UiFontWithEmoji
            };
            doc.SetResourceReference(TextElement.ForegroundProperty, "PrimaryForeground");

            if (string.IsNullOrEmpty(markdown))
                return doc;

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var paragraphBuffer = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                // Fenced code block
                if (trimmed.StartsWith("```"))
                {
                    FlushParagraph(doc, paragraphBuffer);
                    var codeLines = new List<string>();
                    i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }
                    doc.Blocks.Add(BuildCodeBlock(string.Join("\n", codeLines)));
                    continue;
                }

                // Пустая строка — разрыв параграфа
                if (trimmed.Length == 0)
                {
                    FlushParagraph(doc, paragraphBuffer);
                    continue;
                }

                // Заголовок
                var heading = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
                if (heading.Success)
                {
                    FlushParagraph(doc, paragraphBuffer);
                    doc.Blocks.Add(BuildHeading(heading.Groups[2].Value, heading.Groups[1].Value.Length));
                    continue;
                }

                // Горизонтальная линия
                if (Regex.IsMatch(trimmed, @"^(-{3,}|\*{3,}|_{3,})$"))
                {
                    FlushParagraph(doc, paragraphBuffer);
                    doc.Blocks.Add(BuildRule());
                    continue;
                }

                // Таблица GFM: строка с пайпами + следующая строка-разделитель |---|---|
                if (IsTableStart(lines, i))
                {
                    FlushParagraph(doc, paragraphBuffer);
                    doc.Blocks.Add(BuildTable(lines, ref i));
                    continue;
                }

                // Список (маркированный / нумерованный) — собираем все подряд идущие пункты
                if (IsListItem(trimmed, out _, out _))
                {
                    FlushParagraph(doc, paragraphBuffer);
                    var list = BuildList(lines, ref i);
                    doc.Blocks.Add(list);
                    continue;
                }

                // Цитата
                if (trimmed.StartsWith(">"))
                {
                    FlushParagraph(doc, paragraphBuffer);
                    var quoteLines = new List<string> { trimmed.TrimStart('>').TrimStart() };
                    while (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith(">"))
                    {
                        i++;
                        quoteLines.Add(lines[i].TrimStart().TrimStart('>').TrimStart());
                    }
                    doc.Blocks.Add(BuildQuote(quoteLines));
                    continue;
                }

                paragraphBuffer.Add(line);
            }

            FlushParagraph(doc, paragraphBuffer);
            return doc;
        }

        /// <summary>Шрифт UI с эмодзи-фолбэком: Segoe UI не содержит ⏳/✅ и рисует квадратики.</summary>
        private static readonly FontFamily UiFontWithEmoji =
            new("Segoe UI, Segoe UI Emoji, Segoe UI Symbol");

        // --- Таблицы (GFM) --------------------------------------------------

        /// <summary>Заголовок таблицы + строка-разделитель под ним: |---|:--:|---|</summary>
        private static bool IsTableStart(string[] lines, int i)
        {
            if (i + 1 >= lines.Length) return false;
            if (!lines[i].Contains('|')) return false;
            return Regex.IsMatch(lines[i + 1].Trim(), @"^\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?$");
        }

        private static string[] SplitRow(string line) =>
            line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

        private static Table BuildTable(string[] lines, ref int i)
        {
            var header = SplitRow(lines[i]);
            i++; // строка-разделитель — её саму не рисуем
            var rows = new List<string[]>();

            while (i + 1 < lines.Length && lines[i + 1].Contains('|') &&
                   lines[i + 1].Trim().Length > 0)
            {
                i++;
                rows.Add(SplitRow(lines[i]));
            }

            var table = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 4, 0, 8),
                FontSize = 12
            };
            for (var c = 0; c < header.Length; c++)
                table.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            table.RowGroups.Add(group);
            group.Rows.Add(BuildTableRow(header, isHeader: true, header.Length));
            foreach (var r in rows)
                group.Rows.Add(BuildTableRow(r, isHeader: false, header.Length));

            return table;
        }

        private static TableRow BuildTableRow(string[] cells, bool isHeader, int columnCount)
        {
            var row = new TableRow();
            if (isHeader) row.FontWeight = FontWeights.SemiBold;

            for (var c = 0; c < columnCount; c++)
            {
                var para = new Paragraph { Margin = new Thickness(0) };
                if (c < cells.Length) AppendInlines(para.Inlines, cells[c]);

                var cell = new TableCell(para)
                {
                    Padding = new Thickness(7, 4, 7, 4),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                cell.SetResourceReference(TableCell.BorderBrushProperty, "BorderColor");
                if (isHeader)
                    cell.SetResourceReference(TableCell.BackgroundProperty, "TertiaryBackground");

                row.Cells.Add(cell);
            }
            return row;
        }

        // --- Блоки ----------------------------------------------------------

        private static void FlushParagraph(FlowDocument doc, List<string> buffer)
        {
            if (buffer.Count == 0) return;

            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
            for (int j = 0; j < buffer.Count; j++)
            {
                if (j > 0) para.Inlines.Add(new LineBreak());
                AppendInlines(para.Inlines, buffer[j]);
            }
            doc.Blocks.Add(para);
            buffer.Clear();
        }

        private static Paragraph BuildHeading(string text, int level)
        {
            var para = new Paragraph
            {
                FontWeight = FontWeights.SemiBold,
                FontSize = level switch { 1 => 17, 2 => 16, 3 => 14.5, _ => 13.5 },
                Margin = new Thickness(0, level <= 2 ? 8 : 6, 0, 4)
            };
            AppendInlines(para.Inlines, text);
            return para;
        }

        private static Paragraph BuildCodeBlock(string code)
        {
            var para = new Paragraph(new Run(code))
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 8),
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(2, 0, 0, 0),
                LineHeight = 16
            };
            para.SetResourceReference(TextElement.BackgroundProperty, "CodeBackground");
            para.SetResourceReference(Paragraph.BorderBrushProperty, "CodeBorder");
            return para;
        }

        private static Paragraph BuildRule()
        {
            var para = new Paragraph
            {
                Margin = new Thickness(0, 4, 0, 8),
                BorderThickness = new Thickness(0, 0, 0, 1),
                FontSize = 1
            };
            para.SetResourceReference(Paragraph.BorderBrushProperty, "BorderColor");
            return para;
        }

        private static Paragraph BuildQuote(List<string> quoteLines)
        {
            var para = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 8),
                Padding = new Thickness(10, 4, 0, 4),
                BorderThickness = new Thickness(3, 0, 0, 0)
            };
            para.SetResourceReference(Paragraph.BorderBrushProperty, "BorderColor");
            para.SetResourceReference(TextElement.ForegroundProperty, "SecondaryForeground");
            for (int j = 0; j < quoteLines.Count; j++)
            {
                if (j > 0) para.Inlines.Add(new LineBreak());
                AppendInlines(para.Inlines, quoteLines[j]);
            }
            return para;
        }

        private static bool IsListItem(string trimmed, out string content, out int? number)
        {
            content = string.Empty;
            number = null;

            var bullet = Regex.Match(trimmed, @"^[-*•]\s+(.*)$");
            if (bullet.Success)
            {
                content = bullet.Groups[1].Value;
                return true;
            }

            var ordered = Regex.Match(trimmed, @"^(\d{1,3})[.)]\s+(.*)$");
            if (ordered.Success)
            {
                content = ordered.Groups[2].Value;
                number = int.Parse(ordered.Groups[1].Value);
                return true;
            }

            return false;
        }

        private static List BuildList(string[] lines, ref int i)
        {
            IsListItem(lines[i].TrimStart(), out var first, out var firstNumber);

            var list = new List
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(22, 0, 0, 0),
                MarkerStyle = firstNumber.HasValue ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc
            };
            if (firstNumber.HasValue)
                list.StartIndex = firstNumber.Value;

            AddListItem(list, first);

            while (i + 1 < lines.Length && IsListItem(lines[i + 1].TrimStart(), out var next, out _))
            {
                i++;
                AddListItem(list, next);
            }

            return list;
        }

        private static void AddListItem(List list, string content)
        {
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
            AppendInlines(para.Inlines, content);
            list.ListItems.Add(new ListItem(para));
        }

        // --- Инлайн ----------------------------------------------------------

        private static readonly Regex InlineToken = new(
            @"(`[^`]+`)|(\*\*[^*]+?\*\*)|(\*[^*\s][^*]*?\*)", RegexOptions.Compiled);

        private static void AppendInlines(InlineCollection inlines, string text)
        {
            int pos = 0;
            foreach (Match m in InlineToken.Matches(text))
            {
                if (m.Index > pos)
                    inlines.Add(new Run(text[pos..m.Index]));

                var token = m.Value;
                if (token.StartsWith("`"))
                {
                    var run = new Run(token[1..^1])
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12
                    };
                    run.SetResourceReference(TextElement.BackgroundProperty, "CodeBackground");
                    inlines.Add(run);
                }
                else if (token.StartsWith("**"))
                {
                    inlines.Add(new Bold(new Run(token[2..^2])));
                }
                else
                {
                    inlines.Add(new Italic(new Run(token[1..^1])));
                }

                pos = m.Index + m.Length;
            }

            if (pos < text.Length)
                inlines.Add(new Run(text[pos..]));
        }
    }

    /// <summary>
    /// Attached-свойство: биндинг markdown-строки в RichTextBox
    /// (Document не является DP-таргетом для обычного биндинга).
    /// </summary>
    public static class MarkdownBehavior
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.RegisterAttached(
                "Markdown", typeof(string), typeof(MarkdownBehavior),
                new PropertyMetadata(null, OnMarkdownChanged));

        public static void SetMarkdown(DependencyObject element, string? value) =>
            element.SetValue(MarkdownProperty, value);

        public static string? GetMarkdown(DependencyObject element) =>
            (string?)element.GetValue(MarkdownProperty);

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox rtb)
                rtb.Document = MarkdownRenderer.Render(e.NewValue as string);
        }
    }
}
