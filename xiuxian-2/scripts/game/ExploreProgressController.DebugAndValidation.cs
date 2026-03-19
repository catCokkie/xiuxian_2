using Godot;
using System.Text;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private void EnsureDebugPanel()
        {
            _debugPanelLabel = new Label();
            _debugPanelLabel.Name = "DebugPanelLabel";
            _debugPanelLabel.Position = new Vector2(360.0f, 4.0f);
            _debugPanelLabel.Size = new Vector2(620.0f, 130.0f);
            _debugPanelLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _debugPanelLabel.Modulate = new Color(0.95f, 0.95f, 0.75f, 0.95f);
            _debugPanelLabel.Visible = false;
            _debugPanelLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _battleInfoLabel.GetParent().AddChild(_debugPanelLabel);
        }

        private void RefreshDebugPanel()
        {
            if (_debugPanelLabel == null || !_debugPanelVisible)
            {
                return;
            }

            var sb = new StringBuilder();
            string actionMode = IsDungeonMode() ? "dungeon" : "cultivation";
            sb.Append($"[F8] debug | zone={_currentZone}");
            sb.Append($" | mode={actionMode}");
            sb.Append($" | progress={_exploreProgress:0.0}%");
            sb.Append($" | monster={_battleMonsterName}({_battleMonsterId})");
            sb.Append($" | drop={_lastDropSummary}");
            sb.Append($"\nSimFilter level={(string.IsNullOrEmpty(_simulationLevelFilterId) ? "active" : _simulationLevelFilterId)}");
            sb.Append($" | monster={(string.IsNullOrEmpty(_simulationMonsterFilterId) ? "auto" : _simulationMonsterFilterId)}");

            if (_levelConfigLoader != null)
            {
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildDebugSummary());
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildValidationSummary(6));
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildLevelPreviewSummary(8));
            }

            sb.Append("\n[F4] toggle main action  [F5] switch unlocked level  [F6] sim-level  [F7] sim-monster  [F9] sim200  [F10] sim1000  [F11] scope  [F12] active-level");
            sb.Append($"\nSim: {_lastSimulationSummary}");

            _debugPanelLabel.Text = sb.ToString();
        }

        private bool IsDungeonMode()
        {
            return _actionState == null || _actionState.IsDungeonMode;
        }

        private void RefreshValidationPanel()
        {
            if (_validationPanel == null || _validationTitleLabel == null || _validationBodyLabel == null)
            {
                return;
            }

            _validationPanel.Visible = _validationPanelEnabled;
            if (!_validationPanelEnabled)
            {
                return;
            }

            if (_levelConfigLoader == null)
            {
                _validationPanel.SelfModulate = new Color(0.82f, 0.82f, 0.82f, 0.95f);
                _validationTitleLabel.Text = "配置校验：不可用";
                _validationBodyLabel.Text = "LevelConfigLoader 未加载。\n[color=#bfc7d4][F11][/color] scope  [color=#bfc7d4][F12][/color] 当前关卡";
                return;
            }

            var entries = _levelConfigLoader.GetValidationEntries();
            var filtered = FilterValidationEntries(entries);
            int issueCount = filtered.Count;
            int totalCount = entries.Count;
            if (issueCount <= 0)
            {
                _validationPanel.SelfModulate = new Color(0.70f, 0.90f, 0.74f, 0.95f);
                _validationTitleLabel.Text = $"配置校验：通过 ({BuildValidationFilterSummary()})";
                _validationBodyLabel.Text = "当前过滤条件下未发现配置错误。\n[color=#bfc7d4][F11][/color] scope  [color=#bfc7d4][F12][/color] 当前关卡";
                return;
            }

            _validationPanel.SelfModulate = new Color(0.98f, 0.72f, 0.72f, 0.96f);
            _validationTitleLabel.Text = $"配置校验：{issueCount}/{totalCount} 项 ({BuildValidationFilterSummary()})";

            int maxLines = 4;
            var sb = new StringBuilder();
            int shown = Mathf.Min(maxLines, issueCount);
            for (int i = 0; i < shown; i++)
            {
                var entry = filtered[i];
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string id = entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)";
                string field = entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)";
                string message = entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed";
                string severity = entry.ContainsKey("severity") ? entry["severity"].AsString() : "error";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";
                string monsterId = entry.ContainsKey("monster_id") ? entry["monster_id"].AsString() : "";
                string dropTableId = entry.ContainsKey("drop_table_id") ? entry["drop_table_id"].AsString() : "";

                if (i > 0)
                {
                    sb.Append('\n');
                }

                string severityColor = severity == "warning" ? "#f6ce72" : "#ff8b8b";
                sb.Append($"• [color={severityColor}]{EscapeBbCode(severity)}[/color] {EscapeBbCode(scope)}/{EscapeBbCode(id)} {EscapeBbCode(field)} {EscapeBbCode(message)}");

                if (!string.IsNullOrEmpty(levelId) || !string.IsNullOrEmpty(monsterId) || !string.IsNullOrEmpty(dropTableId))
                {
                    sb.Append(" [color=#cdd8ea](");
                    bool first = true;
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        sb.Append(BuildValidationKeyValueTag("level_id", levelId));
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(monsterId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(BuildValidationKeyValueTag("monster_id", monsterId));
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(dropTableId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(BuildValidationKeyValueTag("drop_table_id", dropTableId));
                    }
                    sb.Append(")[/color]");
                }
            }

            if (issueCount > shown)
            {
                sb.Append($"\n... 还有 {issueCount - shown} 项");
            }

            sb.Append("\n[color=#bfc7d4][F11][/color] scope  [color=#bfc7d4][F12][/color] 当前关卡");
            _validationBodyLabel.Text = sb.ToString();
        }

        public void SetValidationPanelEnabled(bool enabled)
        {
            _validationPanelEnabled = enabled;
            RefreshValidationPanel();
        }

        public void SetGlobalDebugOverlayEnabled(bool enabled)
        {
            _globalDebugOverlayEnabled = enabled;
            ApplyGlobalDebugOverlayVisibility();
            RefreshMoveDebugLabel();
        }

        private void CycleValidationScopeFilter()
        {
            _validationScopeFilterIndex = (_validationScopeFilterIndex + 1) % ValidationScopeFilters.Length;
        }

        private string BuildValidationFilterSummary()
        {
            string scope = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            string levelScope = _validationOnlyActiveLevel ? "active-level" : "all-levels";
            return $"{scope}, {levelScope}";
        }

        private Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> FilterValidationEntries(
            Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> entries)
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            string scopeFilter = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            string activeLevelId = _levelConfigLoader?.ActiveLevelId ?? "";

            foreach (var entry in entries)
            {
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";

                if (scopeFilter != "all" && scope != scopeFilter)
                {
                    continue;
                }

                if (_validationOnlyActiveLevel && !string.IsNullOrEmpty(activeLevelId))
                {
                    if (string.IsNullOrEmpty(levelId) || levelId != activeLevelId)
                    {
                        continue;
                    }
                }

                result.Add(entry);
            }

            return result;
        }

        private static string BuildValidationKeyValueTag(string key, string value)
        {
            return $"[color=#9ecbff]{EscapeBbCode(key)}[/color]=[color=#f8f8f2]{EscapeBbCode(value)}[/color]";
        }

        private static string EscapeBbCode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text
                .Replace("[", "[lb]")
                .Replace("]", "[rb]");
        }

    }
}
