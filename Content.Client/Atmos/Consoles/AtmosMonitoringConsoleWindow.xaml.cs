using Content.Client.Pinpointer.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos.Components;
using Content.Shared.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client.Atmos.Consoles;

[GenerateTypedNameReferences]
public sealed partial class AtmosMonitoringConsoleWindow : FancyWindow
{
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _protoManager;
    private readonly SpriteSystem _spriteSystem;

    private EntityUid? _owner;
    private NetEntity? _focusEntity;
    private int? _focusNetId;

    private bool _autoScrollActive = false;

    private readonly Color _unfocusedDeviceColor = Color.DimGray;
    private ProtoId<NavMapBlipPrototype> _navMapConsoleProtoId = "NavMapConsole";
    private ProtoId<NavMapBlipPrototype> _gasPipeSensorProtoId = "GasPipeSensor";

    public AtmosMonitoringConsoleWindow(AtmosMonitoringConsoleBoundUserInterface userInterface, EntityUid? owner)
    {
        RobustXamlLoader.Load(this);
        _entManager = IoCManager.Resolve<IEntityManager>();
        _protoManager = IoCManager.Resolve<IPrototypeManager>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        // Pass the owner to nav map
        _owner = owner;
        NavMap.Owner = _owner;

        // Set nav map grid uid
        var stationName = Loc.GetString("atmos-monitoring-window-unknown-location");
        EntityCoordinates? consoleCoords = null;

        if (_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
        {
            consoleCoords = xform.Coordinates;
            NavMap.MapUid = xform.GridUid;

            // Assign station name      
            if (_entManager.TryGetComponent<MetaDataComponent>(xform.GridUid, out var stationMetaData))
                stationName = stationMetaData.EntityName;

            var msg = new FormattedMessage();
            msg.TryAddMarkup(Loc.GetString("atmos-monitoring-window-station-name", ("stationName", stationName)), out _);

            StationName.SetMessage(msg);
        }

        else
        {
            StationName.SetMessage(stationName);
            NavMap.Visible = false;
        }

        // Set trackable entity selected action
        NavMap.TrackedEntitySelectedAction += SetTrackedEntityFromNavMap;

        // Update nav map
        NavMap.ForceNavMapUpdate();

        // Set tab container headers
        MasterTabContainer.SetTabTitle(0, Loc.GetString("atmos-monitoring-window-tab-networks"));

        // Set UI toggles
        ShowPipeNetwork.OnToggled += _ => OnShowPipeNetworkToggled();
        ShowGasPipeSensors.OnToggled += _ => OnShowGasPipeSensors();

        // Set nav map colors
        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner, out var console))
            return;

        NavMap.TileColor = console.NavMapTileColor;
        NavMap.WallColor = console.NavMapWallColor;

