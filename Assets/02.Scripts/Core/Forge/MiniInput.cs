namespace Blacksmith.Core.Forge
{
    // 미니게임이 한 프레임에 받는 입력. 입력 장치는 입력 계층이 읽어서 이 값으로 넘긴다.
    public struct MiniInput
    {
        public bool ActionPressed;
        public bool ActionHeld;
        public bool ActionReleased;
    }
}
