using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Blacksouls.Scripts;
[RegisterOwnedCardKeyword(nameof(Replay), IconPath = "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(ForceDeath), IconPath = "res://bs_ancient/assets/images/relics/StageEndRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(Kill), IconPath = "res://bs_ancient/assets/images/relics/NodeRibbonRelic.png", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
// [RegisterOwnedCardKeyword(nameof(Unique2), IconPath = "res://icon.svg")] // 如果要加更多关键词，添加特性
public class MyKeywords
{
    public static readonly string Replay = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Replay));
    public static readonly string ForceDeath = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(ForceDeath));
    public static readonly string Kill = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Kill));
    // public static readonly string Unique2 = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Unique2));
}
