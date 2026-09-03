using Microsoft.AspNetCore.Mvc.RazorPages;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Pages;

public class DesignSystemModel : PageModel
{
    public UiTableModel SampleTable { get; private set; } = default!;

    public void OnGet()
    {
        SampleTable = new UiTableModel(
            "Pickup ledger preview",
            new[]
            {
                new UiTableColumnModel("ID", Width: "14%"),
                new UiTableColumnModel("Date", Width: "18%"),
                new UiTableColumnModel("Collection address"),
                new UiTableColumnModel("Waste type", Width: "18%"),
                new UiTableColumnModel("Status", Width: "16%", Align: "center")
            },
            new[]
            {
                new UiTableRowModel(new[]
                {
                    new UiTableCellModel("#LP-9082", Strong: true),
                    new UiTableCellModel("12 May 2025, 09:00 AM"),
                    new UiTableCellModel("452 Eco Circular Ave, Suite 3B"),
                    new UiTableCellModel("Recyclables", Strong: true),
                    new UiTableCellModel("Scheduled", BadgeTone: "info", Align: "center")
                }),
                new UiTableRowModel(new[]
                {
                    new UiTableCellModel("#LP-8931", Strong: true),
                    new UiTableCellModel("08 May 2025, 02:30 PM"),
                    new UiTableCellModel("452 Eco Circular Ave, Suite 3B"),
                    new UiTableCellModel("Organic", Strong: true),
                    new UiTableCellModel("Completed", BadgeTone: "success", Align: "center")
                }),
                new UiTableRowModel(new[]
                {
                    new UiTableCellModel("#LP-8521", Strong: true),
                    new UiTableCellModel("28 Apr 2025, 10:00 AM"),
                    new UiTableCellModel("710 Greenwood Terrace"),
                    new UiTableCellModel("E-waste", Strong: true),
                    new UiTableCellModel("Cancelled", BadgeTone: "danger", Align: "center")
                })
            },
            "No pickups available",
            "Once the collection layer is wired up, this table will show live results, filters, and status changes.");
    }
}
