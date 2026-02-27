using Godot;

public static class UiText
{
    public const string DragHandle = "↕";
    public const string ResizeHandle = "↔";
    public const string BookButton = "书";
    public const string SpiritStoneLabelPrefix = "灵石";
    public const string DefaultZoneName = "幽泉洞窟";
    public const string DefaultMonsterName = "妖物";
    public const string SettingsTitle = "设置";
    public const string RealmFallback = "炼气一层";

    public const string LeftTabCultivation = "修炼概况";
    public const string LeftTabEquipment = "装备情况";
    public const string LeftTabStats = "统计概览";
    public const string RightTabOnline = "联机";
    public const string RightTabBug = "Bug反馈";
    public const string RightTabSettings = "设置";

    public const string SystemSection = "系统";
    public const string DisplaySection = "画面";
    public const string ProgressSection = "进度";
    public const string ResetAndApply = "重置并应用";
    public const string Quit = "退出";
    public const string Open = "打开";
    public const string DevHintCloudSync = "说明：云端同步功能仍在开发中，当前仅保存开关状态。";
    public const string Language = "语言";
    public const string KeepOnTop = "保持窗口置顶";
    public const string TaskbarIcon = "任务栏图标";
    public const string StartupAnimation = "开机启动动画";
    public const string AdminMode = "管理员模式";
    public const string HandwritingSupport = "手写支持";
    public const string Vsync = "垂直同步";
    public const string MaxFps = "帧率";
    public const string Resolution = "主界面分辨率";
    public const string ShowControlMarkers = "显示界面控制标记";
    public const string LogFolder = "显示日志文件夹";
    public const string GameScale = "游戏缩放比例";
    public const string UiScale = "界面缩放比例";
    public const string AutoSaveInterval = "自动存档频率";
    public const string CloudSync = "云端同步";
    public const string MilestoneTips = "里程碑提示";

    public static string SpiritStone(int amount) => $"{SpiritStoneLabelPrefix} {amount}";
    public static string RealmStage(int realmLevel, double percent) => $"炼气{realmLevel}层 {percent:0}%";
    public static string BatchInputAndAp(int inputEvents, double apFinal) => $"本批输入 {inputEvents} | 本批AP(资源) {apFinal:0.0}";
    public static string ExploreFrame(int frame) => $"探索中 | 帧 {frame}";
    public static string ExploreProgress(float progress) => $"进度 {progress:0.0}%";
    public static string Encounter(string monsterName) => $"遭遇{monsterName}，输入推进战斗回合";
    public static string BattleRound(int round, string monsterName, int hp) => $"Round {round} | {monsterName} HP {hp}";
    public static string BattleInProgress(string monsterName) => $"战斗中.. {monsterName}";
    public static string BattleVictory(string monsterName) => $"战斗胜利，结算{monsterName}战利品";

    public const string ExploreIdle = "探索待命";
    public const string WaitingInput = "等待输入...";
    public const string ZoneComplete = "区域探索完成，切换下一区域";

    public static string CultivationOverview(
        int realmLevel,
        double realmExp,
        double realmExpRequired,
        double expPercent,
        double lingqi,
        double insight,
        double petAffinity,
        double moodMultiplier)
    {
        return
            $"{LeftTabCultivation}\n" +
            $"- 当前境界: 炼气{realmLevel}层\n" +
            $"- 境界经验: {realmExp:0.0}/{realmExpRequired:0.0} ({expPercent:0}%)\n" +
            $"- 灵气: {lingqi:0.0}\n" +
            $"- 悟性: {insight:0.0}\n" +
            $"- 灵宠亲和: {petAffinity:0.0}\n" +
            $"- 心情倍率: x{moodMultiplier:0.00}";
    }

    public static string StatsOverview(
        long keyCount,
        long clickCount,
        long scrollSteps,
        double moveDistance,
        double apAccumulator,
        int herbCount,
        int shardCount)
    {
        return
            $"{LeftTabStats}\n" +
            $"- 总键盘按下: {keyCount:N0}\n" +
            $"- 总鼠标点击: {clickCount:N0}\n" +
            $"- 总滚轮步数: {scrollSteps:N0}\n" +
            $"- 总移动距离: {moveDistance:N0}px\n" +
            $"- AP缓存: {apAccumulator:0.0}\n" +
            $"- 灵草库存: {herbCount}\n" +
            $"- 灵气碎片库存: {shardCount}";
    }

    public static string CultivationTemplate =>
        $"{LeftTabCultivation}\n- 当前境界\n- 突破条件\n- 心法加成";

    public static string EquipmentTemplate =>
        $"{LeftTabEquipment}\n- 武器/护具/饰品\n- 词条预览\n- 套装效果";

    public static string StatsTemplate =>
        $"{LeftTabStats}\n- 总输入次数\n- 累计探索时长\n- 战斗胜率";

    public static string OnlineTemplate =>
        $"{RightTabOnline}\n该功能开发中。";

    public static string BugTemplate =>
        $"{RightTabBug}\n- 描述问题\n- 复制日志路径\n- 导出反馈包";
}
