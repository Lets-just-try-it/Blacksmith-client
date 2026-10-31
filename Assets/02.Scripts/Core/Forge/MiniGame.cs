using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 제작 단계 하나를 직접 조작하는 미니게임. 끝나면 Accuracy(0~1)가 정해진다.
    // 실패는 없어서 정확도가 0이어도 끝까지 진행하고 완료된다.
    public abstract class MiniGame
    {
        public bool Finished { get; protected set; }
        public float Accuracy { get; protected set; }
        public Queue<MiniEvent> Events { get; } = new Queue<MiniEvent>();

        public abstract void Tick(float deltaTime, MiniInput input);

        protected void Raise(MiniEventKind kind, float value = 0f, int index = 0, float extra = 0f)
        {
            Events.Enqueue(new MiniEvent(kind, value, index, extra));
        }

        protected static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}
