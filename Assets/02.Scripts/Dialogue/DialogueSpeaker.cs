using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 대사창에 보여 줄 화자. 같은 인물을 대사마다 다시 적지 않도록 에셋으로 둔다.
    [CreateAssetMenu(fileName = "Speaker", menuName = "Blacksmith/Dialogue/Speaker")]
    public class DialogueSpeaker : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("비워 두면 초상화 없이 보여 준다")]
        [SerializeField] private Sprite _portrait;

        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
    }
}
