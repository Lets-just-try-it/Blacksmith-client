using Blacksmith.Core.Dialogue;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 대화 조건이 읽는 값을 게임 상태에서 꺼낸다. 씬 오브젝트이며 호스트와 클라이언트 모두 같은 복제 값을 읽는다.
    // DialogueStat에 항목을 추가하면 여기에도 값을 채운다.
    public class GameDialogueStats : MonoBehaviour, IDialogueStats
    {
        [SerializeField] private DialogueChannel _channel;
        [SerializeField] private AdventurerRosterState _roster;

        private void OnEnable()
        {
            _channel.Register(this);
        }

        private void OnDisable()
        {
            _channel.Unregister(this);
        }

        public int Get(DialogueStat stat)
        {
            switch (stat)
            {
                case DialogueStat.FreeRosterSlots:
                    return _roster.IsReady ? _roster.SlotCount - _roster.Count : 0;
                default:
                    Debug.LogWarning($"{stat} 값을 채우는 코드가 없다.", this);
                    return 0;
            }
        }
    }
}
