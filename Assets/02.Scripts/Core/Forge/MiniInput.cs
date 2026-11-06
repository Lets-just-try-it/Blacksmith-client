namespace Blacksmith.Core.Forge
{
    // 미니게임이 한 프레임에 받는 입력. 입력 장치는 입력 계층이 읽어서 이 값으로 넘긴다.
    public struct MiniInput
    {
        public bool ActionPressed;
        public bool ActionHeld;
        public bool ActionReleased;
        // 화면을 직접 찍거나 쥐는 미니게임의 포인터. 이번 프레임에 눌렀는지, 누르고 있는지, 미니게임 좌표의 위치다.
        public bool PointerPressed;
        public bool PointerHeld;
        public float PointerX;
        public float PointerY;
        // 오른쪽 마우스 버튼(뒤집기 등)을 이번 프레임에 눌렀는지
        public bool SecondaryPressed;
        // 이번 프레임에 누른 좌우 방향. -1 왼쪽, 1 오른쪽, 0 없음
        public int Arrow;
        // 이번 프레임에 누른 숫자 키(1부터). 0이면 누르지 않았다
        public int ToolNumber;
        // 마무리 키(F)를 이번 프레임에 눌렀는지
        public bool FinishPressed;
    }
}
