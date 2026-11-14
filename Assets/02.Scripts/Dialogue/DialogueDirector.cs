using System.Collections.Generic;
using Blacksmith.Core.Dialogue;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Blacksmith.Dialogue
{
    // 내 화면의 대화 하나를 진행한다. 씬 오브젝트이며 네트워크를 모른다.
    // 대화가 열려 있는 동안 캐릭터 조작 입력을 잠그고, 대사 넘기기·선택지 입력은 입력 액션을 직접 읽는다.
    public class DialogueDirector : MonoBehaviour
    {
        private enum Stage
        {
            Lines,
            Choices,
            Waiting,
        }

        [SerializeField] private DialogueChannel _channel;
        [SerializeField] private DialogueView _view;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [Tooltip("대사 넘기기와 강조된 선택지 확정 (UI/Submit)")]
        [SerializeField] private InputActionReference _submit;
        [SerializeField] private InputActionReference _choiceUp;
        [SerializeField] private InputActionReference _choiceDown;
        [Tooltip("숫자키 1~9. 누른 키의 숫자로 선택지를 강조한다")]
        [SerializeField] private InputActionReference _choiceNumber;

        private readonly List<DialogueChoiceView> _choiceViews = new List<DialogueChoiceView>();

        private DialogueRequest _current;
        private DialogueData.Entry _entry;
        private int _lineIndex;
        private Stage _stage;
        private DialogueChoiceCursor _cursor;

        public bool IsOpen => _current != null;

        private void OnEnable()
        {
            _channel.Register(this);
            _view.ChoiceClicked += OnChoiceClicked;
            _submit.action.Enable();
            _choiceUp.action.Enable();
            _choiceDown.action.Enable();
            _choiceNumber.action.Enable();
        }

        private void OnDisable()
        {
            if (_current != null)
            {
                Finish(DialogueCloseReason.ClosedByOwner);
            }

            _view.ChoiceClicked -= OnChoiceClicked;
            _channel.Unregister(this);
        }

        public bool Play(DialogueRequest request)
        {
            if (_current != null)
            {
                // 함께 보는 대화만 열린 대화를 닫고 들어올 수 있다.
                if (!request.IsShared || _current.IsShared)
                {
                    return false;
                }

                Finish(DialogueCloseReason.Interrupted);
            }

            if (_channel.Stats == null)
            {
                Debug.LogWarning("대화 조건 값을 읽을 GameDialogueStats가 씬에 없다.", this);
                return false;
            }

            DialogueData.Entry entry = request.Data.PickEntry(_channel.Stats);
            if (entry == null || entry.Lines.Count == 0)
            {
                Debug.LogWarning($"{request.Data.name}에 조건이 맞는 대사가 없다.", request.Data);
                return false;
            }

            _current = request;
            _entry = entry;
            _lineIndex = 0;
            _inputLock.Lock(this);
            ShowLine();
            return true;
        }

        public void Close(DialogueRequest request)
        {
            if (request != null && request == _current)
            {
                Finish(DialogueCloseReason.ClosedByOwner);
            }
        }

        private void Update()
        {
            if (_current == null)
            {
                return;
            }

            if (_stage == Stage.Lines && _submit.action.WasPressedThisFrame())
            {
                Advance();
            }
            else if (_stage == Stage.Choices)
            {
                ReadChoiceInput();
            }
        }

        private void ShowLine()
        {
            _stage = Stage.Lines;
            DialogueData.Line line = _entry.Lines[_lineIndex];
            string speakerName = line.Speaker != null ? line.Speaker.DisplayName : _current.FallbackSpeakerName;
            Sprite portrait = line.Speaker != null ? line.Speaker.Portrait : null;
            _view.ShowLine(speakerName, portrait, line.Text);
        }

        private void Advance()
        {
            _lineIndex++;
            if (_lineIndex < _entry.Lines.Count)
            {
                ShowLine();
            }
            else if (_current.IsShared)
            {
                _stage = Stage.Waiting;
                _view.ShowWaiting();
                _current.LinesFinished?.Invoke();
            }
            else if (_current.Data.Choices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                Finish(DialogueCloseReason.Finished);
            }
        }

        private void ShowChoices()
        {
            IReadOnlyList<DialogueData.Choice> choices = _current.Data.Choices;
            var enabled = new bool[choices.Count];
            _choiceViews.Clear();
            for (int i = 0; i < choices.Count; i++)
            {
                enabled[i] = _current.Data.IsChoiceEnabled(i, _channel.Stats);
                _choiceViews.Add(new DialogueChoiceView
                {
                    Text = choices[i].Text,
                    IsEnabled = enabled[i],
                    DisabledReason = choices[i].DisabledText,
                });
            }

            _stage = Stage.Choices;
            _cursor = new DialogueChoiceCursor(enabled);
            _view.ShowChoices(_choiceViews);
            _view.SetHighlight(_cursor.Highlighted);
        }

        private void ReadChoiceInput()
        {
            if (_choiceUp.action.WasPressedThisFrame())
            {
                _cursor.Move(-1);
            }
            else if (_choiceDown.action.WasPressedThisFrame())
            {
                _cursor.Move(1);
            }
            else if (_choiceNumber.action.WasPressedThisFrame() && TryReadNumber(out int number))
            {
                _cursor.HighlightNumber(number);
            }
            else if (_submit.action.WasPressedThisFrame())
            {
                TryChoose(_cursor.Highlighted);
                return;
            }

            _view.SetHighlight(_cursor.Highlighted);
        }

        private bool TryReadNumber(out int number)
        {
            number = 0;
            if (!(_choiceNumber.action.activeControl is KeyControl key))
            {
                return false;
            }

            if (key.keyCode >= Key.Digit1 && key.keyCode <= Key.Digit9)
            {
                number = key.keyCode - Key.Digit1 + 1;
            }
            else if (key.keyCode >= Key.Numpad1 && key.keyCode <= Key.Numpad9)
            {
                number = key.keyCode - Key.Numpad1 + 1;
            }

            return number > 0;
        }

        // 마우스 클릭은 강조를 거치지 않고 바로 고른다.
        private void OnChoiceClicked(int index)
        {
            if (_stage != Stage.Choices || _current == null)
            {
                return;
            }

            _cursor.HighlightNumber(index + 1);
            _view.SetHighlight(_cursor.Highlighted);
            TryChoose(index);
        }

        private void TryChoose(int index)
        {
            if (index == DialogueChoiceCursor.NoHighlight)
            {
                return;
            }

            if (!_cursor.IsEnabled(index))
            {
                _view.PlayReject(index);
                return;
            }

            // 호스트에서는 선택 RPC가 바로 실행되어 결과 대답(새 대화)을 열 수 있으므로, 창을 먼저 닫고 선택을 알린다.
            DialogueRequest request = Clear();
            request.ChoiceSelected?.Invoke(index);
            request.Closed?.Invoke(DialogueCloseReason.Chosen);
        }

        private void Finish(DialogueCloseReason reason)
        {
            Clear().Closed?.Invoke(reason);
        }

        private DialogueRequest Clear()
        {
            DialogueRequest request = _current;
            _current = null;
            _entry = null;
            _cursor = null;
            _view.Hide();
            _inputLock.Release(this);
            return request;
        }
    }
}
