---
AIGC:
    ContentProducer: Minimax Agent AI
    ContentPropagator: Minimax Agent AI
    Label: AIGC
    ProduceID: "00000000000000000000000000000000"
    PropagateID: "00000000000000000000000000000000"
    ReservedCode1: 3045022100d548d6294c97bca2d038ee345ecf9bbeaefc74d8d31a804a961e2bef29dc2dd102207005aa7d0f3743cf362b9361a3f2b0be3feff4722190873d2f7f442a128072e8
    ReservedCode2: 304502201a05a9b5d4e29afd8ba35e95e0189c962364f94bf8c7a30c74822545c8ab8700022100f20232a391d4deb914bdd711367aabbeea102028bfb4c5f6a9b7ef30d730e2dc
---

# xiuxian_2 素材库 / Asset Library

为 [catCokkie/xiuxian_2](https://github.com/catCokkie/xiuxian_2) 生成的游戏素材。
项目：桌面底部驻留修仙桌宠（Godot 4 + C#），以键鼠操作驱动修炼进度。

生成日期：2026-03-02
生成工具：AI 图像生成（自创） + 开源字体（需手动获取）

---

## 📁 目录结构

```
output/
├── ui/                    # UI 框架素材（10项）
├── spirit_pet/            # 灵宠精灵图（5项）
├── scene_bg/              # 场景/背景素材（3项）
├── monsters/              # 怪物精灵图（5项）
├── items/                 # 物品图标（3项）
├── effects/               # 特效动画（3项）
└── fonts/                 # 字体（说明文档，需手动下载）
```

---

## 🖼 UI 框架素材（`ui/`）

| 文件名 | 说明 | 对应设计位置 |
|--------|------|------------|
| `main_bar_bg.png` | 主横向底栏背景，羊皮纸+木质棕色调 | 横向主窗口底部贴边 |
| `book_window_bg.png` | 图书/卷轴子菜单窗口背景 | 子菜单窗口 |
| `tab_buttons.png` | 页签组件（激活/未激活各2态） | 子菜单顶部页签组 |
| `status_bar_bg.png` | 底部状态条背景 | 子菜单底部灵石栏 |
| `explore_progress_bar.png` | 探索进度条（空/进行中/满各态） | 主窗口右下 |
| `icon_drag_handle.png` | 拖动按钮A（位移柄） | 主窗口左上 |
| `icon_resize_handle.png` | 拖动按钮B（缩放柄） | 主窗口右上 |
| `icon_book_button.png` | 图书按钮（开/关两态） | 主窗口左下 |
| `icon_close_button.png` | 关闭按钮X（普通/悬停两态） | 子菜单窗口左上 |
| `icon_lingshi.png` | 灵石货币图标 | 底部状态条右侧 |

---

## 🐾 灵宠素材（`spirit_pet/`）

| 文件名 | 帧数 | 说明 |
|--------|------|------|
| `pet_idle_sheet.png` | 6帧横向Spritesheet | 呼吸待机动画 |
| `pet_blink_sheet.png` | 4帧横向Spritesheet | 眨眼动画 |
| `pet_interact_sheet.png` | 6帧横向Spritesheet | 互动/被逗动画 |
| `pet_mood_low.png` | 单帧 | 心情低（≤30）状态 |
| `pet_mood_high.png` | 单帧 | 心情高（≥80）状态 |

> 每帧尺寸：64×64 px，像素风像素小狐/小兔灵宠

---

## 🌄 场景/背景素材（`scene_bg/`）

| 文件名 | 说明 |
|--------|------|
| `zone_001_cave_bg.png` | 幽泉洞窟外层主背景（横向滚动，960×128px）|
| `zone_001_cave_midground.png` | 洞窟中景层（钟乳石/发光蘑菇，带透明通道）|
| `player_cultivator.png` | 玩家修仙者精灵图（Spritesheet：待机/走路/攻击各帧）|

---

## 👾 怪物素材（`monsters/`）

| 文件名 | 对应配置 | 说明 |
|--------|---------|------|
| `mob_001_cave_insect_idle.png` | mob_001 洞窟青螟 | 4帧待机动画 Spritesheet，32×32px |
| `mob_001_hit_sheet.png` | mob_001 洞窟青螟 | 3帧受击动画（闪白→后仰）|
| `mob_001_death_sheet.png` | mob_001 洞窟青螟 | 4帧死亡淡出动画（0.28s）|
| `mob_002_cave_bat.png` | mob_002（待命名）| 洞窟蝙蝠/幽灵，4帧待机 |
| `drop_marker.png` | DropMarker节点 | 掉落提示标记，4帧出现动画 |

---

## 🌿 物品图标（`items/`）

| 文件名 | 对应配置 | 稀有度 | 说明 |
|--------|---------|--------|------|
| `icon_spirit_herb.png` | drop_spirit_herb 灵草 | 普通 | 突破材料，32×32px |
| `icon_lingqi_shard.png` | drop_lingqi_shard 灵气碎片 | 普通 | 100%掉落，32×32px |
| `icon_breakthrough_pill.png` | breakthrough_pill 突破丹 | 稀有 | 突破消耗品，32×32px |

---

## ✨ 特效素材（`effects/`）

| 文件名 | 帧数 | 触发条件 | 说明 |
|--------|------|---------|------|
| `breakthrough_effect.png` | 4帧 | 境界突破时 | 金色爆发光效，96×96px/帧 |
| `epiphany_popup.png` | 3帧 | 顿悟小事件 | 非打扰式提示气泡，64×32px |
| `zone_complete_effect.png` | 4帧 | 区域探索100%完成 | 金色庆祝粒子爆发，96×96px/帧 |

---

## 🔤 字体（`fonts/`）

字体因网络限制需手动下载，详见 `fonts/FONTS_README.md`。

推荐：
- **标题**：[方舟像素字体 Ark Pixel Font](https://github.com/TakWolf/ark-pixel-font) (SIL OFL 1.1)
- **正文**：[霞鹜文楷 LXGW WenKai](https://github.com/lxgw/LxgwWenKai) (SIL OFL 1.1)

---

## 📋 素材接入建议

### Godot 目录建议
```
xiuxian-2/assets/
├── ui/              ← 本目录 ui/ 内容
├── characters/      ← spirit_pet/ + scene_bg/player_cultivator.png
├── scenes/          ← scene_bg/zone_*/
├── monsters/        ← monsters/
├── items/           ← items/
├── effects/         ← effects/
└── fonts/           ← 手动下载后放入
```

### 精灵图用法（Godot C#）
```csharp
// 加载Spritesheet示例
var sprite = new AnimatedSprite2D();
var frames = new SpriteFrames();
frames.AddAnimation("idle");
// 按帧数切割：pet_idle_sheet.png → 6帧，每帧64px宽
sprite.SpriteFrames = frames;
```

---

## 📜 版权说明

- 所有图像素材均为 AI 生成（原创），无第三方版权限制
- 字体需遵守各自的 SIL OFL 1.1 许可证（可免费商用，需保留版权声明）
- 建议在游戏内 `设置` > `关于` 页面注明字体来源
