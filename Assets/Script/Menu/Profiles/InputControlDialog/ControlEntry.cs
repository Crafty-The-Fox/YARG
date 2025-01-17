﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YARG.Menu.Profiles
{
    public class ControlEntry : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _deviceName;

        private InputControl _inputControl;
        private Action<InputControl> _selectCallback;

        public void Init(InputControl inputControl, Action<InputControl> selectCallback)
        {
            _inputControl = inputControl;
            _selectCallback = selectCallback;

            _deviceName.text = inputControl.displayName;
        }

        public void SelectControl()
        {
            _selectCallback(_inputControl);
        }
    }
}