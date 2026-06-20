# BS Ancient

Note: this English README and the in-game English localization are machine-translated.

A Slay the Spire 2 Ancient expansion mod. It currently adds 3 map Ancients, replaces the starting Neow appearance with Grand Guignol, and adds a separate "Grand Guignol?" compendium category for starting Ancient relics.

## Requirements

- RitsuLib 0.4.10 or later

## Ancient Spawn Rules

- Node: appears in Act 2.
- Prickett: appears in Act 3.
- Mabel: appears in Act 2 or Act 3, at most once per run.
- Grand Guignol: replaces Neow's starting appearance and text.
- Grand Guignol?: compendium-only category. It does not appear on the map.
- If "Only Use Mod Ancients" is enabled, Act 2 randomly chooses between Node and Mabel, while Act 3 randomly chooses between Prickett and Mabel.

## Settings

These settings can be changed in the in-game mod settings UI, or by editing `bs_ancient_config.cfg` in the mod folder. The UI and config file use the same options; pressing Save in the UI writes them back to the config file.

`bs_ancient_config.cfg` is a JSON config file. Default values:

```json
{
  "OnlyUseModAncients": true,
  "DisableModAncients": false,
  "ReplaceNeowAppearance": true,
  "EnableModEvents": true,
  "DisableTestingEvents": true,
  "EnableFairyTaleMode": false,
  "GrandGuignolInitialRelicChance": 30
}
```

- `OnlyUseModAncients`: whether only this mod's map Ancients can spawn. If `true`, Act 2 only chooses from Node/Mabel and Act 3 only chooses from Prickett/Mabel. If `false`, the new Ancients enter the candidate pool without blocking vanilla or other modded Ancients. Requires restarting the game and starting a new run.
- `DisableModAncients`: completely disables this mod's map Ancients. If `true`, Node, Prickett, and Mabel will not appear on the map. This takes priority over `OnlyUseModAncients`. Requires restarting the game and starting a new run.
- `ReplaceNeowAppearance`: replaces Neow's appearance, name, title, and related dialogue with Grand Guignol. If `false`, Neow display returns to vanilla. Requires restarting the game.
- `EnableModEvents`: enables this mod's random events. If `false`, this mod's new random events will not enter the event pool. Requires restarting the game and starting a new run.
- `DisableTestingEvents`: disables testing events. If `true`, SAN/hand-mirror related testing events such as Clown and Girl in the Maze will not appear naturally. Requires restarting the game and starting a new run.
- `EnableFairyTaleMode`: enables Fairy Tale Mode. If `true`, you start with Unnamed Fairy Tale Book. After every 7 non-Boss/non-Ancient nodes, it grants a random Fairy Tale. Duplicates are allowed. Requires restarting the game and starting a new run.
- `GrandGuignolInitialRelicChance`: chance, from 0 to 100, for a Grand Guignol starting relic to replace a positive starting option. Default is 30. Requires restarting the game and starting a new run.

## Grand Guignol?

This category displays starting Ancient relics. These relics are not shown in the event relic pool category.

- Re-Thinking Poker: may appear in Grand Guignol's starting options. Gain the Gold left over from the previous run, then choose 1 card from up to 3 valid vanilla cards from the previous deck. It will not appear without a valid previous run, if the previous run was not a vanilla character, or in multiplayer.
- Caterpillar's Smoke: may appear in Grand Guignol's starting options. At rest sites, you may spend max HP to choose and upgrade 1 card without consuming the rest-site option. Up to 3 uses; costs 3/5/8 max HP.
- Duchess' Menu: may appear in Grand Guignol's starting options. At card rewards, you may choose to supply the reward. After 3 supplies, gain a reward based on the total quality score of the supplied card sets. Common cards are 1 point, uncommon cards 2 points, rare cards 3 points. 12 or less: 200 Gold and 1 uncommon card reward. 13-18: 300 Gold and 1 rare card reward. 19 or more: 500 Gold and 2 rare card rewards. If you obtain Driftwood, this relic is disabled.
- Angel's Feather: may appear in Grand Guignol's starting options. The first time you take HP damage each combat, gain that much Vigor. When entering Act 2, it becomes Angel's Feather?. Angel's Feather? grants Vigor whenever you take HP damage. When entering Act 3, it becomes Brutalizing Angel's Feather. Brutalizing Angel's Feather grants Vigor whenever you take HP damage; after the first time you are damaged each combat, attacks that deal HP damage heal you, up to the amount of that first damage. A power icon shows the remaining heal amount.
- Mabel's Soldier Piece: may appear in Grand Guignol's starting options. During Act 1, all enchantable Common and Uncommon cards in card rewards are enchanted with Ascension. After 7 nodes, an Ascension card transforms into a random card of the next higher rarity: Common to Uncommon, Uncommon to Rare. Rare cards are not enchanted, so this relic does not Ascend cards into Ancient cards.

