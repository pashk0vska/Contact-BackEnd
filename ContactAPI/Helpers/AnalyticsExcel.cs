using ClosedXML.Excel;

namespace Contact.API.Helpers
{
    public static class AnalyticsExcel
    {
        private const string MoneyFmt = "#,##0.00 \"₴\"";
        private const string IntFmt = "#,##0";
        private const string PctFmt = "0.0%";

        public static byte[] Build(ReportData d)
        {
            using var wb = new XLWorkbook();
            wb.Style.Font.FontName = "Calibri";
            wb.Style.Font.FontSize = 11;

            BuildOverview(wb, d);
            BuildTrend(wb, d);
            if (d.IncludeSales && (d.TopProducts.Count > 0 || d.TopServices.Count > 0)) BuildProducts(wb, d);
            if (d.IncludeRepairs && (d.RepairsByStatus.Count > 0 || d.RepairsByDevice.Count > 0)) BuildRepairs(wb, d);
            if (d.StaffPerformance.Count > 0) BuildStaff(wb, d);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ОГЛЯД
        private static void BuildOverview(XLWorkbook wb, ReportData d)
        {
            var ws = wb.Worksheets.Add("Огляд");
            ws.ShowGridLines = false;
            double[] widths = { 34, 18, 18, 18, 16, 16 };
            for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];

            // Брендова шапка
            var band = ws.Range(1, 1, 3, 6);
            band.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandAssets.Ink);
            ws.Row(1).Height = 16; ws.Row(2).Height = 26; ws.Row(3).Height = 16;
            try
            {
                using var logo = new MemoryStream(BrandAssets.Logo);
                ws.AddPicture(logo, "kontakt").MoveTo(ws.Cell(1, 1), 12, 14).WithSize(184, 36);
            }
            catch { /* логотип не критичний */ }

            var title = ws.Cell(2, 4); title.Value = "Аналітичний звіт";
            ws.Range(2, 4, 2, 6).Merge();
            title.Style.Font.Bold = true; title.Style.Font.FontSize = 16; title.Style.Font.FontColor = XLColor.White;
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int r = 5;
            ws.Cell(r, 1).Value = "Період:"; ws.Cell(r, 2).Value = d.PeriodText;
            ws.Cell(r + 1, 1).Value = "Тип даних:"; ws.Cell(r + 1, 2).Value = d.ModeText;
            ws.Cell(r, 4).Value = "Кількість днів:"; ws.Cell(r, 5).Value = d.Days;
            ws.Cell(r + 1, 4).Value = "Згенеровано:"; ws.Cell(r + 1, 5).Value = d.GeneratedAt.ToString("dd.MM.yyyy HH:mm");
            ws.Range(r, 1, r + 1, 6).Style.Font.FontColor = XLColor.FromHtml(BrandAssets.TextMuted);
            ws.Range(r, 1, r + 1, 1).Style.Font.Bold = true;
            ws.Range(r, 4, r + 1, 4).Style.Font.Bold = true;
            r += 3;

            // KPI
            Section(ws, ref r, "Ключові показники", 6);
            int hRow = r;
            Head(ws, r, new[] { "Показник", "Значення", "Поперед. період", "Δ %" }, 1);
            r++;
            int kpiStart = r;
            foreach (var m in d.Kpis)
            {
                ws.Cell(r, 1).Value = m.Label;
                var vc = ws.Cell(r, 2);
                if (m.IsMoney) { vc.Value = (double)m.Current; vc.Style.NumberFormat.Format = MoneyFmt; }
                else { vc.Value = (double)m.Current; vc.Style.NumberFormat.Format = m.Current == decimal.Truncate(m.Current) ? IntFmt : "#,##0.##"; }
                vc.Style.Font.Bold = true;

                var pc = ws.Cell(r, 3);
                if (m.Previous.HasValue)
                {
                    pc.Value = (double)m.Previous.Value;
                    pc.Style.NumberFormat.Format = m.IsMoney ? MoneyFmt : (m.Previous.Value == decimal.Truncate(m.Previous.Value) ? IntFmt : "#,##0.##");
                    pc.Style.Font.FontColor = XLColor.FromHtml(BrandAssets.TextMuted);
                }
                else pc.Value = "—";

                var dc = ws.Cell(r, 4);
                if (m.DeltaPct.HasValue)
                {
                    dc.Value = (double)m.DeltaPct.Value / 100.0;
                    dc.Style.NumberFormat.Format = "+0.0%;-0.0%";
                    var good = (m.DeltaPct.Value >= 0) == m.MoreIsBetter;
                    dc.Style.Font.FontColor = XLColor.FromHtml(good ? BrandAssets.Green : BrandAssets.Red);
                    dc.Style.Font.Bold = true;
                }
                else { dc.Value = "—"; dc.Style.Font.FontColor = XLColor.FromHtml(BrandAssets.TextMuted); }
                r++;
            }
            Boxed(ws, hRow, 1, r - 1, 4, kpiStart);

