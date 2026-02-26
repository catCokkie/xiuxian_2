using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Event-driven input aggregator with anti-abuse guards:
    /// 1) rolling-window frequency decay
    /// 2) per-minute soft cap
    /// </summary>
    public partial class InputActivityState : Node
    {
        [Signal]
        public delegate void ActivityTickEventHandler(double apThisBatch, double apFinal);

        [Signal]
        public delegate void InputBatchTickEventHandler(int inputEventsThisBatch, double apFinal);

        // Latest batch counters (updated every frame when pending input exists)
        public int KeyDownCount { get; private set; }
        public int MouseClickCount { get; private set; }
        public int MouseScrollSteps { get; private set; }
        public double MouseMoveDistancePx { get; private set; }

        // Lifetime counters
        public long TotalKeyDownCount { get; private set; }
        public long TotalMouseClickCount { get; private set; }
        public long TotalMouseScrollSteps { get; private set; }
        public double TotalMouseMoveDistancePx { get; private set; }

        // AP results
        public double ApThisSecond { get; private set; }
        public double ApFinal { get; private set; }
        public double ApAccumulator { get; private set; }
        public int InputEventsThisSecond { get; private set; }

        [Export] public double ApBaseline { get; set; } = 6.0;
        [Export] public double KeyDownWeight { get; set; } = 1.0;
        [Export] public double MouseClickWeight { get; set; } = 1.2;
        [Export] public double ScrollStepWeight { get; set; } = 0.4;
        [Export] public double MovePxDivider { get; set; } = 600.0;

        [Export] public double DecayThreshold { get; set; } = 1.0;
        [Export] public double DecayRate { get; set; } = 0.25;
        [Export] public double MinDecayMultiplier { get; set; } = 0.45;

        [Export] public double RollingWindowSeconds { get; set; } = 10.0;
        [Export] public double SoftCapPerMinute { get; set; } = 420.0;
        [Export] public double MinCapMultiplier { get; set; } = 0.20;
        [Export] public double AccumulatorDrainPerSecond { get; set; } = 0.6;

        private readonly object _pendingLock = new();
        private readonly Queue<(double time, double rawAp)> _rollingEvents = new();

        private double _pendingRawAp;
        private int _pendingInputEvents;
        private int _pendingKeyDown;
        private int _pendingMouseClick;
        private int _pendingMouseScroll;
        private double _pendingMouseMove;

        private double _rollingRawApSum;
        private double _runtimeSeconds;
        private double _minuteWindowStartSeconds;
        private double _apFinalThisMinute;

        public override void _Process(double delta)
        {
            _runtimeSeconds += delta;

            if (_runtimeSeconds - _minuteWindowStartSeconds >= 60.0)
            {
                _minuteWindowStartSeconds = _runtimeSeconds;
                _apFinalThisMinute = 0.0;
            }

            DrainPendingInput(out double apRawBatch, out int inputBatch, out int keyBatch, out int clickBatch, out int scrollBatch, out double moveBatch);
            if (apRawBatch <= 0.0 && inputBatch <= 0)
            {
                return;
            }

            KeyDownCount = keyBatch;
            MouseClickCount = clickBatch;
            MouseScrollSteps = scrollBatch;
            MouseMoveDistancePx = moveBatch;
            InputEventsThisSecond = inputBatch;

            PushRollingWindow(apRawBatch);
            PruneRollingWindow();

            double apPerSecondEquivalent = RollingWindowSeconds > 0.0 ? _rollingRawApSum / RollingWindowSeconds : _rollingRawApSum;
            double decayMultiplier = CalculateDecayMultiplier(apPerSecondEquivalent);
            double capMultiplier = CalculateCapMultiplier();

            ApThisSecond = apRawBatch;
            ApFinal = apRawBatch * decayMultiplier * capMultiplier;
            _apFinalThisMinute += ApFinal;

            ApAccumulator = Math.Max(0.0, ApAccumulator + ApFinal - AccumulatorDrainPerSecond * delta);

            EmitSignal(SignalName.ActivityTick, ApThisSecond, ApFinal);
            EmitSignal(SignalName.InputBatchTick, InputEventsThisSecond, ApFinal);
        }

        public void RegisterKeyDown()
        {
            lock (_pendingLock)
            {
                _pendingKeyDown++;
                _pendingInputEvents++;
                _pendingRawAp += KeyDownWeight;
                TotalKeyDownCount++;
            }
        }

        public void RegisterMouseClick()
        {
            lock (_pendingLock)
            {
                _pendingMouseClick++;
                _pendingInputEvents++;
                _pendingRawAp += MouseClickWeight;
                TotalMouseClickCount++;
            }
        }

        public void RegisterMouseScroll(int steps)
        {
            if (steps <= 0)
            {
                return;
            }

            lock (_pendingLock)
            {
                _pendingMouseScroll += steps;
                _pendingInputEvents += steps;
                _pendingRawAp += steps * ScrollStepWeight;
                TotalMouseScrollSteps += steps;
            }
        }

        public void RegisterMouseMove(double distancePx)
        {
            if (distancePx <= 0.0)
            {
                return;
            }

            lock (_pendingLock)
            {
                _pendingMouseMove += distancePx;
                _pendingRawAp += distancePx / MovePxDivider;
                TotalMouseMoveDistancePx += distancePx;
            }
        }

        private void DrainPendingInput(
            out double apRawBatch,
            out int inputBatch,
            out int keyBatch,
            out int clickBatch,
            out int scrollBatch,
            out double moveBatch)
        {
            lock (_pendingLock)
            {
                apRawBatch = _pendingRawAp;
                inputBatch = _pendingInputEvents;
                keyBatch = _pendingKeyDown;
                clickBatch = _pendingMouseClick;
                scrollBatch = _pendingMouseScroll;
                moveBatch = _pendingMouseMove;

                _pendingRawAp = 0.0;
                _pendingInputEvents = 0;
                _pendingKeyDown = 0;
                _pendingMouseClick = 0;
                _pendingMouseScroll = 0;
                _pendingMouseMove = 0.0;
            }
        }

        private void PushRollingWindow(double apRawBatch)
        {
            _rollingEvents.Enqueue((_runtimeSeconds, apRawBatch));
            _rollingRawApSum += apRawBatch;
        }

        private void PruneRollingWindow()
        {
            double cutoff = _runtimeSeconds - Math.Max(1.0, RollingWindowSeconds);
            while (_rollingEvents.Count > 0 && _rollingEvents.Peek().time < cutoff)
            {
                var ev = _rollingEvents.Dequeue();
                _rollingRawApSum -= ev.rawAp;
            }

            if (_rollingRawApSum < 0.0)
            {
                _rollingRawApSum = 0.0;
            }
        }

        private double CalculateDecayMultiplier(double apPerSecond)
        {
            double ratio = ApBaseline > 0 ? apPerSecond / ApBaseline : apPerSecond;
            double decay = 1.0 - Math.Max(0, ratio - DecayThreshold) * DecayRate;
            if (decay < MinDecayMultiplier)
            {
                return MinDecayMultiplier;
            }
            if (decay > 1.0)
            {
                return 1.0;
            }
            return decay;
        }

        private double CalculateCapMultiplier()
        {
            if (SoftCapPerMinute <= 0.0)
            {
                return 1.0;
            }

            double ratio = _apFinalThisMinute / SoftCapPerMinute;
            if (ratio <= 1.0)
            {
                return 1.0;
            }

            double multiplier = 1.0 / ratio;
            if (multiplier < MinCapMultiplier)
            {
                return MinCapMultiplier;
            }
            return multiplier;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["total_key_down"] = TotalKeyDownCount,
                ["total_mouse_click"] = TotalMouseClickCount,
                ["total_scroll_steps"] = TotalMouseScrollSteps,
                ["total_move_distance"] = TotalMouseMoveDistancePx,
                ["ap_accumulator"] = ApAccumulator
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            TotalKeyDownCount = data.ContainsKey("total_key_down") ? data["total_key_down"].AsInt64() : 0L;
            TotalMouseClickCount = data.ContainsKey("total_mouse_click") ? data["total_mouse_click"].AsInt64() : 0L;
            TotalMouseScrollSteps = data.ContainsKey("total_scroll_steps") ? data["total_scroll_steps"].AsInt64() : 0L;
            TotalMouseMoveDistancePx = data.ContainsKey("total_move_distance") ? data["total_move_distance"].AsDouble() : 0.0;
            ApAccumulator = data.ContainsKey("ap_accumulator") ? data["ap_accumulator"].AsDouble() : 0.0;
        }

        public void ResetCurrentTick()
        {
            lock (_pendingLock)
            {
                _pendingRawAp = 0.0;
                _pendingInputEvents = 0;
                _pendingKeyDown = 0;
                _pendingMouseClick = 0;
                _pendingMouseScroll = 0;
                _pendingMouseMove = 0.0;
            }

            KeyDownCount = 0;
            MouseClickCount = 0;
            MouseScrollSteps = 0;
            MouseMoveDistancePx = 0.0;
            ApThisSecond = 0.0;
            ApFinal = 0.0;
            InputEventsThisSecond = 0;

            _rollingEvents.Clear();
            _rollingRawApSum = 0.0;
        }
    }
}