## Node

Options:

- Option 1: Time Queen's Blessing / Winter Bell Covenant / White Rabbit's Hairband / Stagnant Gear
- Option 2: Dream of Kadath / Unicorn Royal Crest / Lion Royal Crest
- Option 3: White Rabbit's Oath / Fairy-Tale Writer's Quill Pen / Pet Cat's Collar. Pet Cat's Collar will not appear if the deck does not have 2 transformable cards or if you already have a pet.
- Option 4: White Queen's Soldier Piece

Relics:

- Time Queen's Blessing: add Rehearsal to 1 card in your deck. While a Rehearsal card is in the exhaust pile, it is automatically played again at the start of turn, up to 3 times per combat.
- Winter Bell Covenant: at the start of combat, shuffle Gerda, Florence, and Ghost Hunter into the draw pile. When drawn, Gerda grants 1 Undying and exhausts; Florence grants 3 Regen, draws 3 cards, and exhausts; Ghost Hunter grants 3 Strength, draws 3 cards, and exhausts.
- Stagnant Gear: on pickup, gain 1 matching card. Stagnant Gear is a 1-cost Ancient Skill with Exhaust. Choose 1 card in hand and add Encore to it; upgraded version has Retain. After you manually play an Encore card this turn, at the start of next turn it is automatically played a number of times equal to how many times it has been played this combat. Automatic plays count as plays, but do not independently trigger Encore for the next turn.
- Pet Cat's Collar: on pickup, choose 2 cards in your deck to transform into Cheshire Cat's Smile and Dinah's Bite. Smile is a 0-cost Skill that grants 4 Block, upgraded to 6; every 2 triggers grants 1 Intangible. Bite is a 0-cost Attack that removes all enemy Block and deals 15 damage, upgraded to 20. The two cat cards can trigger at most once total each turn.
- Dream of Kadath: rest sites only allow resting. Resting heals an additional 30% max HP and randomly upgrades 3 cards.
- Fairy-Tale Writer's Quill Pen: gain 1 Power of Rewrite. Power of Rewrite is a 3-cost Attack with Retain. It deals 39 damage, removes all debuffs from you, and exhausts; upgraded version deals 48 damage.
- White Rabbit's Hairband: at end of turn, if the enemy's total attack damage is at least your current HP, Block, Plated Armor, and Osty current HP combined, immediately gain 1 extra turn. Once per combat.
- Unicorn Royal Crest: gain 2 Dexterity at the start of combat. Whenever an enemy attack deals no HP damage to you, immediately deal equal counterattack damage to that enemy.
- Lion Royal Crest: enemies cannot gain Block, Buffer, Plated Armor, or Intangible. Whenever an enemy gains a positive power, gain 1 Strength.
- White Rabbit's Oath: at the start of turn, if your HP is below 50%, gain 1 Energy. The first time this triggers each combat, gain 3 Regen.
- White Queen's Soldier Piece: changes the first 5 map rows of the current act to 4 normal combats and 1 elite combat, preserving the original paths. After completing all trials, promotes into White Queen.
- White Queen: you may freely choose the next map node any number of times.

## Prickett

Options:

