using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace YARG.Input
{
    public class AxisBindingParameters
    {
    }

    public class AxisBinding : ControlBinding<float, AxisBindingParameters>
    {
        private float _currentValue;

        public AxisBinding(string name, int action) : base(name, action)
        {
        }

        public override bool IsControlActuated(ActuationSettings settings, InputControl<float> control, InputEventPtr eventPtr)
        {
            if (!control.HasValueChangeInEvent(eventPtr))
                return false;

            // The buffer that ReadValue reads from is not updated until after all events have been processed
            float previousValue = control.ReadValue();
            float value = control.ReadValueFromEvent(eventPtr);

            return Math.Abs(value - previousValue) >= settings.AxisDeltaThreshold;
        }

        public override void ProcessInputEvent(InputEventPtr eventPtr)
        {
            float max = 0f;
            foreach (var binding in Bindings)
            {
                var value = binding.UpdateState(eventPtr);
                if (value > max)
                    max = value;
            }

            ProcessNextState(eventPtr.time, max);
        }

        private void ProcessNextState(double time, float state)
        {
            // Ignore if state is unchanged
            if (Mathf.Approximately(_currentValue, state))
                return;

            _currentValue = state;
            FireInputEvent(time, state);
        }
    }
}