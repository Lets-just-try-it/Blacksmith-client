using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 모험가의 초상을 정한다. 초상은 외형에서 나오므로 따로 저장하지 않는다.
    // [고도화] 외형 프리셋과 색을 조합해 초상을 그린다. 지금은 모든 모험가가 같은 임시 초상을 쓴다.
    public class AdventurerPortraitProvider : MonoBehaviour
    {
        [SerializeField] private Sprite _placeholder;

        public Sprite GetPortrait(AdventurerData adventurer)
        {
            return _placeholder;
        }
    }
}
