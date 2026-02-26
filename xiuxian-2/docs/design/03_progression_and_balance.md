# 03 进度与数值

## 探索进度公式（按输入事件）
- `explore_progress_gain = input_events * 0.02% * zone_speed_factor * battle_pause_factor`
- 基础区口径：`每 100 次键鼠输入 -> 探索进度约 2.0%`
- 说明：探索/战斗进度只使用输入事件计数，不使用 `AP_final`。

## 活动点 AP 公式（每秒）
- `AP_sec = key_down * 1.0 + mouse_click * 1.2 + scroll_step * 0.4 + move_px / 600`

## 高频衰减
- 设 `R = AP_sec / AP_baseline`，建议 `AP_baseline = 6`。
- 衰减系数：`decay = clamp(1.0 - max(0, R - 1) * 0.25, 0.45, 1.0)`
- 实际活动点：`AP_final = AP_sec * decay`

## 资源转换（每 10 秒结算）
- 说明：AP 仅用于资源结算，不直接推进探索进度。
- `lingqi += AP_final_10s * 0.9 * mood_mul * realm_mul`
- `insight += AP_final_10s * 0.08`
- `pet_affinity += AP_final_10s * 0.03 * interact_mul`

## 倍率
- `mood_mul`：
- `pet_mood >= 80` -> 1.10
- `31-79` -> 1.00
- `<= 30` -> 0.85
- `realm_mul = 1 + (realm_level - 1) * 0.06`

## 境界经验需求
- `exp_required(r) = 120 * r^1.32 + 180`（r 从 1 开始）

## 日常节奏目标（V1）
- 轻度办公用户（日均约 2000-3500 次键鼠事件）：可稳定推进 1-2 个小阶段。
- 重度办公用户（日均约 5000+ 次）：有收益提升但受衰减与软上限控制。
- 单日收益上限建议：基础收益的 2.2 倍。

## 调参与监控点
- 每小时 AP 产出分布（P50/P90）。
- 境界突破耗时分布。
- 衰减触发频次（用于识别是否过严）。
- 低活跃用户 3 日留存与进度停滞率。
