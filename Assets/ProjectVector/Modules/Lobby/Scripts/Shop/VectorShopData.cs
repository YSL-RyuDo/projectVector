using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public sealed class VectorShopItem
{
    public int id, kind, grade, model, skin, price;
    public string name;
    public string GradeName => new[] { "신화", "희귀", "고급" }[grade];
}

public sealed class VectorShopReward
{
    public long time;
    public int item, refund;
    public bool duplicate;
}

public sealed class VectorShopState
{
    public const int HistoryPageLimit = 2;
    public ulong request;
    public int status, coin0, coin1, page, pages;
    public readonly List<VectorShopItem> items = new();
    public readonly HashSet<int> owned = new();
    public readonly List<VectorShopReward> rewards = new(), history = new();
    public double Chance(VectorShopItem item) => new[] { 5d, 24d, 71d }[item.grade] / items.Count(i => i.grade == item.grade);
    public static VectorShopState Parse(byte[] bytes)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes), Encoding.UTF8);
        var state = new VectorShopState { request = reader.ReadUInt64(), status = reader.ReadByte(), coin0 = reader.ReadInt32(), coin1 = reader.ReadInt32() };
        int count = reader.ReadUInt16(), owned = reader.ReadUInt16(), rewards = reader.ReadUInt16(), history = reader.ReadUInt16();
        state.page = reader.ReadInt32(); state.pages = reader.ReadInt32();
        if (count > 100 || owned > 100 || rewards > 10 || history > 10 || bytes.Length != 33 + count * 82 + owned * 4 + (rewards + history) * 17)
            throw new InvalidDataException("Invalid shop packet size");
        for (int i = 0; i < count; ++i)
        {
            var item = new VectorShopItem { id = reader.ReadInt32(), kind = reader.ReadByte(), grade = reader.ReadByte(), model = reader.ReadInt32(), skin = reader.ReadInt32(), price = reader.ReadInt32() };
            item.name = Encoding.UTF8.GetString(reader.ReadBytes(64)).TrimEnd('\0');
            if (item.grade > 2 || item.kind > 1) throw new InvalidDataException("Invalid shop item");
            state.items.Add(item);
        }
        for (int i = 0; i < owned; ++i) state.owned.Add(reader.ReadInt32());
        for (int i = 0; i < rewards + history; ++i)
        {
            var reward = new VectorShopReward { time = reader.ReadInt64(), item = reader.ReadInt32(), duplicate = reader.ReadByte() != 0, refund = reader.ReadInt32() };
            (i < rewards ? state.rewards : state.history).Add(reward);
        }
        // Keep the two-page UI limit even while talking to an older server.
        state.pages = Math.Clamp(state.pages, 1, HistoryPageLimit);
        if (state.page < 0 || state.page >= state.pages) state.history.Clear();
        state.page = Math.Clamp(state.page, 0, state.pages - 1);
        return state;
    }
}
