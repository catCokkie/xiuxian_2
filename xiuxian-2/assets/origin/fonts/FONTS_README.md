---
AIGC:
    ContentProducer: Minimax Agent AI
    ContentPropagator: Minimax Agent AI
    Label: AIGC
    ProduceID: "00000000000000000000000000000000"
    PropagateID: "00000000000000000000000000000000"
    ReservedCode1: 3045022100a6166e19e85a645413b888916a4324dae1ae59313041b1cc0885dd489d7dd161022039c9643b5e5cb25d1d0961d117659fc03d2e1196df8cada785a9f0ddf90eff6e
    ReservedCode2: 30450221009a3a9ab78266360a87424a52e7f8bd91f6a8240ae9d0ed9c30a73ae8ee7c6775022015c9e5d358e225e0b72fee31b5132025d3f48231c3088aea74c597b8cff6caec
---

# 字体资源说明

> 由于网络限制，字体文件无法在此环境直接下载。请手动获取以下推荐字体。

## 推荐字体（均免费可商用）

### 🅰 标题字体 —— 方舟像素字体（Ark Pixel Font）

- **许可证**：SIL Open Font License 1.1（可商用）
- **支持**：简体中文 / 繁体中文 / 日文 / 韩文 全覆盖
- **下载**：https://github.com/TakWolf/ark-pixel-font/releases
- **推荐版本**：`ark-pixel-font-12px-monospaced-zh_cn-otf.zip`
- **用途**：境界名、标题文案（修仙风像素字体）
- **存放路径**：`fonts/ark-pixel-12px.otf`

### 🅱 正文字体 —— 霞鹜文楷轻便版（LXGW WenKai Lite）

- **许可证**：SIL Open Font License 1.1（可商用）
- **支持**：简体中文 + 拉丁字符
- **下载**：https://github.com/lxgw/LxgwWenKai/releases
- **推荐版本**：`LXGWWenKaiLite-Regular.ttf`
- **用途**：数值说明、系统文案（清晰易读）
- **存放路径**：`fonts/lxgw_wenkai_regular.ttf`

### 🅲 备选像素字体 —— 凤凰点阵体（Vonwaon Bitmap）

- **许可证**：SIL Open Font License 1.1（可商用）
- **支持**：简体中文 GBK 全字符集
- **下载**：https://github.com/googlefonts/VonwaonBitmap
- **用途**：数字/状态值显示（像素点阵风格）
- **存放路径**：`fonts/vonwaon-bitmap-16px.ttf`

## Godot 集成方式

```gdscript
# 在 project.godot 中注册字体资源
# 或在场景 .tscn 中：
# [sub_resource type="DynamicFont" id="1"]
# font_data = ExtResource("ark-pixel.otf")
# size = 12
```

## 注意事项

- `.tscn/.tres` 文件必须保存为 **UTF-8 (no BOM)**
- 字体放入 `xiuxian-2/assets/fonts/` 目录
- 运行时文案统一从 `scripts/ui/UiText.cs` 输出，字体在场景中引用
