# BS Ancient 维护风险登记

本文记录无法仅靠编译和单元测试完全排除的运行期风险，供升级游戏版本、
RitsuLib 或重构大型功能时参考。当前兼容基线为正式版 `0.107.1`、Beta
`0.111.0`（`41cef1ea`）和 RitsuLib `0.6.9`。

## 风险等级

- **高**：目标 API 或异步时序变化后，可能黑屏、卡回合、损坏事件流程或处理错误存档。
- **中**：通常可以安全降级，但可能导致某项遗物、UI 或提示失效。
- **低**：主要影响维护成本、翻译显示或包体，不太可能直接破坏一局游戏。

## RitsuLib 0.6.3 后续审查

本次修正了以下已确认的实现问题，没有增加全局补丁，并移除了四个旧补丁：

- 兵装的每回合自动打出标记改为在 `BeforeHandDraw` 重置，避免新回合抽牌仍使用旧标记；复制兵装牌时清除正在自动打出的临时保护标记。
- 沉睡的 `OnEnchant` 不再覆盖已保存的原始层数与唤醒状态，保留第 X+1 次抽到才能打出的规则。
- 处刑、残虐、进食、水银、人鱼之泪，以及独角兽皇家纹章、大鹿女神、狐狸与酸葡萄、暴魔的胶卷和屠夫数学课的可变集合，在克隆模型时分别分配，避免实例共享运行期记录。
- 人鱼之泪的待删牌记录明确存放在对应的永久牌组版本，战斗副本结束时也通过该版本执行删除，不再依靠共享集合。
- 判决令的即时打出保护支持同牌嵌套作用域，重复释放不会破坏后续保护。
- 槌球杆在结算缓存攻击牌前重新检查战斗、所有者、牌堆和额外打出条件；按卡牌类型缓存条件反射结果。
- 独角兽反击保留 `ValueProp.Unpowered` 和实例级重入保护，移除受伤前后钩子、荆棘与蜂巢的四个全局跳过补丁，使正常受伤监听可以继续执行。原版荆棘与蜂巢只响应攻击伤害，不会因为这种反击而递归。

`CombatGuardRegressionTests` 与 `ModelStateIsolationTests` 覆盖这些边界。
卡牌与附魔克隆采用执行测试；遗物基类需要 Godot 原生初始化，因此普通
`dotnet test` 只检查其克隆方法是否分配并写入独立集合，不能替代多人实测。
发布前重点复测兵装开局抽牌和重抽、沉睡升级与存档刷新、多人持有相同遗物、
多张人鱼之泪战后删牌，以及槌球杆和判决令的组合。

## 运行期兼容风险

| 等级 | 区域 | 当前脆弱点 | 可能症状 | 已有保护与升级检查 |
| --- | --- | --- | --- | --- |
| 高 | 舵手的书页 | `HelmsmansPageRelicPatch` 读取 `MoveState._onPerform`，并检查异步状态机 IL | 敌人行动、对话或回合结算卡住 | 字段或 IL 不匹配时禁用功能并写警告。升级后必须实测会说话和不会说话的敌人、召唤物及多人战斗。 |
| 高 | 隐藏事件选项 | `HiddenEventOptionSupport` 反射调用 `EventModel.SetEventFinished` / `SetEventState` | 选择彩蛋选项后没有离开按钮，事件软锁 | 两种结束接口互为回退，失败时写日志。升级后逐个测试银行家、律师、舵手、贝尔曼、面包师和生物课选项。 |
| 高 | 博基姆记忆 | `BoojumHistoryMemory` 读取 `SaveManager._saveStore` 和历史对局元数据 | 读取错误 profile、无法备份或错误标记历史记录 | 操作绑定 profile 与开局时间，写入前保留备份。升级后用非主存档槽测试胜利、失败、退出重进及 Steam 云同步。 |
| 高 | 异步战斗结算 | 邪气、兵装、处刑、槌球杆、判决令等会自动打牌或插入行动 | 状态图标不消失、无法结束回合、重复自动打出 | 自动打出均应经过战斗状态和重入检查。升级后测试空目标、敌人死亡、华丽收场类额外条件牌、抽取大量牌和提前结束战斗。 |
| 中 | 仅使用 Mod 先古 | `OnlyUseModAncientsPatch` 读取 `ActModel._rooms` 与 `_sharedAncientSubset` | 先古房间不替换、候选池重复或楼层错误 | 私有字段不存在时退回本层候选，不阻止正常开局。升级后检查单人、多人和每个楼层的先古生成。 |
| 中 | 槌球杆 | `CroquetMalletCombatService` 反射调用非公开 `get_IsPlayable` | 自动打出的邻侧攻击牌忽略或错误应用额外条件 | 找不到接口时拒绝自动打出。升级后测试普通攻击、X 费牌、无目标和有额外打出条件的攻击牌。 |
| 中 | 屠夫的数学课 | `ButchersMathClassDirectValuePatch` 动态枚举命令方法并修改即时数值 | 预览与实际数值不一致，或新命令没有参与计算 | Harmony 目标解析测试覆盖当前版本。升级后检查伤害、格挡、治疗、抽牌、能量、金币、生成/弃置/消耗数量。 |
| 中 | UI 注入 | 童话模式开关、扑克按钮、卡牌描边和弹窗依赖场景节点生命周期 | `Node not found`、空引用、按钮重叠或进入菜单黑屏 | 注入逻辑应使用节点就绪信号和空值检查，不依赖固定延迟。升级后检查主菜单、设置页、角色选择和战斗 UI。 |
| 中 | 多人同步 | 自动打牌、事件选牌和部分遗物依赖原版 `PlayerChoiceContext` 与行动队列 | 双方行动栈不同、只在主机生效、结束回合卡住 | 不绕过原版行动队列；Beta 测试只能验证方法存在。发布前至少进行一次双人房间、事件和战斗结算测试。 |
| 中 | 双版本加载器 | `0.111.0` 及以上目前统一选择现有 Beta 实现 | 未来 Beta API 再次变化时加载失败 | 正式版和 Beta DLL 已隔离，但不是永久前向兼容。新 Beta 发布后先添加引用包和独立实现，再调整版本选择边界。 |
| 低 | 动态本地化 | 动态描述依赖 `{Amount}` 等变量和中英文键完整对应 | 格式化异常、悬浮提示显示变量名或空文本 | `LocalizationIntegrityTests` 检查键和 JSON；新动态变量仍需在游戏内查看卡牌、状态和遗物悬浮提示。 |
| 低 | PCK 导出 | Godot 的 `all_resources` 会收集项目根目录中的资源 | 把测试、上传区或归档素材打进 PCK，导致体积和启动时间增加 | `export_presets.cfg` 已排除 `archive/compat/docs/tests/upload`。修改目录结构后检查导出日志和 PCK 体积。 |

