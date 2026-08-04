# StarCountUnlocker

解锁《戴森球计划》新建游戏界面的**恒星数量滑块**，允许选择最多 **1024 颗恒星**（原版默认上限为 256）。

## 链接

- GitHub: https://github.com/ywscjlq/StarCountUnlocker
- Thunderstore: (待发布)

## 功能

- 扩展恒星数量滑块范围：**1 ~ 1024** 颗恒星（可配置，默认 1~1024）
- 自动修补内部数组大小（`GalaxyData.astrosData`、`SectorModel` 数组）以支持更大星系
- DLC 兼容 — 自动检测并修补 DLC 专属常量
- 配置文件生成于 `BepInEx/config/ywscjlq.star.count.unlocker.cfg`

## 安装

1. 为《戴森球计划》安装 [BepInEx](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/BepInEx/)
2. 将本 Mod 解压至 `BepInEx/plugins/`
3. 启动游戏，进入**新游戏**即可使用扩展后的恒星数量滑块！

## 配置

首次运行后，编辑：
```
BepInEx/config/ywscjlq.star.count.unlocker.cfg
```

| 设置项 | 默认值 | 说明 |
|---------|---------|------|
| `MaxStars` | 1024 | 滑块最大恒星数 |
| `MinStars` | 1 | 滑块最小恒星数 |

## 兼容性

- **DLC 兼容** ✓ — 自动检测 DLC 专属数组常量
- **星云/Multiplayer** — 理论上可用（BepInEx 插件，Harmony 补丁）

## 更新日志

### 1.0.0
- 初始发布
- 滑块范围扩展（1~1024 恒星）
- 通过 Harmony transpiler 修补 ASTRO_COUNT 常量
- DLC 常量自动检测
