using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Dialogue
{
    public struct DialogueChoiceView
    {
        public string Text;
        public bool IsEnabled;
        public string DisabledReason;
    }

    // 대사창 화면만 맡는다. 진행과 입력은 DialogueDirector가 한다.
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _speakerName;
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private GameObject _advanceHint;
        [SerializeField] private GameObject _waitingLabel;
        [SerializeField] private Transform _choiceRoot;
        [SerializeField] private DialogueChoiceButton _choicePrefab;

        private readonly List<DialogueChoiceButton> _choices = new List<DialogueChoiceButton>();

        public event Action<int> ChoiceClicked;

        private void Awake()
        {
            Hide();
        }

        public void ShowLine(string speakerName, Sprite portrait, string text)
        {
            _panel.SetActive(true);
            _speakerName.text = speakerName;
            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);
            _lineText.text = text;
            _advanceHint.SetActive(true);
            _waitingLabel.SetActive(false);
            HideChoices();
        }

        public void ShowChoices(IReadOnlyList<DialogueChoiceView> choices)
        {
            _advanceHint.SetActive(false);
            EnsureChoiceCount(choices.Count);
            for (int i = 0; i < _choices.Count; i++)
            {
                bool isUsed = i < choices.Count;
                _choices[i].gameObject.SetActive(isUsed);
                if (isUsed)
                {
                    _choices[i].Show(i + 1, choices[i].Text, choices[i].IsEnabled, choices[i].DisabledReason);
                }
            }
        }

        // 강조가 없으면 index가 음수다.
        public void SetHighlight(int index)
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                _choices[i].SetHighlighted(i == index);
            }
        }

        public void PlayReject(int index)
        {
            _choices[index].PlayReject();
        }

        public void ShowWaiting()
        {
            _advanceHint.SetActive(false);
            _waitingLabel.SetActive(true);
        }

        public void Hide()
        {
            HideChoices();
            _panel.SetActive(false);
        }

        private void HideChoices()
        {
            foreach (DialogueChoiceButton choice in _choices)
            {
                choice.gameObject.SetActive(false);
            }
        }

        private void EnsureChoiceCount(int count)
        {
            while (_choices.Count < count)
            {
                DialogueChoiceButton choice = Instantiate(_choicePrefab, _choiceRoot);
                choice.Clicked += OnChoiceClicked;
                _choices.Add(choice);
            }
        }

        private void OnChoiceClicked(DialogueChoiceButton choice)
        {
            ChoiceClicked?.Invoke(_choices.IndexOf(choice));
        }
    }
}