## 高耦合文件

以下文件职责较多。它们目前可以工作，不应仅为缩短行数进行大规模重写：

- `Scripts/Relics/RoyalChipRelic.cs`：地图标注、赌注、条件跟踪、奖励和存档状态。
- `Scripts/Enchantments/PlayingCardSuitEnchantments.cs`：花色次数、牌型触发、免费触发和悬浮信息。
- `Scripts/Relics/SuspiciousHatRelic.cs`：历史统计、动态悬浮提示和卡牌概率。
- `Scripts/Cards/MirrorReflectionCards.cs` 与 `Scripts/Modifiers/MirrorSanModifier.cs`：镜像牌和跨战斗状态。
- `Scripts/Services/BalatroTrainingCombatService.cs`：临时牌组、UI、抽弃牌和计分流程。
- `Scripts/Patches/ButchersMathClassDirectValuePatch.cs`：多种命令的即时数值拦截。
- `Scripts/Patches/HelmsmansPageRelicPatch.cs`：私有字段、IL 和异步敌人行动。

如需拆分，优先提取纯计算或纯状态序列化部分，并先补回归测试；不要同时改变
Harmony 目标、异步时序和数据结构。

## 当前刻意禁用的内容

- **骑士的盟约**已实现，右键触发逻辑保留用于控制台测试。
- 它不在罗丽娜的 `AllPossibleOptions` 或任何选项池中。
- `KnightCovenantRelic.IsAllowed()` 固定返回 `false`，因此不会在正常游戏中出现。
- 正式实装时应同时检查选项池、`IsAllowed()`、README 和中英文遗物文本。

## 每次升级后的验证顺序

1. 更新对应游戏引用程序集和 RitsuLib 包，不要让正式版项目直接引用 Beta DLL。
2. 执行正式版测试：

   ```powershell
   dotnet test tests/bs_ancient.Tests/bs_ancient.Tests.csproj --no-restore
   ```

3. 执行 Beta API 目标测试：

   ```powershell
   dotnet test tests/bs_ancient.Beta.Tests/bs_ancient.Beta.Tests.csproj --no-restore
   ```

4. 构建并导出：

   ```powershell
   dotnet build bs_ancient.csproj --no-restore -t:ExportPck
   ```

5. 检查本地 Mod 与 `upload/content` 中 Loader、两个实现 DLL 和 PCK 的哈希是否一致。
6. 确认 `upload/content` 没有 `bs_ancient_config.cfg`，避免覆盖玩家设置。
7. 在正式版和 Beta 各启动一次游戏，依次测试隐藏事件、自动打牌、保存继续和多人结束回合。

## 自动测试的边界

现有测试可以确认 JSON、本地化键、配置迁移、扑克规则、皇家筹码规则、加载器选择
和 Harmony 目标是否存在。它们不能证明 Godot 节点已经就绪，也不能完全模拟原版行动
队列、网络同步、Steam 云存档或玩家在事件/战斗中途退出。因此，涉及上述高风险区域
的修改必须保留一次真实游戏回归测试。
