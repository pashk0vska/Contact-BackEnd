using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Contact.API.Helpers
{
    public static class AnalyticsPdf
    {
        public static byte[] Build(ReportData d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(BrandAssets.TextDark));

                    page.Header().Element(c => Header(c, d));
                    page.Content().PaddingHorizontal(36).PaddingVertical(16).Column(col =>
                    {
                        col.Spacing(18);
                        col.Item().Element(c => KpiGrid(c, d));
                        col.Item().Element(c => CategorySection(c, d));
                        if (d.Daily.Any(x => x.Total > 0)) col.Item().Element(c => TrendSection(c, d));
                        if (d.IncludeSales && d.TopProducts.Count > 0) col.Item().Element(c => ProductsSection(c, d));
                        if (d.IncludeSales && (d.TopServices.Count > 0 || d.Payments.Count > 0)) col.Item().Element(c => SalesExtraSection(c, d));
                        if (d.IncludeRepairs && (d.RepairsByStatus.Count > 0 || d.RepairsByDevice.Count > 0)) col.Item().Element(c => RepairsSection(c, d));
                        if (d.StaffPerformance.Count > 0) col.Item().Element(c => StaffSection(c, d));
                    });
                    page.Footer().Element(c => Footer(c, d));
                });
            });

            return doc.GeneratePdf();
        }

        // ===== Шапка з логотипом =====
        private static void Header(IContainer c, ReportData d) =>
            c.Background(BrandAssets.Ink).PaddingHorizontal(36).PaddingVertical(18).Row(row =>
            {
                row.ConstantItem(150).AlignMiddle().Height(32).Image(BrandAssets.Logo).FitArea();
                row.RelativeItem().AlignRight().AlignMiddle().Column(col =>
                {
                    col.Item().AlignRight().Text("Аналітичний звіт").FontSize(17).Bold().FontColor(BrandAssets.HeadText);
                    col.Item().AlignRight().Text($"Період: {d.PeriodText}").FontSize(10).FontColor("#C7D0D8");
                    col.Item().AlignRight().Text($"Дані: {d.ModeText}  •  {d.Days} дн.").FontSize(8.5f).FontColor(BrandAssets.GreenBright);
                });
            });

        private static void Footer(IContainer c, ReportData d) =>
            c.BorderTop(1).BorderColor("#E3E8EC").PaddingHorizontal(36).PaddingVertical(8).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text($"Kontakt CRM  •  Згенеровано {d.GeneratedAt:dd.MM.yyyy HH:mm}").FontSize(8).FontColor(BrandAssets.TextMuted);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(BrandAssets.TextMuted));
                    t.Span("Сторінка "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                });
            });

        // ===== KPI-плитки =====
        private static void KpiGrid(IContainer c, ReportData d)
        {
            var k = d.Kpis;
            c.Column(col =>
            {
                col.Spacing(8);
                for (int i = 0; i < k.Count; i += 3)
                {
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);
                        for (int j = i; j < Math.Min(i + 3, k.Count); j++)
                        {
                            var m = k[j];
                            row.RelativeItem().Element(cc => KpiCard(cc, m));
                        }
                        for (int pad = k.Count; pad < i + 3; pad++) row.RelativeItem();
                    });
                }
            });
        }

        private static void KpiCard(IContainer c, Metric m) =>
            c.Background("#FAFCFD").Border(1).BorderColor("#E6ECEF").PaddingVertical(10).PaddingHorizontal(12).Column(col =>
            {
                col.Item().Text(m.Label.ToUpperInvariant()).FontSize(7.5f).FontColor(BrandAssets.TextMuted);
                col.Item().PaddingTop(3).Text(FmtMetric(m)).FontSize(15).Bold().FontColor(BrandAssets.Ink);
                if (m.DeltaPct.HasValue)
                {
                    var up = m.DeltaPct.Value >= 0;
                    var good = up == m.MoreIsBetter;
                    col.Item().PaddingTop(2).Text($"{(up ? "▲" : "▼")} {Math.Abs(m.DeltaPct.Value):0.#}% до попер. періоду")
                        .FontSize(7.5f).FontColor(good ? BrandAssets.Green : BrandAssets.Red);
                }
                else col.Item().PaddingTop(2).Text("—").FontSize(7.5f).FontColor("#C2CAD1");
            });

        // ===== Дохід за категоріями =====
        private static void CategorySection(IContainer c, ReportData d) =>
            c.Column(col =>
            {
                Title(col, "Структура доходу за категоріями");
                Table(col.Item(),
                    new[] { ("Категорія", 0f, 'l'), ("Сума, ₴", 90f, 'r'), ("Частка", 220f, 'l') },
                    d.ByCategory.Select(x => (Action<TableRow>)(r =>
                    {
                        r.Text(x.Name);
                        r.Money(x.Value);
                        r.Bar((double)x.Share, CatColor(x.Name));
                    })).ToList(),
                    footer: r => { r.TextBold("Разом"); r.Money(d.ByCategory.Sum(x => x.Value), true); r.Text(""); });
            });

        // ===== Динаміка по днях (стовпчикова діаграма) =====
        private static void TrendSection(IContainer c, ReportData d) =>
            c.Column(col =>
            {
                Title(col, "Динаміка доходу по днях");
                var max = d.Daily.Max(x => x.Total);
                if (max <= 0) max = 1;
                col.Item().Height(110).Background("#FAFCFD").Border(1).BorderColor("#E6ECEF").Padding(8).Row(row =>
                {
                    row.Spacing(1);
                    foreach (var day in d.Daily)
                    {
                        var ratio = (double)day.Total / (double)max;
                        var px = day.Total > 0 ? (float)Math.Max(ratio * 86.0, 2.0) : 0f;
                        row.RelativeItem().AlignBottom().Column(bar =>
                        {
                            if (px > 0) bar.Item().Height(px).Background(BrandAssets.Green);
                        });
                    }
                });
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Початок: {d.FromLocal:dd.MM}").FontSize(7.5f).FontColor(BrandAssets.TextMuted);
                    row.RelativeItem().AlignCenter().Text(d.BestDay != null
                        ? $"Найкращий день: {d.BestDay.Date:dd.MM.yyyy} — ₴ {d.BestDay.Total:N0}"
                        : "").FontSize(7.5f).FontColor(BrandAssets.Green).Bold();
                    row.RelativeItem().AlignRight().Text($"Кінець: {d.ToLocal:dd.MM}").FontSize(7.5f).FontColor(BrandAssets.TextMuted);
                });
            });

        // ===== ТОП товарів =====
        private static void ProductsSection(IContainer c, ReportData d) =>
            c.Column(col =>
            {
                Title(col, "ТОП-10 продуктів за виручкою");
                Table(col.Item(),
                    new[] { ("#", 22f, 'c'), ("Назва", 0f, 'l'), ("Категорія", 70f, 'l'), ("К-ть", 42f, 'c'), ("Сер. ціна", 68f, 'r'), ("Сума, ₴", 78f, 'r'), ("Частка", 110f, 'l') },
                    d.TopProducts.Select((x, idx) => (Action<TableRow>)(r =>
                    {
                        r.Text((idx + 1).ToString());
                        r.Text(x.Product);
                        r.TextMuted(x.Category);
                        r.Text(x.Qty.ToString());
                        r.Money(x.AvgPrice);
                        r.Money(x.Sum);
                        r.Bar((double)x.Share, BrandAssets.Green);
                    })).ToList());
            });

        // ===== Послуги + оплати =====
        private static void SalesExtraSection(IContainer c, ReportData d) =>
            c.Row(row =>
            {
                row.Spacing(16);
                if (d.TopServices.Count > 0)
                    row.RelativeItem().Column(col =>
                    {
                        Title(col, "ТОП послуг");
                        Table(col.Item(),
                            new[] { ("Послуга", 0f, 'l'), ("К-ть", 45f, 'c'), ("Сума, ₴", 80f, 'r') },
                            d.TopServices.Select(x => (Action<TableRow>)(r => { r.Text(x.Name); r.Text(x.Count.ToString()); r.Money(x.Sum); })).ToList());
                    });
                if (d.Payments.Count > 0)
                    row.RelativeItem().Column(col =>
                    {
                        Title(col, "Способи оплати");
                        Table(col.Item(),
                            new[] { ("Метод", 0f, 'l'), ("Чеків", 45f, 'c'), ("Сума, ₴", 78f, 'r'), ("Частка", 78f, 'l') },
                            d.Payments.Select(x => (Action<TableRow>)(r => { r.Text(x.Method); r.Text(x.Count.ToString()); r.Money(x.Sum); r.Bar((double)x.Share, BrandAssets.Blue); })).ToList());
                    });
            });

        // ===== Ремонти =====
        private static void RepairsSection(IContainer c, ReportData d) =>
            c.Column(col =>
            {
                Title(col, "Ремонти");
                col.Item().Row(row =>
                {
                    row.Spacing(16);
                    if (d.RepairsByStatus.Count > 0)
                        row.RelativeItem().Column(cl =>
                        {
                            cl.Item().PaddingBottom(4).Text("За статусами").FontSize(9).Bold().FontColor(BrandAssets.Ink);
                            Table(cl.Item(),
                                new[] { ("Статус", 0f, 'l'), ("К-ть", 45f, 'c'), ("Частка", 90f, 'l') },
                                d.RepairsByStatus.Select(x => (Action<TableRow>)(r => { r.Text(x.Status); r.Text(x.Count.ToString()); r.Bar((double)x.Share, StatusColor(x.Status)); })).ToList());
                        });
                    if (d.RepairsByDevice.Count > 0)
                        row.RelativeItem().Column(cl =>
                        {
                            cl.Item().PaddingBottom(4).Text("За типом пристрою").FontSize(9).Bold().FontColor(BrandAssets.Ink);
                            Table(cl.Item(),
                                new[] { ("Пристрій", 0f, 'l'), ("К-ть", 40f, 'c'), ("Сума, ₴", 75f, 'r') },
                                d.RepairsByDevice.Select(x => (Action<TableRow>)(r => { r.Text(x.Device); r.Text(x.Count.ToString()); r.Money(x.Sum); })).ToList());
                        });
                });
                col.Item().PaddingTop(6).Text($"Завершено (готово/видано): {d.CompletionRate:0.#}%   •   Сер. вартість ремонту: ₴ {d.AvgRepairCost:N2}")
                    .FontSize(8.5f).FontColor(BrandAssets.TextMuted);
            });

        // ===== Персонал =====
        private static void StaffSection(IContainer c, ReportData d) =>
            c.Column(col =>
            {
                Title(col, "Ефективність персоналу");
                Table(col.Item(),
                    new[] { ("Майстер / менеджер", 0f, 'l'), ("Прод.", 42f, 'c'), ("₴ продажі", 78f, 'r'), ("Рем.", 42f, 'c'), ("₴ ремонти", 78f, 'r'), ("Разом, ₴", 82f, 'r') },
                    d.StaffPerformance.Select(x => (Action<TableRow>)(r =>
                    {
                        r.Text(x.Name);
                        r.Text(x.SalesCount.ToString());
                        r.Money(x.SalesSum);
                        r.Text(x.RepairsCount.ToString());
                        r.Money(x.RepairsSum);
                        r.Money(x.Total, true);
                    })).ToList());
            });

        // ===== Допоміжні =====
        private static void Title(ColumnDescriptor col, string text)
        {
            col.Item().PaddingBottom(6).Row(r =>
            {
                r.ConstantItem(4).Background(BrandAssets.Green);
                r.ConstantItem(8);
                r.RelativeItem().AlignMiddle().Text(text).FontSize(12).Bold().FontColor(BrandAssets.Ink);
            });
        }

        // columns = (заголовок, ширина(0=відносна), вирівнювання l/c/r)
        private static void Table(IContainer host, (string h, float w, char a)[] cols, List<Action<TableRow>> rows, Action<TableRow>? footer = null)
        {
            host.Border(1).BorderColor("#E6ECEF").Table(table =>
            {
                table.ColumnsDefinition(def =>
                {
                    foreach (var col in cols) { if (col.w <= 0) def.RelativeColumn(); else def.ConstantColumn(col.w); }
                });
                table.Header(header =>
                {
                    foreach (var col in cols)
                    {
                        var cell = header.Cell().Background(BrandAssets.Ink).PaddingVertical(6).PaddingHorizontal(7).AlignMiddle();
                        cell = Align(cell, col.a);
                        cell.Text(col.h).FontSize(8).Bold().FontColor(BrandAssets.HeadText);
                    }
                });
                int ri = 0;
                foreach (var build in rows)
                {
                    var tr = new TableRow(table, cols, ri % 2 == 1 ? BrandAssets.RowAlt : "#FFFFFF");
                    build(tr);
                    ri++;
                }
                if (footer != null)
                {
                    var tr = new TableRow(table, cols, "#EAF6EB");
                    footer(tr);
                }
            });
        }

        private static IContainer Align(IContainer c, char a) => a switch { 'r' => c.AlignRight(), 'c' => c.AlignCenter(), _ => c.AlignLeft() };

        private static string CatColor(string name) => name switch
        {
            "Ремонти" => BrandAssets.Green,
            "Товари" => BrandAssets.Blue,
            "Збірки" => BrandAssets.Violet,
            "Послуги" => BrandAssets.Amber,
            _ => BrandAssets.Green
        };

        private static string StatusColor(string s) => s switch
        {
            "Новий" => BrandAssets.Blue,
            "В процесі" => BrandAssets.Amber,
            "Готово" => BrandAssets.GreenBright,
            "Видано" => BrandAssets.Green,
            "Скасовано" => BrandAssets.Red,
            _ => "#5B6B76"
        };

        private static string FmtMetric(Metric m)
        {
            if (m.IsMoney) return $"₴ {m.Current:N2}";
            if (m.IsPercent) return $"{m.Current:0.#}%";
            if (m.Current == decimal.Truncate(m.Current)) return $"{m.Current:N0}{m.Suffix}";
            return $"{m.Current:0.##}{m.Suffix}";
        }

        // Рядок таблиці: додає осередки відповідно до визначення колонок
        internal class TableRow
        {
            private readonly TableDescriptor _t;
            private readonly (string h, float w, char a)[] _cols;
            private readonly string _bg;
            private int _i;
            public TableRow(TableDescriptor t, (string h, float w, char a)[] cols, string bg) { _t = t; _cols = cols; _bg = bg; }

            private IContainer Cell(bool overrideAlignLeft = false)
            {
                var col = _cols[_i];
                IContainer cell = _t.Cell().Background(_bg).BorderBottom(1).BorderColor("#EEF1F3").PaddingVertical(5).PaddingHorizontal(7).AlignMiddle();
                cell = Align(cell, overrideAlignLeft ? 'l' : col.a);
                _i++;
                return cell;
            }

            public void Text(string s) => Cell().Text(s).FontSize(8.8f);
            public void TextBold(string s) => Cell().Text(s).FontSize(8.8f).Bold();
            public void TextMuted(string s) => Cell().Text(s).FontSize(8.8f).FontColor(BrandAssets.TextMuted);

            public void Money(decimal v, bool bold = false)
            {
                var t = Cell().Text($"{v:N2}").FontSize(8.8f);
                if (bold) t.Bold();
            }

            public void Bar(double share, string color)
            {
                var pct = (float)Math.Max(0, Math.Min(100, share));
                Cell(overrideAlignLeft: true).Row(r =>
                {
                    r.Spacing(6);
                    r.ConstantItem(36).AlignMiddle().Text($"{share:0.#}%").FontSize(8).FontColor(BrandAssets.TextDark);
                    r.RelativeItem().AlignMiddle().Height(8).Background("#EDF1F4").Row(b =>
                    {
                        if (pct > 0) b.RelativeItem(pct).Background(color);
                        var rest = 100f - pct;
                        if (rest > 0) b.RelativeItem(rest);
                    });
                });
            }
        }
    }
}
