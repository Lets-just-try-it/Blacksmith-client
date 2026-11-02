using Blacksmith.Core.Dialogue;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 대화를 여는 쪽과 씬의 대화 구성 요소를 잇는 창구다. 런타임에 스폰되는 프리팹은 씬 오브젝트를
    // 인스펙터로 참조할 수 없으므로, 씬 쪽 구성 요소가 켜질 때 여기에 자신을 등록하고 프리팹은 이 에셋만 참조한다.
    [CreateAssetMenu(fileName = "DialogueChannel", menuName = "Blacksmith/Runtime/Dialogue Channel")]
    public class DialogueChannel : ScriptableObject
    {
        public DialogueDirector Director { get; private set; }
        public IDialogueStats Stats { get; private set; }
        public SharedDialogue Shared { get; private set; }
        // 내 PC에서 입력 권한을 가진 플레이어의 대화 행동 컴포넌트다.
        public DialoguePlayerActions LocalPlayer { get; private set; }

        public bool IsOpen => Director != null && Director.IsOpen;

        // 다른 대화가 열려 있어 열지 못하면 false다. 함께 보는 대화는 열려 있던 대화를 닫고 연다.
        public bool Play(DialogueRequest request)
        {
            return Director != null && Director.Play(request);
        }

        // 이미 닫혔거나 다른 요청이 열려 있으면 아무것도 하지 않는다.
        public void Close(DialogueRequest request)
        {
            if (Director != null)
            {
                Director.Close(request);
            }
        }

        public void Register(DialogueDirector director) => Director = director;
        public void Unregister(DialogueDirector director) => Director = Director == director ? null : Director;

        public void Register(IDialogueStats stats) => Stats = stats;
        public void Unregister(IDialogueStats stats) => Stats = Stats == stats ? null : Stats;

        public void Register(SharedDialogue shared) => Shared = shared;
        public void Unregister(SharedDialogue shared) => Shared = Shared == shared ? null : Shared;

        public void Register(DialoguePlayerActions localPlayer) => LocalPlayer = localPlayer;
        public void Unregister(DialoguePlayerActions localPlayer) => LocalPlayer = LocalPlayer == localPlayer ? null : LocalPlayer;
    }
}
