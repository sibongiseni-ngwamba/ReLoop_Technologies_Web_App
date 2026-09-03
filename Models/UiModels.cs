namespace ReLoop_Technologies_Web_App.Models;

public sealed record UiButtonModel(
    string Text,
    string? Href = null,
    string Variant = "primary",
    bool IsSubmit = false,
    bool FullWidth = false,
    string? Icon = null,
    string? AriaLabel = null);

public sealed record UiBadgeModel(
    string Text,
    string Tone = "neutral");

public sealed record UiInputOption(
    string Value,
    string Text,
    bool Selected = false);

public sealed record UiInputModel(
    string Label,
    string Name,
    string Type = "text",
    string? Placeholder = null,
    string? Value = null,
    string? HelpText = null,
    string? ErrorText = null,
    bool Multiline = false,
    bool Required = false,
    IReadOnlyList<UiInputOption>? Options = null);

public sealed record UiStateModel(
    string Title,
    string Message,
    string? ActionText = null,
    string? ActionHref = null,
    string? Tone = null);

public sealed record UiTableColumnModel(
    string Header,
    string? Align = null,
    string? Width = null);

public sealed record UiTableCellModel(
    string Text,
    string? BadgeTone = null,
    bool Strong = false,
    string? Align = null);

public sealed record UiTableRowModel(
    IReadOnlyList<UiTableCellModel> Cells);

public sealed record UiTableModel(
    string Title,
    IReadOnlyList<UiTableColumnModel> Columns,
    IReadOnlyList<UiTableRowModel> Rows,
    string EmptyTitle,
    string EmptyMessage);