            r += 1;
            // Категорії
            Section(ws, ref r, "Структура доходу за категоріями", 6);
            int cHead = r;
            Head(ws, r, new[] { "Категорія", "Сума", "Частка", "Візуалізація" }, 1);
            r++; int cStart = r;
            foreach (var c in d.ByCategory)
            {
                ws.Cell(r, 1).Value = c.Name;
                ws.Cell(r, 2).Value = (double)c.Value; ws.Cell(r, 2).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 3).Value = (double)c.Share / 100.0; ws.Cell(r, 3).Style.NumberFormat.Format = PctFmt;
                ws.Cell(r, 4).Value = (double)c.Value;
                ws.Cell(r, 4).Style.NumberFormat.Format = ";;;"; // ховаємо число, лишаємо лише data bar
                r++;
            }
            if (r > cStart)
            {
                ws.Range(cStart, 4, r - 1, 4).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Green)).LowestValue().HighestValue();
                ws.Cell(r, 1).Value = "Разом"; ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 2).Value = (double)d.ByCategory.Sum(x => x.Value); ws.Cell(r, 2).Style.NumberFormat.Format = MoneyFmt; ws.Cell(r, 2).Style.Font.Bold = true;
                ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF6EB");
                Boxed(ws, cHead, 1, r, 4, cStart);
                r++;
            }

            // Способи оплати + Статистика чеків
            if (d.IncludeSales)
            {
                r += 1;
                Section(ws, ref r, "Способи оплати", 6);
                int pHead = r;
                Head(ws, r, new[] { "Метод", "Чеків", "Сума", "Частка" }, 1);
                r++; int pStart = r;
                foreach (var p in d.Payments)
                {
                    ws.Cell(r, 1).Value = p.Method;
                    ws.Cell(r, 2).Value = p.Count; ws.Cell(r, 2).Style.NumberFormat.Format = IntFmt;
                    ws.Cell(r, 3).Value = (double)p.Sum; ws.Cell(r, 3).Style.NumberFormat.Format = MoneyFmt;
                    ws.Cell(r, 4).Value = (double)p.Share / 100.0; ws.Cell(r, 4).Style.NumberFormat.Format = PctFmt;
                    r++;
                }
                if (r > pStart) Boxed(ws, pHead, 1, r - 1, 4, pStart);

                r += 1;
                Section(ws, ref r, "Статистика чеків", 6);
                int sHead = r;
                var stats = new (string, decimal, bool)[]
                {
                    ("Мінімальний чек", d.MinCheck, true),
                    ("Медіанний чек", d.MedianCheck, true),
                    ("Середній чек", d.AvgCheck, true),
                    ("Максимальний чек", d.MaxCheck, true),
                    ("Усього позицій продано", d.TotalItems, false),
                    ("Позицій на чек", d.ItemsPerSale, false),
                };
                foreach (var (label, val, money) in stats)
                {
                    ws.Cell(r, 1).Value = label;
                    var c = ws.Cell(r, 2); c.Value = (double)val;
                    c.Style.NumberFormat.Format = money ? MoneyFmt : (val == decimal.Truncate(val) ? IntFmt : "#,##0.##");
                    c.Style.Font.Bold = true;
                    r++;
                }
                Boxed(ws, sHead, 1, r - 1, 2, sHead);
            }

            ws.SheetView.FreezeRows(3);
        }

        // ДИНАМІКА
        private static void BuildTrend(XLWorkbook wb, ReportData d)
        {
            var ws = wb.Worksheets.Add("Динаміка");
            ws.ShowGridLines = false;
            ws.Column(1).Width = 16; ws.Column(2).Width = 16; ws.Column(3).Width = 16; ws.Column(4).Width = 16; ws.Column(5).Width = 26;
            ws.Column(7).Width = 14; ws.Column(8).Width = 18;

            int r = 1;
            Section(ws, ref r, "Динаміка доходу по днях", 5);
            int head = r;
            Head(ws, r, new[] { "Дата", "Продажі", "Ремонти", "Разом", "Тренд" }, 1);
            r++; int start = r;
            foreach (var day in d.Daily)
            {
                ws.Cell(r, 1).Value = day.Date; ws.Cell(r, 1).Style.NumberFormat.Format = "dd.MM.yyyy";
                ws.Cell(r, 2).Value = (double)day.Sales; ws.Cell(r, 2).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 3).Value = (double)day.Repairs; ws.Cell(r, 3).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 4).Value = (double)day.Total; ws.Cell(r, 4).Style.NumberFormat.Format = MoneyFmt; ws.Cell(r, 4).Style.Font.Bold = true;
                ws.Cell(r, 5).Value = (double)day.Total; ws.Cell(r, 5).Style.NumberFormat.Format = ";;;";
                r++;
            }
            if (r > start)
            {
                ws.Range(start, 5, r - 1, 5).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Green)).LowestValue().HighestValue();
                // підсумок
                ws.Cell(r, 1).Value = "Разом"; ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 2).Value = (double)d.Daily.Sum(x => x.Sales); ws.Cell(r, 2).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 3).Value = (double)d.Daily.Sum(x => x.Repairs); ws.Cell(r, 3).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 4).Value = (double)d.Daily.Sum(x => x.Total); ws.Cell(r, 4).Style.NumberFormat.Format = MoneyFmt;
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF6EB");
                Boxed(ws, head, 1, r, 5, start);
                ws.Range(start, 1, r - 1, 5).SetAutoFilter();
            }

            // Дні тижня (окрема міні-таблиця праворуч)
            int wr = head;
            Section(ws, ref wr, "За днями тижня", 8, col: 7);
            Head(ws, wr, new[] { "День", "Дохід" }, 7);
            wr++; int wStart = wr;
            foreach (var w in d.Weekdays)
            {
                ws.Cell(wr, 7).Value = w.Name;
                ws.Cell(wr, 8).Value = (double)w.Value; ws.Cell(wr, 8).Style.NumberFormat.Format = MoneyFmt;
                wr++;
            }
            ws.Range(wStart, 8, wr - 1, 8).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Blue)).LowestValue().HighestValue();
            Boxed(ws, head, 7, wr - 1, 8, wStart);

            ws.SheetView.FreezeRows(head + 1);
        }

        // ТОВАРИ / ПОСЛУГИ
        private static void BuildProducts(XLWorkbook wb, ReportData d)
        {
            var ws = wb.Worksheets.Add("Товари");
            ws.ShowGridLines = false;
            double[] widths = { 6, 38, 14, 10, 16, 18, 12, 22 };
            for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];

            int r = 1;
            Section(ws, ref r, "ТОП-10 продуктів за виручкою", 8);
            int head = r;
            Head(ws, r, new[] { "#", "Назва", "Категорія", "К-ть", "Сер. ціна", "Сума", "Частка", "Візуалізація" }, 1);
            r++; int start = r;
            int idx = 1;
            foreach (var p in d.TopProducts)
            {
                ws.Cell(r, 1).Value = idx++;
                ws.Cell(r, 2).Value = p.Product;
                ws.Cell(r, 3).Value = p.Category; ws.Cell(r, 3).Style.Font.FontColor = XLColor.FromHtml(BrandAssets.TextMuted);
                ws.Cell(r, 4).Value = p.Qty; ws.Cell(r, 4).Style.NumberFormat.Format = IntFmt;
                ws.Cell(r, 5).Value = (double)p.AvgPrice; ws.Cell(r, 5).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 6).Value = (double)p.Sum; ws.Cell(r, 6).Style.NumberFormat.Format = MoneyFmt; ws.Cell(r, 6).Style.Font.Bold = true;
                ws.Cell(r, 7).Value = (double)p.Share / 100.0; ws.Cell(r, 7).Style.NumberFormat.Format = PctFmt;
                ws.Cell(r, 8).Value = (double)p.Sum; ws.Cell(r, 8).Style.NumberFormat.Format = ";;;";
                r++;
            }
            if (r > start)
            {
                ws.Range(start, 8, r - 1, 8).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Green)).LowestValue().HighestValue();
                Boxed(ws, head, 1, r - 1, 8, start);
                ws.Range(start, 1, r - 1, 8).SetAutoFilter();
            }

            if (d.TopServices.Count > 0)
            {
                r += 1;
                Section(ws, ref r, "ТОП послуг", 8);
                int sHead = r;
                Head(ws, r, new[] { "Послуга", "Кількість", "Сума" }, 1);
                r++; int sStart = r;
                foreach (var s in d.TopServices)
                {
                    ws.Cell(r, 1).Value = s.Name;
                    ws.Cell(r, 2).Value = s.Count; ws.Cell(r, 2).Style.NumberFormat.Format = IntFmt;
                    ws.Cell(r, 3).Value = (double)s.Sum; ws.Cell(r, 3).Style.NumberFormat.Format = MoneyFmt;
                    r++;
                }
                Boxed(ws, sHead, 1, r - 1, 3, sStart);
            }

            ws.SheetView.FreezeRows(head + 1);
        }

        // РЕМОНТИ
        private static void BuildRepairs(XLWorkbook wb, ReportData d)
        {
            var ws = wb.Worksheets.Add("Ремонти");
            ws.ShowGridLines = false;
            double[] widths = { 22, 12, 14, 4, 22, 12, 16, 16 };
            for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];

            int r = 1;
            Section(ws, ref r, $"Ремонти — завершено {d.CompletionRate:0.#}% · сер. вартість {d.AvgRepairCost:#,##0.00} ₴", 8);
            int top = r;

            // За статусами (ліва таблиця)
            Head(ws, r, new[] { "Статус", "К-ть", "Частка" }, 1);
            int sr = r + 1; int sStart = sr;
            foreach (var s in d.RepairsByStatus)
            {
                ws.Cell(sr, 1).Value = s.Status;
                ws.Cell(sr, 2).Value = s.Count; ws.Cell(sr, 2).Style.NumberFormat.Format = IntFmt;
                ws.Cell(sr, 3).Value = (double)s.Share / 100.0; ws.Cell(sr, 3).Style.NumberFormat.Format = PctFmt;
                sr++;
            }
            if (sr > sStart)
            {
                ws.Range(sStart, 2, sr - 1, 2).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Amber)).LowestValue().HighestValue();
                Boxed(ws, top, 1, sr - 1, 3, sStart);
            }

            // За пристроями (права таблиця)
            Head(ws, top, new[] { "Пристрій", "К-ть", "Сума", "Сер." }, 5);
            int dr = top + 1; int dStart = dr;
            foreach (var dev in d.RepairsByDevice)
            {
                ws.Cell(dr, 5).Value = dev.Device;
                ws.Cell(dr, 6).Value = dev.Count; ws.Cell(dr, 6).Style.NumberFormat.Format = IntFmt;
                ws.Cell(dr, 7).Value = (double)dev.Sum; ws.Cell(dr, 7).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(dr, 8).Value = (double)dev.Avg; ws.Cell(dr, 8).Style.NumberFormat.Format = MoneyFmt;
                dr++;
            }
            if (dr > dStart)
            {
                ws.Range(dStart, 7, dr - 1, 7).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Green)).LowestValue().HighestValue();
                Boxed(ws, top, 5, dr - 1, 8, dStart);
            }

            ws.SheetView.FreezeRows(top + 1);
        }

        // ПЕРСОНАЛ
        private static void BuildStaff(XLWorkbook wb, ReportData d)
        {
            var ws = wb.Worksheets.Add("Персонал");
            ws.ShowGridLines = false;
            double[] widths = { 30, 12, 18, 12, 18, 20 };
            for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];

            int r = 1;
            Section(ws, ref r, "Ефективність персоналу", 6);
            int head = r;
            Head(ws, r, new[] { "Майстер / менеджер", "Продажі", "Сума продажів", "Ремонти", "Сума ремонтів", "Разом" }, 1);
            r++; int start = r;
            foreach (var m in d.StaffPerformance)
            {
                ws.Cell(r, 1).Value = m.Name;
                ws.Cell(r, 2).Value = m.SalesCount; ws.Cell(r, 2).Style.NumberFormat.Format = IntFmt;
                ws.Cell(r, 3).Value = (double)m.SalesSum; ws.Cell(r, 3).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 4).Value = m.RepairsCount; ws.Cell(r, 4).Style.NumberFormat.Format = IntFmt;
                ws.Cell(r, 5).Value = (double)m.RepairsSum; ws.Cell(r, 5).Style.NumberFormat.Format = MoneyFmt;
                ws.Cell(r, 6).Value = (double)m.Total; ws.Cell(r, 6).Style.NumberFormat.Format = MoneyFmt; ws.Cell(r, 6).Style.Font.Bold = true;
                r++;
            }
            if (r > start)
            {
                ws.Range(start, 6, r - 1, 6).AddConditionalFormat().DataBar(XLColor.FromHtml(BrandAssets.Green)).LowestValue().HighestValue();
                Boxed(ws, head, 1, r - 1, 6, start);
                ws.Range(start, 1, r - 1, 6).SetAutoFilter();
            }
            ws.SheetView.FreezeRows(head + 1);
        }

        // ДОПОМІЖНІ
        private static void Section(IXLWorksheet ws, ref int row, string text, int lastCol, int col = 1)
        {
            var rng = ws.Range(row, col, row, lastCol);
            rng.Merge();
            var cell = ws.Cell(row, col);
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 12;
            cell.Style.Font.FontColor = XLColor.FromHtml(BrandAssets.Ink);
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF6EB");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thick;
            cell.Style.Border.LeftBorderColor = XLColor.FromHtml(BrandAssets.Green);
            ws.Row(row).Height = 22;
            row++;
        }

        private static void Head(IXLWorksheet ws, int row, string[] headers, int startCol)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(row, startCol + i);
                c.Value = headers[i];
                c.Style.Font.Bold = true;
                c.Style.Font.FontColor = XLColor.White;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandAssets.Ink);
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                c.Style.Alignment.Horizontal = i == 0 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
            }
            ws.Row(row).Height = 20;
        }

        // Рамка + зеброві рядки для діапазону тіла таблиці (firstBodyRow..lastRow)
        private static void Boxed(IXLWorksheet ws, int headerRow, int c1, int lastRow, int c2, int firstBodyRow)
        {
            var all = ws.Range(headerRow, c1, lastRow, c2);
            all.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            all.Style.Border.OutsideBorderColor = XLColor.FromHtml(BrandAssets.Ink2);
            all.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            all.Style.Border.InsideBorderColor = XLColor.FromHtml("#E6ECEF");
            for (int rr = firstBodyRow; rr <= lastRow; rr++)
                if ((rr - firstBodyRow) % 2 == 1)
                    ws.Range(rr, c1, rr, c2).Style.Fill.BackgroundColor = XLColor.FromHtml(BrandAssets.RowAlt);
        }
    }
}