- Option 1: Red Queen's Signed Album / Prickett's Dice / Jabberwock's Film Reel
- Option 2: Alice's Ribbon / Prickett's Looking Glass / Killer's Film Reel. Prickett's Looking Glass will not appear in multiplayer.
- Option 3: Prickett's Oath / 蟇?ｋ蟆大･ｳ縺溘 / Fairy-Tale Writer's Quill Pen. Default odds are 45% / 10% / 45%; if your deck has at least 3 curses, odds become 10% / 80% / 10%.
- Option 4: Red Queen's Soldier Piece

Relics:

- Red Queen's Signed Album: at the start of each turn, randomly reduce the cost of 1 current hand card by 1 for this combat.
- Prickett's Dice: at the start of each turn, gain 1 Energy. During combat, all cards' attack and block values randomly fluctuate by up to 3. If at least one third of eligible cards roll +3, gain 1 Big Success, once per run.
- Big Success: 0-cost Ethereal Power card. At the end of combat, gain 1 rare card reward and may remove 1 card.
- Jabberwock's Film Reel: gain Jabberwock at the start of combat. Jabberwock cannot gain Block; attacks heal HP equal to unblocked damage dealt; starting on turn 2, lose half your current HP at the start of each turn, but this cannot kill you.
- Alice's Ribbon: one-use relic. When you die, revive to full HP and gain 10 Strength.
- Prickett's Looking Glass: gain 1 Red Queen's Looking Glass. Red Queen's Looking Glass is a 0-cost Ancient Skill with Ethereal. Choose to remove the discard pile and copy the draw pile into it, or remove the draw pile and copy the discard pile into it. Removed cards do not enter the exhaust pile and do not trigger exhaust effects. Upgraded version removes Ethereal.
- 蟇?ｋ蟆大･ｳ縺溘: transform all curses in your deck into 雋ｴ譁ｹ繧呈?縺. 雋ｴ譁ｹ繧呈?縺 is a 0-cost Ancient Curse with Exhaust and Ethereal. When exhausted, take 1 damage, draw 1 card, add 1 copy to the draw pile and 1 copy to the discard pile, then randomly gain 1 Intangible, 1 Undying, 2 Strength, 1 Dexterity, 5 Plated Armor, 1 Madness, or draw 2 cards. In single-player, each time you play 雋ｴ譁ｹ繧呈?縺 there is a 5% chance to trigger 驨o咇輠??vf墈荖鞷?悢篓.
- Killer's Film Reel: in combat, the first time each enemy is hit by your Attack, if that enemy does not intend to attack, Stun it and apply 3 Vulnerable.
- Prickett's Oath: every 2 non-boss combats, gain 1 three-card rare card reward.
- Red Queen's Soldier Piece: changes the first 5 map rows of the current act to 3 normal combats and 2 elite combats, preserving the original paths. After completing all trials, promotes into Red Queen.
- Red Queen: at the start of non-boss combat, reduce all enemies' current HP to 1.

## Mabel

Options:

- Option 1: choose 1 from 3 random favors. Candidate pool: Rapunzel's Favor / Little Mermaid's Favor / Frog Princess' Favor / Snow White's Favor / Cinderella's Favor.
- Option 2: Heinlyth Wine / Stage End / Silver Key.
- Option 3: Eternal Void / Mystery of the Night Sky / Gift of Chaos.

Relics:

