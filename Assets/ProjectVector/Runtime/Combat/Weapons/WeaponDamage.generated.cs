// Generated from Config/weapon_damage_profiles.csv; edit CSV and regenerate.
// Source SHA256: 1af3052e5da1caf076d4f589324b42e75d652d5d93889528635eb926d1997417
using System;
public static class WeaponDamage
{
    private static readonly double[,] Profiles = {
        {2, 1, 0.85, 15, 35, 0.65}, // P92
        {2, 1, 0.85, 20, 40, 0.75}, // P1911
        {2.5, 1, 0.85, 30, 60, 0.8}, // R1895
        {2, 1, 0.8, 10, 30, 0.55}, // UZI
        {2, 1, 0.8, 15, 40, 0.65}, // UMP
        {1.5, 1, 0.75, 5, 20, 0.35}, // S1897
        {1.5, 1, 0.75, 6, 25, 0.3}, // AA12
        {2, 1, 0.85, 40, 80, 0.85}, // Dragunov
        {4, 1, 0.85, 20, 50, 0.8}, // M416
        {4, 1, 0.85, 30, 60, 0.9}, // AK47
        {2.5, 1, 0.85, 25, 65, 0.8}, // M249
        {2, 1, 0.75, 50, 100, 0.9}, // Kar98
        {2, 1, 0.75, 80, 140, 0.9}, // AWM
        {1, 1, 1, 0, 0, 1}, // Knife
    };
    public static double Distance(UnityEngine.Vector3 origin, UnityEngine.Vector3 point)
    {
        // Match the server's double-precision subtraction/sqrt, including floor boundaries.
        double x = (double)point.x - origin.x, y = (double)point.y - origin.y, z = (double)point.z - origin.z;
        return Math.Sqrt(x*x + y*y + z*z);
    }
    public static double Scale(int id, double distance)
    {
        if (id < 0 || id >= 14 || double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0) return 0;
        double start = Profiles[id,3], end = Profiles[id,4], minimum = Profiles[id,5];
        if (end <= start || distance <= start) return 1;
        if (distance >= end) return minimum;
        return 1 - (1 - minimum) * ((distance - start) / (end - start));
    }
    // This mirrors authoritative player damage ONLY for the local practice range.
    public static int Calculate(int id, int baseDamage, double distance, int zone)
    {
        if (id < 0 || id >= 14 || zone < 0 || zone > 2 || baseDamage <= 0) return 0;
        double region = Profiles[id, zone == 1 ? 0 : zone == 2 ? 2 : 1];
        return (int)Math.Floor(baseDamage * Scale(id, distance) * region + 0.000001);
    }
}
