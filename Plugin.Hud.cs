using Stellar.Abstractions.Services;

namespace Stellar.RaidPortalHelper;

public sealed partial class Plugin
{
    private HudElement BuildTile(TileLocation tileLocation) => new CellElement(
        Weight: 1f,
        Child: new ConditionalElement(
            When: () => true,
            Then: new PanelElement(
                new ConditionalElement(
                    When: () => _portals.ContainsKey(tileLocation),
                    Then: new TextElement(
                        () => _portals[tileLocation].ToString(),
                        Align: TextAlign.Center
                    )
                )
            ),
            Fill: true
        )
    );
}