- Rapunzel's Favor: draw 1 fewer card each turn. Every 3 player turns, gain 1 extra turn. The counter persists across combats.
- Little Mermaid's Favor: lose 30 max HP; max Energy +1.
- Frog Princess' Favor: whenever you apply a debuff to an enemy, apply the same debuff one extra time; then there is a 20% chance to apply 1 Weak, Vulnerable, or Frail to yourself.
- Snow White's Favor: lose 5 Dexterity at the start of combat; enemy attack damage taken is halved.
- Cinderella's Favor: lose 3 Strength at the start of each turn; at the end of each turn, remove all debuffs from yourself.
- Heinlyth Wine: gain 1 matching card. Heinlyth Wine is a 2/1-cost Ancient Power with Ethereal. Playing it grants an extra-turn effect.
- Stage End: gain 1 matching card. Stage End is a 0-cost Ancient Skill that refills your hand and Energy and grants Madness; after you play 8 non-Stage End cards, you are forcibly killed. Upgraded version removes Ethereal.
- Silver Key: on pickup, choose 1 card and enchant it with Unlock. Once per combat, an Unlock card can ignore play conditions and be played for free; the first trigger removes all card Afflictions.
- Eternal Void: at the start of combat, give Void to all cards in your deck; each turn, gain 1 random class card with Void.
- Mystery of the Night Sky: each turn, 50% chance for the first card you play to be played one additional time.
- Gift of Chaos: choose up to 3 non-X-cost Attack or Skill cards and fuse them into 1 Chaos Fusion. The fused card inherits material costs, keywords, and tags; its type is random, and it keeps at most 1 random enchantment. When played, it plays all material effects in random order against random targets. Fusion cards cannot be upgraded.

## Fairy Tale Relics

Fairy Tale relics use a separate relic pool and are not automatically treated as Ancient relics. When Fairy Tale Mode is enabled, you start with Unnamed Fairy Tale Book. It displays the number of remaining nodes; after every 7 non-Boss/non-Ancient nodes, it grants a random Fairy Tale. Duplicates are allowed.

- Unnamed Fairy Tale Book: obtained at the start of a run when Fairy Tale Mode is enabled. Shows how many nodes remain before the next random Fairy Tale. After every 7 non-Boss/non-Ancient nodes, gain a random Fairy Tale. Duplicates are allowed.
- Fairy Tale - Pinocchio: on pickup, choose 1 card with damage or block values and enchant it with Lie. After a Lie card is played, swap its damage and block values. Damage-only cards switch to gaining that much Block and dealing no damage; block-only cards switch to dealing that much damage and gaining no Block.
- Alice Through the Looking Glass: on pickup, choose 4 cards and enchant them with Ascension. The next act map becomes up to 8 straight routes. After 7 non-Ancient nodes, Ascension cards transform into a random card of the next higher rarity; rare cards transform into random Ancient cards, and Ancient cards cannot receive Ascension.
- Fairy Tale - The Three Little Pigs: the next 3 combats grant no rewards.
- Fairy Tale - The Emperor's New Clothes: during the second turn of each combat, you cannot gain Block.
- Fairy Tale - Alicuxel's Dog: at the start of combat, gain 3 Feel No Pain.
- Fairy Tale - The Singing Bone: at the start of combat, play Elegy.
- Fairy Tale - The Fox and the Sour Grapes: at the start of combat, all creatures gain 1 Envenom. Monsters also gain Envenom; when a monster attack deals HP damage to the player, the player gains Poison.
- Fairy Tale - The Pied Piper of Hamelin: at the start of combat, gain 3 Poison.
- Fairy Tale - Jack and the Beanstalk: after each node, lose 5 HP, but this cannot reduce you below 1 HP. When you reach the 6th node, gain Max HP equal to the total HP lost this way, then this relic becomes inactive.
- Fairy Tale - Aladdin and the Magic Lamp: at the start of turn, gain 3 Vigor.
- Fairy Tale - Beauty and the Beast: at the start of combat, if you can afford it, spend 20 Gold to gain 3 Strength. If you cannot afford it, lose 3 Strength.
- Fairy Tale - The Ugly Duckling: on pickup, add an upgraded Sovereign Blade and an upgraded Wrought in War to your deck.
- Fairy Tale - The High Jumpers: at the start of combat, gain 2 Flutter.
- Fairy Tale - The Wolf and X Little Goats: for the first three turns, your Block is not removed. At the start of your third turn, deal damage equal to your Block to all creatures.
- Fairy Tale - My Former Rascal: when you enter the next normal or elite combat, all enemies gain 5 Strength, then this relic becomes inactive.

## Random Events

