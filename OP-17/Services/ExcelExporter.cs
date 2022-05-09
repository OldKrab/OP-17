using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using OP_17.ViewModels;

namespace OP_17.Services;

public class ExcelExporter
{
    public static string TemplateFile = "resources/template.xlsx";

    public string Export(MainViewModel mainVM, string fileName = "")
    {
        if (fileName == "")
            fileName = GenerateFileName();
        CreateAndOpenFile();

        FillFields(mainVM);
        _workbook.SaveAs(fileName);
        _workbook.Dispose();
        return fileName;
    }

    private string GenerateFileName()
    {
        string GetFileName(int i) => "resources/" + i + ".xlsx";
        int i = 1;
        while (File.Exists(GetFileName(i)))
            i++;
        return GetFileName(i);
    }

    private void FillFields(MainViewModel mainVM)
    {
        _fields["companyName"].Value = mainVM.CompanyName;
        _fields["companyOKPO"].Value = mainVM.CompanyOKPO;
        _fields["companyUnit"].Value = mainVM.CompanyUnit;
        _fields["companyOKDP"].Value = mainVM.CompanyOKDP;
        _fields["operation"].Value = mainVM.DocumentOperation;
        _fields["docNumber"].Value = Convert.ToInt32(mainVM.DocumentNumber);
        _fields["docDate"].Value = mainVM.DocumentDateTime.ToString("dd.MM.yyyy");
        _fields["startDate"].Value = mainVM.StartDate?.ToString("dd.MM.yyyy");
        _fields["endDate"].Value = mainVM.EndDate?.ToString("dd.MM.yyyy");

        foreach (var (field, product) in GetMergedCells(_fields["products"]).Zip(mainVM.Products))
            field.Value = product;
        foreach (var (field, saleDate) in GetMergedCells(_fields["salesDates"]).Zip(mainVM.SalesDates))
            field.Value = saleDate;

        int curRow = 26;
        foreach (var dishVM in mainVM.Dishes)
        {
            FillDish(dishVM, curRow);
            curRow++;
        }

        foreach (var (field, sale) in GetMergedCells(_fields["summarySales"]).Zip(mainVM.SummarySales))
            field.Value = sale;
        _fields["summaryAllSales"].Value = mainVM.SummaryAllSales;
        _fields["summaryAllPrice"].Value = mainVM.SummaryAllPrice;
        foreach (var (field, productCount) in GetMergedCells(_fields["summaryAllProductCounts"]).Where((_, i) => i % 2 == 1).Zip(mainVM.SummaryAllProductCounts))
            field.Value = productCount;

        _fields["formerPost"].Value = mainVM.SignatureVM?.FormerPost;
        _fields["former"].Value = mainVM.SignatureVM?.Former;
        _fields["productionHead"].Value = mainVM.SignatureVM?.ProductionHead;
        _fields["companyHeadPost"].Value = mainVM.SignatureVM?.CompanyHeadPost;
        _fields["companyHead"].Value = mainVM.SignatureVM?.CompanyHead;
    }

    private void FillDish(DishViewModel dishVM, int row)
    {
        var rowCells = GetMergedCells(_sheet.Range($"A{row}:CC{row}")).ToList();
        rowCells[0].Value = dishVM.Card;
        rowCells[1].Value = dishVM.Name;
        rowCells[2].Value = dishVM.Code;
        for (int i = 0; i < 5; i++)
            rowCells[3 + i].Value = dishVM.Sales[i];
        rowCells[8].Value = dishVM.AllSales;
        rowCells[9].Value = dishVM.Price;
        rowCells[10].Value = dishVM.AllPrice;
        for (int i = 0; i < 5; i++)
        {
            rowCells[11 + 2 * i].Value = dishVM.ProductsCounts[i];
            rowCells[12 + 2 * i].Value = dishVM.AllProductCounts[i];
        }
    }

    private void CreateAndOpenFile()
    {
        var templateFile = new FileInfo(TemplateFile);
        _workbook = new XLWorkbook(TemplateFile);
        _sheet = _workbook.Worksheets.First();

        _fields["companyName"] = GetField("A6");
        _fields["companyOKPO"] = GetField("BY6");
        _fields["companyUnit"] = GetField("A8");
        _fields["companyOKDP"] = GetField("BY9");
        _fields["operation"] = GetField("BY10");
        _fields["docNumber"] = GetField("AX13");
        _fields["docDate"] = GetField("BF13");
        _fields["startDate"] = GetField("BN13");
        _fields["endDate"] = GetField("BS13");
        _fields["products"] = GetField("AS18:CF19");
        _fields["salesDates"] = GetField("R18:AD19");
        _fields["salesDates"].Style.NumberFormat.Format = "dd.mm";

        _fields["summarySales"] = GetField("R37:AD37");
        _fields["summaryAllSales"] = GetField("AG37");
        _fields["summaryAllPrice"] = GetField("AO37");
        _fields["summaryAllProductCounts"] = GetField("AS37:CC37");

        _fields["formerPost"] = GetField("J38");
        _fields["former"] = GetField("AB38");
        _fields["productionHead"] = GetField("BP38");
        _fields["companyHeadPost"] = GetField("R40");
        _fields["companyHead"] = GetField("AP40");

    }

    private IXLRange GetField(string fieldAddr, XLAlignmentHorizontalValues horizontalAlignment = XLAlignmentHorizontalValues.Center)
    {
        var field = _sheet.Range(fieldAddr);
        field.Style.Alignment.Horizontal = horizontalAlignment;
        return field;
    }

    private IEnumerable<IXLRange> GetMergedCells(IXLRange range)
    {
        var rangeCells = range.Cells().ToList();
        var mergedRanges =  _sheet.MergedRanges.Where(mc => rangeCells.Intersect(mc.Cells()).Count() != 0).ToList();
        var notMergedRanges = rangeCells.Except(mergedRanges.SelectMany(mc => mc.Cells())).Select(c=>c.AsRange());
        return mergedRanges.Concat(notMergedRanges).OrderBy(r=>r.RangeAddress.FirstAddress.ColumnNumber);
    }


    private XLWorkbook _workbook = null!;
    private IXLWorksheet _sheet = null!;
    private readonly Dictionary<string, IXLRange> _fields = new();
}


