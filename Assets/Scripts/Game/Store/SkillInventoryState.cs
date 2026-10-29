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

    public static bool HasUltimateState { get; private set; }
    public static int UltimatePoints { get; private set; }
    public static int UltimateRequired { get; private set; } = 7;
    public static int UltimatePointPrice { get; private set; } = 100;
    public static PickCharacterData Character { get; private set; }
    public static void SetCharacter(PickCharacterData data) { Character=data; Changed?.Invoke(); }
    public static PickSkillData Skill(SkillSlot slot) => Character==null?null:slot switch {SkillSlot.C=>Character.cSkill,SkillSlot.Q=>Character.qSkill,SkillSlot.E=>Character.eSkill,_=>Character.xSkill};
    public static void ApplyUltimateState(int points,int required,int price,bool used)
    {
        if(required<1 || required>30 || points<0 || points>required || price<0 || price>10000)return;
        HasUltimateState=true;UltimatePoints=points;UltimateRequired=required;UltimatePointPrice=price;
        PurchasedMask=(byte)((PurchasedMask&7)|((points==required || used)?8:0));
        UsedMask=(byte)((UsedMask&7)|(used?8:0));Changed?.Invoke();
    }

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
        HasUltimateState=false;UltimatePoints=0;UltimateRequired=7;UltimatePointPrice=100;Character=null;
        ApplyServerState(0, 0);
    }

    public static int GetPrice(SkillSlot slot)
    {
        switch (slot)
        {
            case SkillSlot.C: return 300;
            case SkillSlot.Q: return 400;
            case SkillSlot.E: return 400;
            case SkillSlot.X: return HasUltimateState ? (UltimateRequired-UltimatePoints)*UltimatePointPrice : 700;
            default: return int.MaxValue;
        }
    }

    private static byte GetBit(SkillSlot slot)
    {
        return (byte)(1 << (int)slot);
    }
}
