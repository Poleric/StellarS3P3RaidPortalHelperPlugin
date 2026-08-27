using Stellar.Abstractions.Services;

namespace Stellar.RaidPortalHelper;

public sealed partial class Plugin
{
    private HudElement BuildRoot() => new ConditionalElement(
        // TODO: check for ScenePosId as well
        When: () => _services.ClientState.CurrentSceneName is ClashSceneIds or BrutalSceneIds or PurgeSceneIds,
        Then: new ColumnElement(
            Gap: 16f,
            Children:
            [
                new RowElement(
                    Gap: 8f,
                    Children:
                    [
                        BuildTile(TileLocation.TopLeft),
                        BuildTile(TileLocation.Top),
                        BuildTile(TileLocation.TopRight),
                    ]),
                new RowElement(
                    Gap: 8f,
                    Children:
                    [
                        BuildTile(TileLocation.BottomLeft),
                        BuildTile(TileLocation.Bottom),
                        BuildTile(TileLocation.BottomRight),
                    ])
            ]),
        Else: new TextElement(() => "Raid boss 3 not loaded."),
        Fill: true
    );

    private HudElement BuildTile(TileLocation tileLocation) => new CellElement(
        Weight: 1f,
        Child: Expand(new PanelElement(
            Expand(new TextElement(
                () =>
                {
                    var text = "";
                    if (_currentLocation == tileLocation)
                        text += ">";
                    if (_portals.TryGetValue(tileLocation, out var order))
                        text += order.ToString();
                    return text;
                },
                Emphasis: true,
                Shadow: true,
                FontSize: 36,
                Align: TextAlign.Center
            ))
        ))
    );

    private static HudElement Expand(HudElement child) => new ConditionalElement(
        When: () => true,
        Then: child,
        Fill: true
    );
}