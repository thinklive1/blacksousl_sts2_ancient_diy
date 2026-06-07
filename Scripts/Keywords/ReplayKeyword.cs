using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace BlackSouls.Scripts;
[RegisterOwnedCardKeyword(nameof(Replay), IconPath = "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(ForceDeath), IconPath = "res://bs_ancient/assets/images/relics/StageEndRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(Kill), IconPath = "res://bs_ancient/assets/images/relics/NodeRibbonRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(Encore), IconPath = "res://bs_ancient/assets/images/relics/StagnantGearRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
public class MyKeywords
{
    public static readonly CardKeyword Replay = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Replay)).GetModCardKeyword();
    public static readonly CardKeyword ForceDeath = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(ForceDeath)).GetModCardKeyword();
    public static readonly CardKeyword Kill = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Kill)).GetModCardKeyword();
    public static readonly CardKeyword Encore = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Encore)).GetModCardKeyword();
}
