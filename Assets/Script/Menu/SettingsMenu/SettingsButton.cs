using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using YARG.Helpers;
using YARG.Settings;

namespace YARG.Menu.Settings
{
    public class SettingsButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject _buttonTemplate;

        [SerializeField]
        private Transform _container;

        public void SetInfo(IEnumerable<string> buttons)
        {
            // Spawn button(s)
            foreach (var buttonName in buttons)
            {
                var button = Instantiate(_buttonTemplate, _container);

                button.GetComponentInChildren<LocalizeStringEvent>().StringReference =
                    LocaleHelper.StringReference("Settings", buttonName);

                var capture = buttonName;
                button.GetComponent<Button>().onClick.AddListener(() => SettingsManager.InvokeButton(capture));
            }

            // Remove the template
            Destroy(_buttonTemplate);
        }

        public void SetCustomCallback(Action action, string localizationKey)
        {
            _buttonTemplate.GetComponentInChildren<LocalizeStringEvent>().StringReference =
                LocaleHelper.StringReference("Settings", localizationKey);

            _buttonTemplate.GetComponent<Button>().onClick.AddListener(() => action?.Invoke());
        }
    }
}