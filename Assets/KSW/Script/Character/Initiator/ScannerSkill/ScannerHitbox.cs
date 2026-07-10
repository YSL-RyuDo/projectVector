using UnityEngine;

public class ScannerHitbox : MonoBehaviour
{
    private ScannerProjectile _scanner;

    private void Awake()
    {
        _scanner = GetComponentInParent<ScannerProjectile>();
    }

    public uint ScannerId
    {
        get
        {
            if (_scanner == null)
                return 0;

            return _scanner.ScannerId;
        }
    }

    public ScannerProjectile Scanner => _scanner;
}