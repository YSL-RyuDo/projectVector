// Generated from ServerWeaponData.cpp and weapon_damage_profiles.csv. Do not tune values here.
public static class WeaponStoreStats
{
    public sealed class Entry
    {
        public string name, category;
        public int damage, magazine, reserve;
        public float range, interval, reload, falloffStart, falloffEnd;
    }
    public static readonly Entry[] All = {
        new Entry {name="P92",category="Pistol",damage=20,range=300.0f,interval=0.2f,magazine=12,reserve=36,reload=1.4f,falloffStart=15f,falloffEnd=35f},
        new Entry {name="P1911",category="Pistol",damage=25,range=1000.0f,interval=0.25f,magazine=7,reserve=21,reload=1.5f,falloffStart=20f,falloffEnd=40f},
        new Entry {name="R1895",category="Pistol",damage=50,range=300.0f,interval=0.45f,magazine=6,reserve=18,reload=1.7f,falloffStart=30f,falloffEnd=60f},
        new Entry {name="UZI",category="SMG",damage=24,range=200.0f,interval=0.06f,magazine=20,reserve=60,reload=2.2f,falloffStart=10f,falloffEnd=30f},
        new Entry {name="UMP",category="SMG",damage=23,range=1000.0f,interval=0.075f,magazine=30,reserve=90,reload=2.2f,falloffStart=15f,falloffEnd=40f},
        new Entry {name="S1897",category="Shotgun",damage=20,range=25.0f,interval=1.0f,magazine=5,reserve=15,reload=2.35f,falloffStart=5f,falloffEnd=20f},
        new Entry {name="AA12",category="Shotgun",damage=19,range=100.0f,interval=0.5f,magazine=5,reserve=15,reload=2.1f,falloffStart=6f,falloffEnd=25f},
        new Entry {name="Dragunov",category="DMR",damage=75,range=3800.0f,interval=0.22f,magazine=12,reserve=36,reload=2.4f,falloffStart=40f,falloffEnd=80f},
        new Entry {name="M416",category="AR",damage=35,range=3600.0f,interval=0.09f,magazine=30,reserve=90,reload=2.4f,falloffStart=20f,falloffEnd=50f},
        new Entry {name="AK47",category="AR",damage=40,range=3000.0f,interval=0.1f,magazine=25,reserve=75,reload=2.4f,falloffStart=30f,falloffEnd=60f},
        new Entry {name="M249",category="LMG",damage=37,range=3600.0f,interval=0.078f,magazine=100,reserve=200,reload=4.5f,falloffStart=25f,falloffEnd=65f},
        new Entry {name="Kar98",category="SR",damage=105,range=4700.0f,interval=0.8f,magazine=5,reserve=15,reload=2.5f,falloffStart=50f,falloffEnd=100f},
        new Entry {name="AWM",category="SR",damage=150,range=3900.0f,interval=2.0f,magazine=5,reserve=15,reload=3.5f,falloffStart=80f,falloffEnd=140f},
        new Entry {name="Knife",category="Knife",damage=50,range=2.0f,interval=0.3f,magazine=0,reserve=0,reload=0.0f,falloffStart=0f,falloffEnd=0f},
    };
}