- Wax Dolls: can appear in Acts 1-3. Choose Twin Wax Statue or Lonely Wax Statue. If your deck has no pair of same-name cards, the Twin option is disabled.
- Twin Wax Statue: choose a card that has a same-name card, then choose 2 same-name cards to enchant with Twin. When a Twin card is played, the other same-name Twin card is played.
- Lonely Wax Statue: choose 1 card and enchant it with Lonely. When a Lonely card is played, randomly exhaust a same-name card; if successful, that card gains Replay 1. It does not replay immediately on the first play.
- Bird Singer: can appear in the first half of Act 2. Randomly affects 7 later monster nodes on the map; entering an affected node halves enemies' current HP. Waiting loses 10 HP and marks affected nodes; running away does not mark them.
- Horrifying Glutton: can appear in Act 2. Choose to obtain Horrifying Glutton, or spend 100 Gold to gain a common or uncommon relic. Horrifying Glutton lets you choose 1 Attack card and enchant it with Feeding. Feeding damage starts at 150%, decreases by 15% after each combat, and increases by 50% for each successful kill at the end of combat. Minimum 50%, maximum 150%. If every player has no usable option, this event will not appear.
- Queen's Tart: can appear in Act 1. Eat the tart to gain 8 max HP, or keep it to obtain the Queen's Tart relic and unlock a special follow-up event.
- Queen's Tart relic: the next Act 2 event becomes Queen of Hearts.
- Queen of Hearts: special Act 2 event requiring Queen's Tart. Choose Red Queen's Guillotine or 200 Gold.
- Red Queen's Guillotine: on pickup, gain 1 matching card. Red Queen's Guillotine is a 0-cost Attack with Retain and Exhaust. It deals 10 damage, upgraded to 15, and has Execution.
- Execution: deals double damage to minions. If it kills an enemy, it immediately returns to your hand; otherwise, it is exhausted. Enemies killed by this card cannot revive. Revival effects are removed only after the kill is confirmed.
- Friendly Slime: can appear in Acts 1-2. Choose to nod, shake hands, or hug her, gaining a matching relic. The three relics share the same icon but have different effects.
- Friendly Slime relics: nodding chooses 1 Attack or Skill card and enchants it with Dissolve; shaking hands deals 7 damage and chooses 2 cards; hugging deals 15 damage and chooses 3 cards. If you do not have enough enchantable cards, or do not have enough HP to pay the damage, that option is disabled.
- Dissolve: adds Exhaust to this card. After it is played, this card's damage and block values are permanently reduced by 1. When all reducible values reach 0, remove it from the deck.
- Gentle Gift: can appear in Act 3, only if your deck has enchanted cards. Choose Mini Snowman or refuse the gift.
- Mini Snowman: on pickup, remove all card enchantments; at the start of combat, afflict all cards with Evil Qi.
- Refuse the Gift: 1 random card gains Evil Qi enchantment. If it already has an enchantment, Evil Qi replaces it.
- Evil Qi: when you play a card with Evil Qi, gain 1 Evil Qi. At end of turn, resolve once: lose HP equal to Evil Qi stacks, and drain that much Strength, Plated Armor, and HP from random enemies.
- Endless Tea Party: can appear in Acts 1-3. After answering the Hatter's riddle, drink suspicious tea, buy a suspicious hat, or rest to heal 15 HP.
- Mercury: obtained from the tea party. On pickup, gain 1 Mercury. Mercury is an Event Skill with Retain and Exhaust; it copies the cost and effect of the previous non-X-cost card, while keeping its own name and art. Unupgraded Mercury resets the copied effect at end of turn; upgraded Mercury does not.
- Suspicious Hat: bought at the tea party for 106 Gold; cannot be bought if there is no valid relic to copy. It always copies the rightmost vanilla non-Ancient relic, and its hover tip shows the current target. It does not copy modded relics, Ancient relics, or itself.
- The Last White Knight: can appear in Acts 1-2, only when your HP is below 30%. Accept the White Knight's protection, face the Red Knight alone, or ignore both knights and continue alone.
- White Knight's Protection: until the next rest site, gain 3 Strength and 3 Dexterity at the start of combat.
- Knight Chess Piece: obtained by facing the Red Knight in The Last White Knight. When choosing the next map node, you may move 1 column left or right any number of times.
- Alice's Handkerchief: obtained from The Last White Knight. In the first 5 non-Ancient nodes of the next act, if a node is a combat node, gain 3 Strength and 3 Dexterity at the start of combat. The relic becomes greyed out when its remaining counter reaches 0.
- Clown: can appear in Act 1, single-player only. Choose Rabbit Hand Mirror, Pumpkin Hand Mirror, or Jack Hand Mirror.
- Rabbit Hand Mirror: on pickup, gain Banai's Reflection. The relic counter shows current SAN. Banai's Reflection grants Block and shuffles a copy into the draw pile; copies cost 0 and their Block decreases each generation.
- Pumpkin Hand Mirror: on pickup, gain Orr's Reflection. The relic counter shows current SAN. Orr's Reflection heals HP and makes your Attack cards played next turn play one additional time.
- Jack Hand Mirror: on pickup, gain Holmes' Reflection and Jack the Ripper's Reflection, and modify the next act map so one route contains only normal combats and elites. Holmes' Reflection chooses 1 random Colorless card into your hand and increases SAN. Jack the Ripper's Reflection deals damage; each play permanently increases only this card's damage. All Jack cards share a combat counter; after 3 total plays, gain Virtue of Duality and, after combat, you must choose a deck card to transform into Jack the Ripper's Reflection.
- Edith's Ring (shared rare relic): at the start of combat, gain 1 Flutter, which is removed at the start of your second turn.
- Girl in the Maze: can appear in Act 3, single-player only, if you have one of Rabbit/Pumpkin/Jack Hand Mirror and SAN is below 0. Choose Girl's Hand Mirror, break the mirror, or gain 8 max HP.
- Girl's Hand Mirror: on pickup, gain Liddell's Reflection. The first time you take HP damage after combat starts, reflect equal damage to the attacker. Liddell's Reflection is an unplayable Curse; when exhausted or when it vanishes due to Ethereal, it copies your entire deck, gives the copies Ethereal, and adds them to the discard pile.
- Pervasive Malice (?): obtained from Girl in the Maze. Gain 1 Energy and draw 3 cards. Each use loses 15 SAN.
- SAN: the hand mirror series shares one SAN value, starting at 50 and saved across the run. Taking HP damage from attacks loses 5 SAN per hit; multi-hit attacks count each hit. Winning combat restores 20 SAN, up to 80. At 100 or more SAN, enter Rationality: apply 1 Vulnerable and 1 Weak to all enemies, and stop gaining Status cards during combat. When Rationality ends, lose 1 Energy and gain 1 Weak. At -100 or less SAN, enter low SAN: cards drawn this combat randomly transform into obtainable class cards, curses, or statuses; transformed cards are upgraded, and both damage you deal and damage you take are increased by 50%.
- Wriggling Shadow: gained the first time SAN reaches 0 or below, once per run. Wriggling Shadow, 辷卪, and Executioner evolve into the next card and remove themselves after enough kills; the card text shows remaining kills. Executioner Ketch removes all enemy Artifact and deals high damage.

## Other New Relics

- Dodo Run: when entering a non-boss combat, if your HP is below 20, skip that combat. Triggers only once.

## Multiplayer Restrictions

The following do not appear in multiplayer:

- Rapunzel's Favor
- Heinlyth Wine
- Prickett's Looking Glass
- Dodo Run
- White Queen's Soldier Piece / White Queen
- Red Queen's Soldier Piece / Red Queen
- Re-Thinking Poker
- Caterpillar's Smoke
- Duchess' Menu
- Rabbit Hand Mirror / Pumpkin Hand Mirror / Jack Hand Mirror / Girl's Hand Mirror

Some random effects that still appear in multiplayer need more testing, especially Mystery of the Night Sky, Gift of Chaos, Prickett's Dice, and Red Queen's Signed Album.
