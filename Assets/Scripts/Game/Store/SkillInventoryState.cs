using System;

public enum SkillSlot : byte
{
    C = 0,
    Q = 1,
    E = 2,
    X = 3
}

public static class SkillInventoryState
{
    public static event Action Changed;

    public static byte PurchasedMask { get; private set; }
    public static byte UsedMask { get; private set; }

    public static bool IsPurchased(SkillSlot slot)
    {
        return (PurchasedMask & GetBit(slot)) != 0;
    }

    public static bool IsUsed(SkillSlot slot)
    {
        return (UsedMask & GetBit(slot)) != 0;
    }

    public static bool CanUse(SkillSlot slot)
    {
        return IsPurchased(slot) && !IsUsed(slot);
    }

    public static void ApplyServerState(byte purchasedMask, byte usedMask)
    {
        PurchasedMask = (byte)(purchasedMask & 0x0F);
        UsedMask = (byte)(usedMask & PurchasedMask);
        Changed?.Invoke();
    }

    public static void Reset()
    {
        ApplyServerState(0, 0);
    }

    public static int GetPrice(SkillSlot slot)
    {
        switch (slot)
        {
            case SkillSlot.C: return 300;
            case SkillSlot.Q: return 400;
            case SkillSlot.E: return 400;
            case SkillSlot.X: return 700;
            default: return int.MaxValue;
        }
    }

    private static byte GetBit(SkillSlot slot)
    {
        return (byte)(1 << (int)slot);
    }
}