        // Initalize
        UpdateUI(consoleCoords, Array.Empty<AtmosMonitoringConsoleEntry>());
    }

    #region Toggle handling

    private void OnShowPipeNetworkToggled()
    {
        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner.Value, out var console))
            return;

        NavMap.ShowPipeNetwork = ShowPipeNetwork.Pressed;

        foreach (var (netEnt, device) in console.AtmosDevices)
        {
            if (device.NavMapBlip == _gasPipeSensorProtoId)
                continue;

            if (ShowPipeNetwork.Pressed)
                AddTrackedEntityToNavMap(device);

            else
                NavMap.TrackedEntities.Remove(netEnt);
        }
    }

    private void OnShowGasPipeSensors()
    {
        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner.Value, out var console))
            return;

        foreach (var (netEnt, device) in console.AtmosDevices)
        {
            if (device.NavMapBlip != _gasPipeSensorProtoId)
                continue;

            if (ShowGasPipeSensors.Pressed)
                AddTrackedEntityToNavMap(device, true);

            else
                NavMap.TrackedEntities.Remove(netEnt);
        }
    }

    #endregion

    public void UpdateUI
        (EntityCoordinates? consoleCoords,
        AtmosMonitoringConsoleEntry[] atmosNetworks)
    {
        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner.Value, out var console))
            return;

        // Reset nav map values
        NavMap.TrackedCoordinates.Clear();
        NavMap.TrackedEntities.Clear();

        if (_focusEntity != null && !console.AtmosDevices.Any(x => x.Key == _focusEntity))
            ClearFocus();

        // Add tracked entities to the nav map
        UpdateNavMapBlips();

        // Show the monitor location
        var consoleNetEnt = _entManager.GetNetEntity(_owner);

        if (consoleCoords != null && consoleNetEnt != null)
        {
            var proto = _protoManager.Index(_navMapConsoleProtoId);

            if (proto.TexturePaths != null && proto.TexturePaths.Length != 0)
            {
                var texture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(proto.TexturePaths[0]));
                var blip = new NavMapBlip(consoleCoords.Value, texture, proto.Color, proto.Blinks, proto.Selectable);
                NavMap.TrackedEntities[consoleNetEnt.Value] = blip;
            }
        }

        // Update the nav map
        NavMap.ForceNavMapUpdate();

        // Clear excess children from the tables
        while (AtmosNetworksTable.ChildCount > atmosNetworks.Length)
            AtmosNetworksTable.RemoveChild(AtmosNetworksTable.GetChild(AtmosNetworksTable.ChildCount - 1));

        // Update all entries in each table
        for (int index = 0; index < atmosNetworks.Length; index++)
        {
            var entry = atmosNetworks.ElementAt(index);
            UpdateUIEntry(entry, index, AtmosNetworksTable, console);
        }
    }

    private void UpdateNavMapBlips()
    {
        if (_owner == null || !_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner.Value, out var console))
            return;

        if (NavMap.Visible)
        {
            foreach (var (netEnt, device) in console.AtmosDevices)
            {
                // Update the focus network ID, incase it has changed
                if (_focusEntity == netEnt)
                {
                    _focusNetId = device.NetId;
                    NavMap.FocusNetId = _focusNetId;
                }

                var isSensor = device.NavMapBlip == _gasPipeSensorProtoId;

                // Skip network devices if the toggled is off
                if (!ShowPipeNetwork.Pressed && !isSensor)
                    continue;

                // Skip gas pipe sensors if the toggle is off
                if (!ShowGasPipeSensors.Pressed && isSensor)
                    continue;

                AddTrackedEntityToNavMap(device, isSensor);
            }
        }
    }

    private void AddTrackedEntityToNavMap(AtmosDeviceNavMapData metaData, bool isSensor = false)
    {
        var proto = _protoManager.Index(metaData.NavMapBlip);

        if (proto.TexturePaths == null || proto.TexturePaths.Length == 0)
            return;

        var idx = Math.Clamp((int)metaData.Direction / 2, 0, proto.TexturePaths.Length - 1);
        var texture = proto.TexturePaths.Length > 0 ? proto.TexturePaths[idx] : proto.TexturePaths[0];
        var color = isSensor ? proto.Color : proto.Color * metaData.PipeColor;

        if (_focusNetId != null && metaData.NetId != _focusNetId)
            color *= _unfocusedDeviceColor;

        var blinks = proto.Blinks || _focusEntity == metaData.NetEntity;
        var coords = _entManager.GetCoordinates(metaData.NetCoordinates);
        var blip = new NavMapBlip(coords, _spriteSystem.Frame0(new SpriteSpecifier.Texture(texture)), color, blinks, proto.Selectable, proto.Scale);
        NavMap.TrackedEntities[metaData.NetEntity] = blip;
    }

    private void UpdateUIEntry(AtmosMonitoringConsoleEntry data, int index, Control table, AtmosMonitoringConsoleComponent console)
    {
        // Make new UI entry if required
        if (index >= table.ChildCount)
        {
            var newEntryContainer = new AtmosMonitoringEntryContainer(data);

            // On click
            newEntryContainer.FocusButton.OnButtonUp += args =>
            {
                if (_focusEntity == newEntryContainer.Data.NetEntity)
                {
                    ClearFocus();
                }

                else
                {
                    SetFocus(newEntryContainer.Data.NetEntity, newEntryContainer.Data.NetId);

                    var coords = _entManager.GetCoordinates(newEntryContainer.Data.Coordinates);
                    NavMap.CenterToCoordinates(coords);
                }

                // Update affected UI elements across all tables
                UpdateConsoleTable(console, AtmosNetworksTable, _focusEntity);
            };

            // Add the entry to the current table
            table.AddChild(newEntryContainer);
        }

        // Update values and UI elements
        var tableChild = table.GetChild(index);

        if (tableChild is not AtmosMonitoringEntryContainer)
        {
            table.RemoveChild(tableChild);
            UpdateUIEntry(data, index, table, console);

            return;
        }

        var entryContainer = (AtmosMonitoringEntryContainer)tableChild;
        entryContainer.UpdateEntry(data, data.NetEntity == _focusEntity);
    }

    private void UpdateConsoleTable(AtmosMonitoringConsoleComponent console, Control table, NetEntity? currTrackedEntity)
    {
        foreach (var tableChild in table.Children)
        {
            if (tableChild is not AtmosAlarmEntryContainer)
                continue;

            var entryContainer = (AtmosAlarmEntryContainer)tableChild;

            if (entryContainer.NetEntity != currTrackedEntity)
                entryContainer.RemoveAsFocus();

            else if (entryContainer.NetEntity == currTrackedEntity)
                entryContainer.SetAsFocus();
        }
    }

    private void SetTrackedEntityFromNavMap(NetEntity? focusEntity)
    {
        if (focusEntity == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner, out var console))
            return;

        foreach (var (netEnt, device) in console.AtmosDevices)
        {
            if (netEnt != focusEntity)
                continue;

            if (device.NavMapBlip != _gasPipeSensorProtoId)
                return;

            // Set new focus
            SetFocus(focusEntity.Value, device.NetId);

            // Get the scroll position of the selected entity on the selected button the UI
            ActivateAutoScrollToFocus();

            break;
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        AutoScrollToFocus();
    }

    private void ActivateAutoScrollToFocus()
    {
        _autoScrollActive = true;
    }

    private void AutoScrollToFocus()
    {
        if (!_autoScrollActive)
            return;

        var scroll = AtmosNetworksTable.Parent as ScrollContainer;
        if (scroll == null)
            return;

        if (!TryGetVerticalScrollbar(scroll, out var vScrollbar))
            return;

        if (!TryGetNextScrollPosition(out float? nextScrollPosition))
            return;

        vScrollbar.ValueTarget = nextScrollPosition.Value;

        if (MathHelper.CloseToPercent(vScrollbar.Value, vScrollbar.ValueTarget))
            _autoScrollActive = false;
    }

    private bool TryGetVerticalScrollbar(ScrollContainer scroll, [NotNullWhen(true)] out VScrollBar? vScrollBar)
    {
        vScrollBar = null;

        foreach (var control in scroll.Children)
        {
            if (control is not VScrollBar)
                continue;

            vScrollBar = (VScrollBar)control;

            return true;
        }

        return false;
    }

    private bool TryGetNextScrollPosition([NotNullWhen(true)] out float? nextScrollPosition)
    {
        nextScrollPosition = null;

        var scroll = AtmosNetworksTable.Parent as ScrollContainer;
        if (scroll == null)
            return false;

        var container = scroll.Children.ElementAt(0) as BoxContainer;
        if (container == null || container.Children.Count() == 0)
            return false;

        // Exit if the heights of the children haven't been initialized yet
        if (!container.Children.Any(x => x.Height > 0))
            return false;

        nextScrollPosition = 0;

        foreach (var control in container.Children)
        {
            if (control is not AtmosMonitoringEntryContainer)
                continue;

            var entry = (AtmosMonitoringEntryContainer)control;

            if (entry.Data.NetEntity == _focusEntity)
                return true;

            nextScrollPosition += control.Height;
        }

        // Failed to find control
        nextScrollPosition = null;

        return false;
    }

    private void SetFocus(NetEntity focusEntity, int focusNetId)
    {
        _focusEntity = focusEntity;
        _focusNetId = focusNetId;
        NavMap.FocusNetId = focusNetId;

        OnFocusChanged();
    }

    private void ClearFocus()
    {
        _focusEntity = null;
        _focusNetId = null;
        NavMap.FocusNetId = null;

        OnFocusChanged();
    }

    private void OnFocusChanged()
    {
        UpdateNavMapBlips();
        NavMap.ForceNavMapUpdate();

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(_owner, out var console))
            return;

        for (int index = 0; index < AtmosNetworksTable.ChildCount; index++)
        {
            var entry = (AtmosMonitoringEntryContainer)AtmosNetworksTable.GetChild(index);

            if (entry == null)
                continue;

            UpdateUIEntry(entry.Data, index, AtmosNetworksTable, console);
        }
    }
}