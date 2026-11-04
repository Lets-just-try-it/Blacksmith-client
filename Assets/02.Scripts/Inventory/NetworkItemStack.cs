using Blacksmith.Core.Inventory;
using Fusion;

namespace Blacksmith.Inventory
{
    // ItemStack을 [Networked]와 RPC에 담기 위한 형식이다. Core는 Fusion을 모르므로 필드를 풀어서 담는다.
    public struct NetworkItemStack : INetworkStruct
    {
        public int ItemId;
        public int Grade;
        public int Count;
        public int Accuracy;

        public static NetworkItemStack FromStack(ItemStack stack)
        {
            return stack.IsEmpty
                ? default
                : new NetworkItemStack { ItemId = stack.ItemId, Grade = stack.Grade, Count = stack.Count, Accuracy = stack.Accuracy };
        }

        public ItemStack ToStack()
        {
            return new ItemStack(ItemId, Grade, Count, Accuracy);
        }
    }
}
