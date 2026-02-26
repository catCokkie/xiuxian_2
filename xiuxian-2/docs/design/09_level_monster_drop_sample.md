# 09 关卡/怪物/掉落示例（炼气期）

## 1) 关卡示例

### Level: `lv_qi_001`
- `level_id`: `lv_qi_001`
- `level_name`: `幽泉洞窟外层`
- `realm_recommend`: `炼气1-3层`
- `zone_speed_factor`: `1.00`
- `danger_level`: `1`
- `theme_tags`: `洞窟, 潮湿, 阴寒`

#### 探索参数
- `progress_per_100_inputs`: `2.0%`
- `encounter_check_interval_progress`: `20%`
- `base_encounter_rate`: `18%`
- `battle_pause_factor`: `0.0`（战斗中探索暂停）

#### 怪物池
- `monster_slime_moss`，`weight=60`，`min_count=1`，`max_count=2`
- `monster_bat_shadow`，`weight=30`，`min_count=1`，`max_count=2`
- `monster_spider_cave`，`weight=10`，`min_count=1`，`max_count=1`

#### 通关奖励
- `first_clear_reward`: `lingqi +120`, `insight +20`, `item: novice_breakthrough_pill x1`
- `repeat_clear_reward`: `lingqi +40~80`, `insight +6~12`
- `exp_bonus_factor`: `1.00`

#### 存档字段
- `zone_id=lv_qi_001`
- `explore_progress=0~100`
- `battle_state=exploring/in_battle/result`
- `last_clear_unix=<timestamp>`

---

## 2) 怪物示例

### Monster: `monster_slime_moss`
- `monster_id`: `monster_slime_moss`
- `monster_name`: `苔皮黏妖`
- `rarity`: `normal`
- `realm_recommend`: `炼气1层`
- `tags`: `妖, 软体`

#### 战斗参数（输入驱动）
- `hp`: `22`
- `attack`: `4`
- `defense`: `1`
- `inputs_per_round`: `18`
- `speed_factor`: `1.00`

#### 行为机制
- `skill_list`: `黏附(10%减速1回合)`
- `phase_rules`: `无`
- `anti_afk_rule`: `连续3回合低输入则玩家伤害-10%`

#### 掉落配置
- `drop_table_id`: `drop_qi_outer_normal`
- `drop_roll_count`: `1`
- `guaranteed_drop`: `lingqi_shard x1`

#### 结算收益
- `lingqi_reward_min/max`: `18 / 30`
- `insight_reward_min/max`: `2 / 4`

### Monster: `monster_bat_shadow`
- `monster_id`: `monster_bat_shadow`
- `monster_name`: `影翼蝠`
- `rarity`: `normal`
- `realm_recommend`: `炼气2层`
- `tags`: `妖, 飞行`

#### 战斗参数（输入驱动）
- `hp`: `26`
- `attack`: `5`
- `defense`: `2`
- `inputs_per_round`: `17`
- `speed_factor`: `1.05`

#### 行为机制
- `skill_list`: `掠影(先手概率+15%)`
- `phase_rules`: `血量低于30%时攻速+10%`
- `anti_afk_rule`: `无`

#### 掉落配置
- `drop_table_id`: `drop_qi_outer_normal`
- `drop_roll_count`: `1`
- `guaranteed_drop`: `无`

#### 结算收益
- `lingqi_reward_min/max`: `22 / 36`
- `insight_reward_min/max`: `3 / 5`

### Monster: `monster_spider_cave`
- `monster_id`: `monster_spider_cave`
- `monster_name`: `洞窟蛛母`
- `rarity`: `elite`
- `realm_recommend`: `炼气3层`
- `tags`: `妖, 精英`

#### 战斗参数（输入驱动）
- `hp`: `42`
- `attack`: `8`
- `defense`: `4`
- `inputs_per_round`: `20`
- `speed_factor`: `0.95`

#### 行为机制
- `skill_list`: `缠丝(玩家下回合伤害-20%)`
- `phase_rules`: `50%血以下，每2回合召唤小蛛`
- `anti_afk_rule`: `低输入时触发缠丝概率+20%`

#### 掉落配置
- `drop_table_id`: `drop_qi_outer_elite`
- `drop_roll_count`: `2`
- `guaranteed_drop`: `spirit_herb x1`

#### 结算收益
- `lingqi_reward_min/max`: `45 / 70`
- `insight_reward_min/max`: `6 / 10`

---

## 3) 掉落表示例

### DropTable: `drop_qi_outer_normal`
- `drop_table_id`: `drop_qi_outer_normal`
- `bind_level_id`: `lv_qi_001`
- `bind_monster_id`: `monster_slime_moss, monster_bat_shadow`

#### 掉落条目
- `lingqi_shard`，`weight=55`，`min_qty=1`，`max_qty=3`
- `spirit_herb`，`weight=30`，`min_qty=1`，`max_qty=1`
- `broken_talisman`，`weight=15`，`min_qty=1`，`max_qty=2`

#### 保底与衰减
- `pity_counter_key`: `pity_qi_outer_normal`
- `pity_threshold`: `8`
- `pity_item_id`: `spirit_herb x1`
- `repeat_decay_factor`: `0.95`

#### 经济约束
- `daily_cap`: `120 rolls`
- `hourly_soft_cap`: `20 rolls`
- `anti_abuse_flag`: `enabled`

### DropTable: `drop_qi_outer_elite`
- `drop_table_id`: `drop_qi_outer_elite`
- `bind_level_id`: `lv_qi_001`
- `bind_monster_id`: `monster_spider_cave`

#### 掉落条目
- `novice_breakthrough_pill`，`weight=25`，`min_qty=1`，`max_qty=1`
- `spirit_herb`，`weight=45`，`min_qty=1`，`max_qty=2`
- `lingqi_core_fragment`，`weight=30`，`min_qty=1`，`max_qty=2`

#### 保底与衰减
- `pity_counter_key`: `pity_qi_outer_elite`
- `pity_threshold`: `5`
- `pity_item_id`: `novice_breakthrough_pill x1`
- `repeat_decay_factor`: `0.98`

#### 经济约束
- `daily_cap`: `40 rolls`
- `hourly_soft_cap`: `8 rolls`
- `anti_abuse_flag`: `enabled`
