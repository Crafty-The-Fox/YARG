using System.Diagnostics;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using YARG.Input.Serialization;

namespace YARG.Input
{
    public class ButtonBindingParameters
    {
        public float PressPoint;
    }

    public class ButtonBinding : ControlBinding<float, ButtonBindingParameters>
    {
        public const long DEBOUNCE_TIME_MAX = 25;
        public const long DEBOUNCE_ACTIVE_THRESHOLD = 1;

        private Stopwatch _debounceTimer = new();
        private long _debounceThreshold = 0;

        /// <summary>
        /// The debounce time threshold, in milliseconds. Use 0 or less to disable debounce.
        /// </summary>
        public long DebounceLength
        {
            get => _debounceThreshold;
            set
            {
                // Limit debounce amount to 0-25 ms
                // Any larger and input registration will be very bad, the max will limit to 40 inputs per second
                // If someone needs a larger amount their controller is just busted lol
                if (value > DEBOUNCE_TIME_MAX)
                {
                    value = DEBOUNCE_TIME_MAX;
                }
                else if (value < 0)
                {
                    value = 0;
                }

                _debounceThreshold = value;
            }
        }

        public bool DebounceEnabled => DebounceLength >= DEBOUNCE_ACTIVE_THRESHOLD;

        // TODO: maybe rework this? not sure how else to go about it though lol
        /// <summary>
        /// A binding whose debouncing timer should be overridden by a state change on this control,
        /// and whose state changes should also override the debouncing timer on this control.
        /// </summary>
        public ButtonBinding DebounceOverrideBinding { get; set; } = null;

        private bool _currentValue;
        private bool _postDebounceValue;

        public ButtonBinding(string name, int action) : base(name, action)
        {
        }

        public override bool IsControlActuated(ActuationSettings settings, InputControl<float> control,
            InputEventPtr eventPtr)
        {
            if (!control.HasValueChangeInEvent(eventPtr))
                return false;

            float pressPoint = settings.ButtonPressThreshold;
            float value = control.ReadValueFromEvent(eventPtr);

            return value >= pressPoint;
        }

        public override void ProcessInputEvent(InputEventPtr eventPtr)
        {
            bool state = false;
            foreach (var binding in Bindings)
            {
                var value = binding.UpdateState(eventPtr);
                state |= value >= binding.Parameters.PressPoint;
            }

            // Track real state here for debouncing
            _postDebounceValue = state;

            // Skip the rest if debouncing
            if (_debounceTimer.IsRunning)
            {
                UpdateDebounce();
                return;
            }

            ProcessNextState(eventPtr.time, state);
        }

        private void ProcessNextState(double time, bool state)
        {
            // Ignore if state is unchanged
            if (_currentValue == state)
                return;

            _currentValue = state;

            // Start debounce
            if (DebounceEnabled)
            {
                _debounceTimer.Start();
            }

            // Override debounce for a corresponding control, if requested
            DebounceOverrideBinding?.OverrideDebounce();

            FireInputEvent(time, state);
        }

        public override void UpdateForFrame()
        {
            UpdateDebounce();
        }

        private void UpdateDebounce()
        {
            // Ignore if not a button, or threshold is below the minimum
            if (!DebounceEnabled)
            {
                if (_debounceTimer.IsRunning)
                {
                    _debounceTimer.Reset();
                }

                return;
            }

            // Check time elapsed
            if (_debounceTimer.ElapsedMilliseconds >= DebounceLength)
            {
                OverrideDebounce();
                return;
            }
        }

        private void OverrideDebounce()
        {
            if (DebounceEnabled && _debounceTimer.IsRunning)
            {
                // Stop timer and process post-debounce value
                _debounceTimer.Reset();
                ProcessNextState(InputManager.CurrentInputTime, _postDebounceValue);
            }
        }

        protected override void OnControlAdded(ActuationSettings settings, SingleBinding binding)
        {
            base.OnControlAdded(settings, binding);

            float pressPoint = settings.ButtonPressThreshold;
            if (binding.Control is ButtonControl button)
            {
                pressPoint = button.pressPointOrDefault;
            }

            binding.Parameters.PressPoint = pressPoint;
        }

        private const string PRESS_POINT_NAME = nameof(ButtonBindingParameters.PressPoint);

        protected override SerializedInputControl SerializeControl(SingleBinding binding)
        {
            var serialized = base.SerializeControl(binding);
            if (serialized is null)
                return null;

            serialized.Parameters.Add(PRESS_POINT_NAME, binding.Parameters.PressPoint.ToString());

            return serialized;
        }

        protected override SingleBinding DeserializeControl(InputDevice device, SerializedInputControl serialized)
        {
            var binding = base.DeserializeControl(device, serialized);
            if (binding is null)
                return null;

            if (!serialized.Parameters.TryGetValue(PRESS_POINT_NAME, out string pressPointText) ||
                !float.TryParse(pressPointText, out float pressPoint))
                pressPoint = InputSystem.settings.defaultButtonPressPoint;

            binding.Parameters.PressPoint = pressPoint;

            return binding;
        }
    }
}