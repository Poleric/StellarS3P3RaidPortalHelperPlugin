using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Stellar.Abstractions.Domain;
using Stellar.Abstractions.Plugins;
using Stellar.Abstractions.Services;

namespace Stellar.RaidPortalHelper;

enum TileLocation
{
    TopLeft,
    Top,
    TopRight,
    BottomLeft,
    Bottom,
    BottomRight
}

static class TileLocationExtensions
{
    // { "x": -20, "y": 400.41977, "z": -14.5 }
    // { "x": 20, "y": 400.41977, "z": -15 }
    // { "x": -20, "y": 400.41977, "z": 15 }
    // { "x": 20, "y": 400.41977, "z": 15 }
    static readonly Dictionary<PointF, TileLocation> Coordinates = new()
    {
        { new Point(20, 15), TileLocation.TopLeft },
        { new Point(20, 0), TileLocation.Top },
        { new Point(20, -15), TileLocation.TopRight },
        { new Point(-20, 15), TileLocation.BottomLeft },
        { new Point(-20, 0), TileLocation.Bottom },
        { new Point(-20, -15), TileLocation.BottomRight }
    };

    public static TileLocation GetNearestTileLocation(PointF point)
    {
        return Coordinates.OrderBy(r =>
        {
            var dx = point.X - r.Key.X;
            var dy = point.Y - r.Key.Y;

            return dx * dx + dy * dy;
        }).Select(r => r.Value).First();
    }
}

public sealed partial class Plugin : IStellarPlugin
{
    public string Name => "S3P3RaidPortalHelper";
    private const string ClashSceneIds = "13021";
    private const string BrutalSceneIds = "13022";
    private const string PurgeSceneIds = "13023";
    private const string ScenePosId = "109525";
    private const int BuffId1 = 829372;
    private const int BuffId2 = 829373;
    private const int BuffId3 = 829374;
    private const int BuffId4 = 829375;

    private TileLocation? _currentLocation;
    private Dictionary<TileLocation, int> _portals = new();

    private readonly IPluginServices _services;
    private readonly IWindowControl _hud;

    public Plugin(IPluginServices services)
    {
        _services = services;
        _services.Log.Info($"[{Name}] plugin constructed.");

        _hud = services.Windows.Register(new WindowRegistration(
            new WindowSpec(
                Id: "raidportalhelper.main",
                Title: "RaidPortalHelper",
                DefaultRect: new WindowRect(100f, 100f, 800f, 600f),
                Category: WindowCategory.HUD,
                Style: WindowPanelStyle.Borderless)
            {
                StartVisible = true,
                Closable = false,
                Draggable = true,
                EditModeDragOnly = true,
                ShouldRender = () => services.ClientState.Phase == GamePhase.World
                                     && (services.ClientState.UiState & (GameUIState.Blocking | GameUIState.AnyMenu)) ==
                                     0,
                Resizable = true,
                MinWidth = 150f, MinHeight = 100f
            },
            BuildRoot()
        ));

        services.Framework.Update += OnUpdate;
        services.CombatEvents.CombatEventOccurred += OnCombatEvent;
    }

    private void OnUpdate(float deltaTime)
    {
        if (_services.PlayerState.IsAvailable)
        {
            return;
        }

        var pos = _services.PlayerState.Position;
        _currentLocation = TileLocationExtensions.GetNearestTileLocation(new PointF(pos.X, pos.Z));
    }

    private void OnCombatEvent(CombatEvent combatEvent)
    {
        if (combatEvent is not CombatEvent.BuffChanged buffChanged)
            return;

        switch (buffChanged.Kind)
        {
            case BuffChangeKind.Applied:
                switch (buffChanged.BaseId)
                {
                    case BuffId1:
                        StorePortal(buffChanged.TargetId, BuffId1, 1);
                        break;
                    case BuffId2:
                        StorePortal(buffChanged.TargetId, BuffId2, 2);
                        break;
                    case BuffId3:
                        StorePortal(buffChanged.TargetId, BuffId3, 3);
                        break;
                    case BuffId4:
                        StorePortal(buffChanged.TargetId, BuffId4, 4);
                        break;
                }

                break;
            case BuffChangeKind.Removed:
                switch (buffChanged.BaseId)
                {
                    case BuffId1:
                        ClearPortal(1);
                        break;
                    case BuffId2:
                        ClearPortal(2);
                        break;
                    case BuffId3:
                        ClearPortal(3);
                        break;
                    case BuffId4:
                        ClearPortal(4);
                        break;
                }

                break;
        }
    }

    private void StorePortal(EntityId entity, int buffId, int portalIndex)
    {
        var firerId = GetFirerId(entity, buffId);
        _services.EntityTransforms.TryGetTransform(firerId, out var position, out _);
        _services.Log.Info($"[{Name}] Teleport ${portalIndex}: ${position.X}, ${position.Y}, ${position.Z}");
        _portals[TileLocationExtensions.GetNearestTileLocation(new PointF(position.X, position.Z))] = portalIndex;
    }

    private void ClearPortal(int portalIndex)
    {
        var kvp = _portals.First(kvp => kvp.Value == portalIndex);
        _portals.Remove(kvp.Key);
    }

    private EntityId GetFirerId(EntityId entityId, int buffId)
    {
        return _services.CombatLookup.BuffsFor(entityId)
            .Where(buff => buff.BaseId == buffId)
            .Select(buff => buff.FirerId)
            .First();
    }

    public void Dispose()
    {
        _services.Framework.Update -= OnUpdate;
        _services.CombatEvents.CombatEventOccurred -= OnCombatEvent;
        _hud.Remove();
    }
}