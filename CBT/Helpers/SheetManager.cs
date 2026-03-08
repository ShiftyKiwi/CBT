namespace CBT.Helpers;

using System.Collections.Concurrent;
using System.Collections.Generic;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using S = System;

/// <summary>
/// ActionManager accesses the Dalamud DataManager.
/// </summary>
public unsafe class SheetManager : S.IDisposable
{
    private static readonly ExcelSheet<Action>? LuminaActionSheet = Service.DataManager.GetExcelSheet<Action>();
    private static readonly ExcelSheet<Status>? LuminaStatusSheet = Service.DataManager.GetExcelSheet<Status>();
    private static readonly ExcelSheet<Item>? LuminaItemSheet = Service.DataManager.GetExcelSheet<Item>();

    private readonly Dictionary<int, Action?> actionCache = [];
    private readonly Dictionary<int, Status?> statusCache = [];
    private readonly Dictionary<int, Item?> itemCache = [];
    private readonly ConcurrentDictionary<int, byte> loggedInvalidStatusIds = [];

    /// <inheritdoc/>
    public void Dispose()
    {
        this.actionCache.Clear();
        this.statusCache.Clear();
        this.itemCache.Clear();

        S.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get an Icon ID for the given actionID.
    /// </summary>
    /// <param name="actionID">Action ID.</param>
    /// <returns>Icon ID.</returns>
    public ushort GetIconForAction(int actionID)
         => this.actionCache.TryGetValue(actionID, out var action) ? action?.Icon ?? 0 : this.GetActionRow(actionID)?.Icon ?? 0;

    /// <summary>
    /// Get an Icon ID for the given value.
    /// </summary>
    /// <param name="value1">Status ID.</param>
    /// <returns>Icon ID.</returns>
    public uint GetIconForStatus(int value1)
         => this.statusCache.TryGetValue(value1, out var status) ? status?.Icon ?? 0 : (this.GetStatusRow(value1)?.Icon ?? 0);

    /// <summary>
    /// Get an Item ID for the given value.
    /// </summary>
    /// <param name="value1">Item ID.</param>
    /// <returns>Icon ID.</returns>
    public ushort GetIconForItem(int value1)
         => this.itemCache.TryGetValue(value1, out var item) ? item?.Icon ?? 0 : (this.GetItemRow(value1)?.Icon ?? 0);

    /// <summary>
    /// Get the Ability Name for the given actionID.
    /// </summary>
    /// <param name="actionID">Action ID.</param>
    /// <returns>Ability name.</returns>
    public ReadOnlySeString GetNameForAction(int actionID)
         => this.actionCache.TryGetValue(actionID, out var action) ? action?.Name ?? string.Empty : this.GetActionRow(actionID)?.Name ?? string.Empty;

    /// <summary>
    /// Get the Status Name for the given status ID.
    /// </summary>
    /// <param name="value1">Status ID.</param>
    /// <returns>Ability name.</returns>
    public ReadOnlySeString GetNameForStatus(int value1)
        => this.statusCache.TryGetValue(value1, out var status) ? status?.Name ?? string.Empty : this.GetStatusRow(value1)?.Name ?? string.Empty;

    /// <summary>
    /// Get the Item Name for the given value.
    /// </summary>
    /// <param name="value1">Item ID.</param>
    /// <returns>Item name.</returns>
    public ReadOnlySeString GetNameForItem(int value1)
         => this.itemCache.TryGetValue(value1, out var item) ? item?.Name ?? string.Empty : this.GetItemRow(value1)?.Name ?? string.Empty;

    private Action? GetActionRow(int actionID)
    {
        if (this.actionCache.TryGetValue(actionID, out var cachedAction))
        {
            return cachedAction;
        }

        if (actionID < 0)
        {
            this.actionCache[actionID] = null;
            return null;
        }

        try
        {
            var row = LuminaActionSheet?.GetRow((uint)actionID);
            this.actionCache[actionID] = row;
            return row;
        }
        catch (S.ArgumentOutOfRangeException)
        {
            this.actionCache[actionID] = null;
            return null;
        }
    }

    private Status? GetStatusRow(int value1)
    {
        if (this.statusCache.TryGetValue(value1, out var cachedStatus))
        {
            return cachedStatus;
        }

        if (value1 < 0)
        {
            this.LogInvalidStatusIdOnce(value1);
            this.statusCache[value1] = null;
            return null;
        }

        try
        {
            var row = LuminaStatusSheet?.GetRow((uint)value1);
            this.statusCache[value1] = row;
            return row;
        }
        catch (S.ArgumentOutOfRangeException)
        {
            this.LogInvalidStatusIdOnce(value1);
            this.statusCache[value1] = null;
            return null;
        }
    }

    private Item? GetItemRow(int value1)
    {
        if (this.itemCache.TryGetValue(value1, out var cachedItem))
        {
            return cachedItem;
        }

        if (value1 < 0)
        {
            this.itemCache[value1] = null;
            return null;
        }

        try
        {
            var row = LuminaItemSheet?.GetRow((uint)value1);
            this.itemCache[value1] = row;
            return row;
        }
        catch (S.ArgumentOutOfRangeException)
        {
            this.itemCache[value1] = null;
            return null;
        }
    }

    private void LogInvalidStatusIdOnce(int value1)
    {
        if (this.loggedInvalidStatusIds.TryAdd(value1, 0))
        {
            Service.PluginLog.Debug($"Ignoring invalid status rowId {value1} (uint: {(uint)value1}).");
        }
    }
}
